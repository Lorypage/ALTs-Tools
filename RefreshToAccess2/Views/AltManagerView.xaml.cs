using Microsoft.Win32;
using Newtonsoft.Json;
using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using RefreshToAccess2.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RefreshToAccess2.Views
{
    public partial class AltManagerView : System.Windows.Controls.UserControl
    {
        private AltManagerViewModel VM =>
            (AltManagerViewModel)DataContext;

        private MainViewModel RootVM =>
            (MainViewModel)Window.GetWindow(this)!.DataContext;

        public AltManagerView()
        {
            InitializeComponent();
        }

        // ── Copy helpers ───────────────────────────────────────────────

        private void OnCopyName(object sender, RoutedEventArgs e)
            => CopyField(p => p.IGN);

        private void OnCopyAccToken(object sender, RoutedEventArgs e)
            => CopyField(p => p.AccToken);

        private void OnCopyRefToken(object sender, RoutedEventArgs e)
            => CopyField(p => p.RefToken);

        private void OnCopyUuid(object sender, RoutedEventArgs e)
            => CopyField(p => p.UUID);

        private void CopyField(Func<ProfileData, string?> selector)
        {
            var block = VM.GetSelected();
            if (block?.profileData is null)
            {
                MessageBox.Show(
                    "Please select an account first.",
                    "Nothing selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            string? value = selector(block.profileData);
            if (string.IsNullOrEmpty(value))
            {
                MessageBox.Show(
                    "The selected field is empty.",
                    "Empty field",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try { Clipboard.SetText(value); }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to copy to clipboard:\n{ex.Message}",
                    "Clipboard error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // ── Refresh selected token ─────────────────────────────────────

        private async void OnRefreshToken(object sender, RoutedEventArgs e)
        {
            var block = VM.GetSelected();
            if (block?.profileData is null)
            {
                MessageBox.Show(
                    "Please select an account first.",
                    "Nothing selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(block.profileData.RefToken))
            {
                MessageBox.Show(
                    "The selected account has no refresh token stored.",
                    "No refresh token",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var conv = RootVM.Converter;
            conv.RefreshToken        = block.profileData.RefToken;
            conv.SelectedClientIndex = ClientNameToIndex(block.profileData.ClientId);

            // Navigate to Converter page (index 0)
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow is not null)
            {
                RootVM.SelectedNavIndex = 0;
                mainWindow.NavListBox.SelectedIndex = 0;
            }

            var progress = new Progress<string>(msg => conv.StatusMessage = msg);
            await conv.ConvertAsync(progress);
        }
        // ── Delete ─────────────────────────────────────────────────────

        private void OnDeleteSelected(object sender, RoutedEventArgs e)
            => VM.DeleteSelected();

        private void OnDeleteAll(object sender, RoutedEventArgs e)
            => VM.DeleteAll();

        // ── Export ─────────────────────────────────────────────────────

        private void OnExport(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title      = "Export alt profiles",
                DefaultExt = ".tapf",
                Filter     = "Token Alt Profile Files (*.tapf)|*.tapf",
                FileName   = $"alts_{DateTime.Now:yyyyMMdd_HHmmss}.tapf"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                byte[] bytes = ProfileService.ExportToBytes(
                    RootVM.TokenProfiles.ToList());

                File.WriteAllBytes(dlg.FileName, bytes);

                MessageBox.Show(
                    $"Exported {RootVM.TokenProfiles.Count} profile(s) to:\n{dlg.FileName}",
                    "Export complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Export failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ── Import ─────────────────────────────────────────────────────

        private void OnImport(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title      = "Import alt profiles",
                DefaultExt = ".tapf",
                Filter     = "Token Alt Profile Files (*.tapf)|*.tapf"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                byte[] raw = File.ReadAllBytes(dlg.FileName);

                List<ProfileDataBlock>? imported =
                    ProfileService.ImportFromBytes(raw);

                if (imported is null || imported.Count == 0)
                {
                    MessageBox.Show(
                        "The file contained no valid profile entries.",
                        "Nothing imported",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult choice = MessageBox.Show(
                    $"Found {imported.Count} profile(s).\n\n" +
                    "Click YES to merge with your existing alts.\n" +
                    "Click NO to replace all existing alts.",
                    "Import mode",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (choice == MessageBoxResult.Cancel) return;

                if (choice == MessageBoxResult.No)
                    RootVM.TokenProfiles.Clear();

                foreach (var block in imported)
                    RootVM.TokenProfiles.Add(block);

                // De-duplicate keeping the newest entry per IGN.
                var deduped = ProfileService.RemoveDuplicates(
                    RootVM.TokenProfiles.ToList());

                RootVM.TokenProfiles.Clear();
                foreach (var b in deduped)
                    RootVM.TokenProfiles.Add(b);

                VM.Save();

                MessageBox.Show(
                    $"Successfully imported {imported.Count} profile(s).",
                    "Import complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Import failed – the file may be corrupt or in the wrong format.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static int ClientNameToIndex(string? name)
        {
            if (string.IsNullOrEmpty(name)) return 0;

            for (int i = 0; i < TokenConverterViewModel.ClientNames.Length; i++)
            {
                if (string.Equals(
                        TokenConverterViewModel.ClientNames[i], name,
                        StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
        }
    }
}
