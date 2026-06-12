using MaterialDesignThemes.Wpf;
using RefreshToAccess2.Helpers;
using RefreshToAccess2.Localization;
using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using RefreshToAccess2.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace RefreshToAccess2.Views.Inject
{
    /// <summary>
    /// In-app token-selection dialog, hosted inside the root DialogHost.
    /// </summary>
    public partial class InjectionTokenSelectorView : UserControl
    {
        private const string InnerHost = "InjTokenDialog";

        // ── State ──────────────────────────────────────────────────────

        private readonly int _targetPid;
        private readonly MainViewModel _rootVm;

        // Parallel list: index matches StoredTokenComboBox.SelectedIndex.
        // Each entry holds the IGN (display) and the corresponding AccToken.
        private readonly List<(string ign, string accToken)> _validTokens = new();

        // ── Constructor ────────────────────────────────────────────────

        public InjectionTokenSelectorView(int targetPid)
        {
            InitializeComponent();

            _targetPid = targetPid;
            _rootVm = (MainViewModel)Application.Current.MainWindow.DataContext;

            Loaded += (_, _) => AppMessageBox.PushHost(InnerHost);
            Unloaded += (_, _) => AppMessageBox.PopHost(InnerHost);

            PopulateStoredTokens();
        }

        // ── Stored-token list ──────────────────────────────────────────

        private void PopulateStoredTokens()
        {
            ulong now = Helper.GetUnixTimeNative();

            foreach (ProfileDataBlock block in _rootVm.AltManager.AllProfiles())
            {
                string? token = block.profileData?.AccToken;
                string? ign = block.profileData?.IGN;

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(ign))
                    continue;

                // Skip tokens that have already expired.
                try
                {
                    ulong exp = Convert.ToUInt64(
                        RefreshToAccess2.Crypto.JWTDecode
                            .GetDecodedJWTExpDate(token));

                    if (exp < now) continue;
                }
                catch
                {
                    // If we cannot decode the expiry, skip the token to be safe.
                    continue;
                }

                _validTokens.Add((ign, token));
            }

            // Populate the combo box with display names only.
            var displayNames = new List<string>();
            foreach (var (ign, _) in _validTokens)
                displayNames.Add(ign);

            StoredTokenComboBox.ItemsSource = displayNames;
            StoredTokenComboBox.SelectedIndex = displayNames.Count > 0 ? 0 : -1;
        }

        // ── Close button ───────────────────────────────────────────────

        private void OnClose(object sender, RoutedEventArgs e)
            => DialogHost.CloseDialogCommand.Execute(false, this);

        // ── Inject button ──────────────────────────────────────────────

        private async void OnInject(object sender, RoutedEventArgs e)
        {
            if (!TokenInjectionService.PidPortMap.TryGetValue(
                    _targetPid, out int port))
            {
                MessageBox.Show(
                    Loc.T("InjTok.Msg.LostContact"),
                    Loc.T("InjTok.Msg.LostContactTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string? tokenToInject = null;

            // ── Tab 0: stored account ──────────────────────────────────
            if (SelectorTab.SelectedIndex == 0)
            {
                int idx = StoredTokenComboBox.SelectedIndex;

                if (idx < 0 || idx >= _validTokens.Count)
                {
                    MessageBox.Show(
                        Loc.T("InjTok.Msg.SelectAccount"),
                        Loc.T("InjTok.Msg.NothingSelectedTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                tokenToInject = _validTokens[idx].accToken;
            }

            // ── Tab 1: custom token ────────────────────────────────────
            else if (SelectorTab.SelectedIndex == 1)
            {
                string raw = CustomTokenBox.Text.Trim();

                if (string.IsNullOrEmpty(raw))
                {
                    MessageBox.Show(
                        Loc.T("InjTok.Msg.PasteFirst"),
                        Loc.T("InjTok.Msg.EmptyFieldTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Validate expiry before injecting.
                try
                {
                    ulong exp = Convert.ToUInt64(
                        RefreshToAccess2.Crypto.JWTDecode
                            .GetDecodedJWTExpDate(raw));

                    if (exp < Helper.GetUnixTimeNative())
                    {
                        MessageBox.Show(
                            Loc.T("InjTok.Msg.Expired"),
                            Loc.T("InjTok.Msg.ExpiredTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }
                catch
                {
                    // Token could not be decoded – warn but allow the user
                    // to override and inject anyway.
                    MessageBoxResult choice = MessageBox.Show(
                        Loc.T("InjTok.Msg.Unverified"),
                        Loc.T("InjTok.Msg.UnverifiedTitle"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (choice != MessageBoxResult.Yes) return;
                }

                tokenToInject = raw;
            }

            if (tokenToInject is null) return;

            // Disable the controls to prevent double-clicks.
            IsEnabled = false;

            try
            {
                await TokenInjectionService.SendSwapTokenAsync(port, tokenToInject);
            }
            finally
            {
                IsEnabled = true;
            }
        }
    }
}
