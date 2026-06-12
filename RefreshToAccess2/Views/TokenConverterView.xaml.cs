using RefreshToAccess2.Helpers;
using RefreshToAccess2.ViewModels;
using RefreshToAccess2.Views.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RefreshToAccess2.Views
{
    public partial class TokenConverterView : System.Windows.Controls.UserControl
    {
        private TokenConverterViewModel VM =>
            (TokenConverterViewModel)DataContext;

        public TokenConverterView()
        {
            InitializeComponent();
        }

        // ── Convert / Cancel ───────────────────────────────────────────

        private async void OnConvertClicked(object sender, RoutedEventArgs e)
        {
            var progress = new Progress<string>(msg =>
            {
                VM.StatusMessage = msg;
                AnimateStatusLabel();
            });

            await VM.ConvertAsync(progress);
        }

        // ── Refresh token ──────────────────────────────────────────────

        private void OnPasteRefreshToken(object sender, RoutedEventArgs e)
            => VM.PasteRefreshToken();

        private void OnClearRefreshToken(object sender, RoutedEventArgs e)
            => VM.ClearRefreshToken();

        // ── Token expiry check ─────────────────────────────────────────

        private void OnCheckExpiry(object sender, RoutedEventArgs e)
            => VM.CheckExpiry();

        // ── Access token ───────────────────────────────────────────────

        private void OnCopyAccessToken(object sender, RoutedEventArgs e)
            => VM.CopyAccessToken();

        private void OnClearAccessToken(object sender, RoutedEventArgs e)
            => VM.ClearAccessToken();

        // ── Profile ────────────────────────────────────────────────────

        private void OnCopyProfileName(object sender, RoutedEventArgs e)
            => VM.CopyProfileName();

        private void OnCopyUuid(object sender, RoutedEventArgs e)
            => VM.CopyUuid();

        // ── Client-ID selection ────────────────────────────────────────

        private async void OnClientSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VM.SelectedClientIndex == 10)
            {
                var dlg = new CustomClientIdDialog(VM);
                await MaterialDesignThemes.Wpf.DialogHost.Show(
                    dlg, AppMessageBox.RootDialogIdentifier);
            }
        }

        // ── Status label animation ─────────────────────────────────────

        private void AnimateStatusLabel()
        {
            StatusLabel.RenderTransform = new TranslateTransform(0, 8);
            StatusLabel.Opacity         = 0;

            var slide = new DoubleAnimation(0, TimeSpan.FromSeconds(0.35))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var fade = new DoubleAnimation(1, TimeSpan.FromSeconds(0.25))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            ((TranslateTransform)StatusLabel.RenderTransform)
                .BeginAnimation(TranslateTransform.YProperty, slide);
            StatusLabel.BeginAnimation(OpacityProperty, fade);
        }
    }
}
