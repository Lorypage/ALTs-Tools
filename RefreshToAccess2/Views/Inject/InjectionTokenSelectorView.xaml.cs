using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using RefreshToAccess2.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;

namespace RefreshToAccess2.Views.Inject
{
    public partial class InjectionTokenSelectorView : Window
    {
        // ── State ──────────────────────────────────────────────────────

        private readonly int                   _targetPid;
        private readonly MainViewModel         _rootVm;

        // Parallel list: index matches StoredTokenComboBox.SelectedIndex.
        // Each entry holds the IGN (display) and the corresponding AccToken.
        private readonly List<(string ign, string accToken)> _validTokens = new();

        // ── Constructor ────────────────────────────────────────────────

        public InjectionTokenSelectorView(int targetPid)
        {
            InitializeComponent();

            _targetPid = targetPid;
            _rootVm    = (MainViewModel)Application.Current.MainWindow.DataContext;

            PopulateStoredTokens();
        }

        // ── Stored-token list ──────────────────────────────────────────

        private void PopulateStoredTokens()
        {
            ulong now = Helper.GetUnixTimeNative();

            foreach (ProfileDataBlock block in _rootVm.AltManager.AllProfiles())
            {
                string? token = block.profileData?.AccToken;
                string? ign   = block.profileData?.IGN;

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(ign))
                    continue;

                // Skip tokens that have already expired.
                try
                {
                    // JWTDecode.GetDecodedJWTExpDate is your existing helper –
                    // leave the call as-is; it lives in the Crypto namespace
                    // that you said is already implemented.
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

            StoredTokenComboBox.ItemsSource   = displayNames;
            StoredTokenComboBox.SelectedIndex = displayNames.Count > 0 ? 0 : -1;
        }

        // ── Inject button ──────────────────────────────────────────────

        private async void OnInject(object sender, RoutedEventArgs e)
        {
            if (!TokenInjectionService.PidPortMap.TryGetValue(
                    _targetPid, out int port))
            {
                MessageBox.Show(
                    "Lost contact with the target process – " +
                    "the process may have exited.",
                    "Process not found",
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
                        "Please select an account from the list.",
                        "Nothing selected",
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
                        "Please paste an access token first.",
                        "Empty field",
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
                            "This token has already expired.\n" +
                            "Injecting it will not result in a successful authentication.",
                            "Expired token",
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
                        "Could not verify the token's expiry date.\n" +
                        "It may be invalid or in an unexpected format.\n\n" +
                        "Inject anyway?",
                        "Unverified token",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (choice != MessageBoxResult.Yes) return;
                }

                tokenToInject = raw;
            }

            if (tokenToInject is null) return;

            // Disable the button to prevent double-clicks.
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
