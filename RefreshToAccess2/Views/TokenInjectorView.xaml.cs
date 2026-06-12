using RefreshToAccess2.ViewModels;
using RefreshToAccess2.Views.Inject;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RefreshToAccess2.Views
{
    public partial class TokenInjectorView : System.Windows.Controls.UserControl
    {
        private MainViewModel RootVM =>
            (MainViewModel)Window.GetWindow(this)!.DataContext;

        public TokenInjectorView()
        {
            InitializeComponent();
        }

        // ── Open process selector ──────────────────────────────────────

        private async void OnOpenProcSelector(object sender, RoutedEventArgs e)
        {
            if (!MainWindow.InjectInitSuccess)
            {
                ShowStatus(
                    "Injector failed to initialise – " +
                    "ensure only one instance is running.");
                return;
            }

            // Optional pre-supplied access token. When the box is filled in we
            // validate it first and only continue if it checks out; when empty
            // the normal stored-token flow runs unchanged.
            string? prefilledToken = AccessTokenBox.Text?.Trim();
            if (string.IsNullOrEmpty(prefilledToken))
            {
                prefilledToken = null;
            }
            else if (!ValidateToken(prefilledToken))
            {
                return;
            }

            var selector = new MinecraftProcSelectorView(prefilledToken);
            await MaterialDesignThemes.Wpf.DialogHost.Show(
                selector, Helpers.AppMessageBox.RootDialogIdentifier);
        }

        // ── Access-token validation ────────────────────────────────────

        /// <summary>
        /// Validates that <paramref name="token"/> decodes as a JWT and has not
        /// expired. Shows a status message and returns <c>false</c> when it is
        /// expired or cannot be decoded.
        /// </summary>
        private bool ValidateToken(string token)
        {
            try
            {
                ulong exp = Convert.ToUInt64(
                    Crypto.JWTDecode.GetDecodedJWTExpDate(token));

                if (exp < Helper.GetUnixTimeNative())
                {
                    ShowStatus(Localization.Loc.T("InjTok.Msg.Expired"));
                    return false;
                }

                return true;
            }
            catch
            {
                ShowStatus(Localization.Loc.T("Injector.TokenInvalid"));
                return false;
            }
        }

        // ── Status label fade-in ───────────────────────────────────────

        private void ShowStatus(string message)
        {
            InjectorStatus.Text    = message;
            InjectorStatus.Opacity = 0;

            InjectorStatus.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(1, TimeSpan.FromSeconds(0.3)));
        }
    }
}
