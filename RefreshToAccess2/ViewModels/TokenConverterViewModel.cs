using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace RefreshToAccess2.ViewModels
{
    public sealed class TokenConverterViewModel : ViewModelBase
    {
        // ── Event raised after a successful login ──────────────────────
        /// <summary>
        /// Fired on the UI thread after every successful token conversion.
        /// The argument is the newly-created <see cref="ProfileDataBlock"/>.
        /// </summary>
        public event Action<ProfileDataBlock>? OnProfileAdded;

        // ── Client-ID map ──────────────────────────────────────────────
        private static readonly ClientIdentification[] ClientMap =
        {
            ClientIdentification.Vanilla,
            ClientIdentification.HMCL,
            ClientIdentification.PCL,
            ClientIdentification.Essential,
            ClientIdentification.TziChecker,
            ClientIdentification.MalChecker,
            ClientIdentification.InGameAccountSwitcher,
            ClientIdentification.KSYZ_AltManager,
            ClientIdentification.BakaXL,
            ClientIdentification.LabyMod
        };

        public static readonly string[] ClientNames =
        {
            "Vanilla", "HMCL", "PCL", "Essential",
            "Tzi Checker", "Mal Checker",
            "In-Game Account Switcher", "ksyz Alt Manager",
            "BakaXL", "LabyMod", "Custom"
        };

        // ── Bindable properties ────────────────────────────────────────
        private string _refreshToken  = "";
        private string _accessToken   = "";
        private string _profileName   = "Waiting for login…";
        private string _playerUuid    = "Waiting for login…";
        private string _statusMessage = "Ready";
        private bool   _isBusy;
        private bool   _loggedIn;
        private bool   _autoCopyToken = true;
        private int    _selectedClientIndex;
        private ClientIdentification _customClient = new("", "");

        public string RefreshToken
        {
            get => _refreshToken;
            set => SetField(ref _refreshToken, value);
        }

        public string AccessToken
        {
            get => _accessToken;
            set => SetField(ref _accessToken, value);
        }

        public string ProfileName
        {
            get => _profileName;
            set => SetField(ref _profileName, value);
        }

        public string PlayerUuid
        {
            get => _playerUuid;
            set => SetField(ref _playerUuid, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetField(ref _isBusy, value);
                OnPropertyChanged(nameof(ConvertButtonText));
            }
        }

        public bool LoggedIn
        {
            get => _loggedIn;
            set => SetField(ref _loggedIn, value);
        }

        public bool AutoCopyToken
        {
            get => _autoCopyToken;
            set => SetField(ref _autoCopyToken, value);
        }

        public int SelectedClientIndex
        {
            get => _selectedClientIndex;
            set => SetField(ref _selectedClientIndex, value);
        }

        public ClientIdentification CustomClient
        {
            get => _customClient;
            set => SetField(ref _customClient, value);
        }

        public string ConvertButtonText => IsBusy ? "Cancel" : "Convert";

        // ── Cancellation ───────────────────────────────────────────────
        private bool _cancelRequested;

        // ── Main convert logic ─────────────────────────────────────────
        public async Task ConvertAsync(IProgress<string> progress)
        {
            // If already running, treat the call as a cancel request.
            if (IsBusy)
            {
                _cancelRequested = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                MessageBox.Show(
                    "Please paste your refresh token first.",
                    "Missing input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _cancelRequested = false;
            IsBusy           = true;

            try
            {
                ClientIdentification client = ResolveClient();

                string[] result = await MSLoginService.RequestTokenAsync(
                    RefreshToken, client, progress);

                if (_cancelRequested) return;

                // Update observable state on the UI thread
                // (MSLoginService already awaits, so we're back on the UI thread here)
                ProfileName = result[0];
                PlayerUuid  = result[1];
                AccessToken = result[2];
                LoggedIn    = true;

                if (AutoCopyToken)
                    Clipboard.SetText(AccessToken);

                var block = new ProfileDataBlock
                {
                    loginDate   = DateTime.Now.ToString(@"yyyy/MM/dd HH:mm:ss"),
                    profileData = new ProfileData
                    {
                        IGN      = ProfileName,
                        UUID     = PlayerUuid,
                        RefToken = RefreshToken,
                        AccToken = AccessToken,
                        // Guard against out-of-range: Custom is index 10
                        ClientId = SelectedClientIndex < ClientNames.Length - 1
                            ? ClientNames[SelectedClientIndex]
                            : "Custom"
                    }
                };

                // Notify root VM so it can persist + reload the alt list
                OnProfileAdded?.Invoke(block);

                string summary =
                    $"Successfully logged in\n" +
                    $"Player name : {ProfileName}\n" +
                    $"UUID        : {PlayerUuid}";

                if (AutoCopyToken)
                    summary += "\n\nAccess token copied to clipboard.";

                MessageBox.Show(summary, "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                progress.Report("Login successful");
            }
            catch (OperationCanceledException)
            {
                progress.Report("Cancelled");
            }
            catch (Exception ex)
            {
                string friendly = ex.Message switch
                {
                    var m when m.Contains("400") =>
                        "Wrong token format or expired – check with your alt seller.",
                    var m when m.Contains("429") =>
                        "Too many requests – wait a moment or switch VPN node.",
                    var m when m.Contains("502") =>
                        "Network error connecting to Microsoft services.",
                    _ => ex.Message
                };

                MessageBox.Show(
                    $"Something went wrong:\n\n{friendly}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                progress.Report("Login failed");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Clipboard helpers ──────────────────────────────────────────
        public void CopyAccessToken()
        {
            if (!string.IsNullOrEmpty(AccessToken))
                Clipboard.SetText(AccessToken);
        }

        public void CopyProfileName()
        {
            if (!string.IsNullOrEmpty(ProfileName))
                Clipboard.SetText(ProfileName);
        }

        public void CopyUuid()
        {
            if (!string.IsNullOrEmpty(PlayerUuid))
                Clipboard.SetText(PlayerUuid);
        }

        public void ClearRefreshToken()
        {
            if (string.IsNullOrEmpty(RefreshToken)) return;
            if (MessageBox.Show(
                    "Clear the current refresh token?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes)
                RefreshToken = "";
        }

        public void ClearAccessToken()
        {
            if (string.IsNullOrEmpty(AccessToken)) return;
            if (MessageBox.Show(
                    "Clear the current access token?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes)
                AccessToken = "";
        }

        public void PasteRefreshToken()
        {
            string clip = Clipboard.GetText();

            bool looksValid =
                clip.Contains("M.C5") && clip.Contains("0.U.-");

            if (!string.IsNullOrEmpty(RefreshToken))
            {
                if (MessageBox.Show(
                        "Override the current refresh token?", "Confirm",
                        MessageBoxButton.YesNo, MessageBoxImage.Question)
                    != MessageBoxResult.Yes) return;
            }

            if (!looksValid)
            {
                if (MessageBox.Show(
                        "The clipboard text doesn't look like a valid refresh token.\n" +
                        "Paste it anyway?", "Warning",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning)
                    != MessageBoxResult.Yes) return;
            }

            RefreshToken = clip;
        }

        // ── Private helpers ────────────────────────────────────────────
        private ClientIdentification ResolveClient()
        {
            if (SelectedClientIndex == 10) // Custom
            {
                if (string.IsNullOrEmpty(CustomClient.ClientId))
                    throw new Exception(
                        "Custom client ID is not configured. " +
                        "Click the ⚙ button next to the combo box.");
                return CustomClient;
            }

            return ClientMap[SelectedClientIndex];
        }
    }
}
