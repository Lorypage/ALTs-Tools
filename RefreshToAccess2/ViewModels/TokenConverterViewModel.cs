using RefreshToAccess2.Localization;
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
        private string _profileName   = Loc.T("Converter.WaitingLogin");
        private string _playerUuid    = Loc.T("Converter.WaitingLogin");
        private string _statusMessage = Loc.T("Converter.Ready");
        private bool   _isBusy;
        private bool   _loggedIn;
        private bool   _autoCopyToken = true;
        private int    _selectedClientIndex;
        private ClientIdentification _customClient = new("", "");
        private int    _selectedModeIndex; // 0 = Refresh→Access, 1 = Cookie→Token
        private string _expiryResult = Loc.T("Converter.Expiry.Idle");
        private string _expiryInput  = "";

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

        /// <summary>0 = Refresh Token → Access Token, 1 = Cookie → Token.</summary>
        public int SelectedModeIndex
        {
            get => _selectedModeIndex;
            set
            {
                if (SetField(ref _selectedModeIndex, value))
                {
                    OnPropertyChanged(nameof(IsCookieMode));
                    OnPropertyChanged(nameof(IsRefreshMode));
                    OnPropertyChanged(nameof(InputSectionLabel));
                    OnPropertyChanged(nameof(InputHint));
                }
            }
        }

        public bool IsCookieMode  => _selectedModeIndex == 1;
        public bool IsRefreshMode => _selectedModeIndex == 0;

        /// <summary>Section label for the shared input box, depending on mode.</summary>
        public string InputSectionLabel =>
            IsCookieMode ? Loc.T("Cookie.Section") : Loc.T("Converter.RefreshSection");

        /// <summary>Hint for the shared input box, depending on mode.</summary>
        public string InputHint =>
            IsCookieMode ? Loc.T("Cookie.InputHint") : Loc.T("Converter.RefreshHint");

        public string ConvertButtonText => IsBusy ? Loc.T("Converter.Cancel") : Loc.T("Converter.Convert");

        /// <summary>Human-readable result of the most recent token-expiry check.</summary>
        public string ExpiryResult
        {
            get => _expiryResult;
            private set => SetField(ref _expiryResult, value);
        }

        /// <summary>Token/cookie text the user pastes into the expiry-check box.</summary>
        public string ExpiryInput
        {
            get => _expiryInput;
            set => SetField(ref _expiryInput, value);
        }

        /// <summary>
        /// Converts a Microsoft login cookie into a Minecraft access token and
        /// displays the result. Deliberately does NOT log in or add the result
        /// to the Alt Manager — it only fills the display fields. Reads from the
        /// shared input field (<see cref="RefreshToken"/>).
        /// </summary>
        public async Task ConvertCookieAsync(IProgress<string> progress)
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                MessageBox.Show(
                    Loc.T("Cookie.Msg.MissingInput"),
                    Loc.T("Converter.Msg.MissingInputTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                string[] result = await CookieToTokenService.ConvertAsync(RefreshToken, progress);

                // Display only — no LoggedIn flip, no OnProfileAdded invocation.
                ProfileName = result[0];
                PlayerUuid  = result[1];
                AccessToken = result[2];

                if (AutoCopyToken)
                    Clipboard.SetText(AccessToken);

                progress.Report(Loc.T("Cookie.Status.Done"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Loc.T("Converter.Msg.SomethingWrong", ex.Message),
                    Loc.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                progress.Report(Loc.T("Cookie.Status.Failed"));
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Inspects the text in the expiry-check box and reports its expiry,
        /// trying a JWT first and then a Microsoft login cookie dump.
        /// </summary>
        public void CheckExpiry()
        {
            long? exp = Crypto.TokenExpiry.ParseTokenExp(ExpiryInput);
            var info = Crypto.TokenExpiry.Describe(exp);

            if (info is null)
            {
                ExpiryResult = Loc.T("Converter.Expiry.Unknown");
                return;
            }

            var span = info.Remaining.Duration();
            string value, unit;
            if (span.TotalDays >= 1) { value = ((int)span.TotalDays).ToString(); unit = Loc.T("Converter.Expiry.Day"); }
            else if (span.TotalHours >= 1) { value = ((int)span.TotalHours).ToString(); unit = Loc.T("Converter.Expiry.Hour"); }
            else { value = Math.Max(1, (int)span.TotalMinutes).ToString(); unit = Loc.T("Converter.Expiry.Minute"); }

            string rel = info.Expired
                ? Loc.T("Converter.Expiry.Expired", value, unit)
                : Loc.T("Converter.Expiry.Remaining", value, unit);

            string abs = Loc.T("Converter.Expiry.At", info.ExpiryLocal.ToString("yyyy-MM-dd HH:mm"));
            ExpiryResult = rel + "  " + abs;
        }

        /// <summary>Re-raises localized text properties after a language switch.</summary>
        public void RefreshLocalizedText()
        {
            // Update placeholder text only while no profile has been loaded yet,
            // so a logged-in player's real name/UUID are never overwritten.
            if (!LoggedIn)
            {
                ProfileName = Loc.T("Converter.WaitingLogin");
                PlayerUuid  = Loc.T("Converter.WaitingLogin");
            }

            OnPropertyChanged(nameof(ConvertButtonText));
            OnPropertyChanged(nameof(InputSectionLabel));
            OnPropertyChanged(nameof(InputHint));
        }

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
                    Loc.T("Converter.Msg.MissingInput"),
                    Loc.T("Converter.Msg.MissingInputTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Cookie → Token mode: display-only, no login, no Alt Manager.
            if (IsCookieMode)
            {
                await ConvertCookieAsync(progress);
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

                string summary = Loc.T("Converter.Msg.Summary", ProfileName, PlayerUuid);

                if (AutoCopyToken)
                    summary += Loc.T("Converter.Msg.SummaryCopied");

                MessageBox.Show(summary, Loc.T("Common.Success"),
                    MessageBoxButton.OK, MessageBoxImage.Information);

                progress.Report(Loc.T("Converter.Status.LoginSuccess"));
            }
            catch (OperationCanceledException)
            {
                progress.Report(Loc.T("Converter.Status.Cancelled"));
            }
            catch (Exception ex)
            {
                string friendly = ex.Message switch
                {
                    var m when m.Contains("400") =>
                        Loc.T("Converter.Msg.Error400"),
                    var m when m.Contains("429") =>
                        Loc.T("Converter.Msg.Error429"),
                    var m when m.Contains("502") =>
                        Loc.T("Converter.Msg.Error502"),
                    _ => ex.Message
                };

                MessageBox.Show(
                    Loc.T("Converter.Msg.SomethingWrong", friendly),
                    Loc.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);

                progress.Report(Loc.T("Converter.Status.LoginFailed"));
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
                    Loc.T("Converter.Msg.ClearRefresh"), Loc.T("Common.Confirm"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes)
                RefreshToken = "";
        }

        public void ClearAccessToken()
        {
            if (string.IsNullOrEmpty(AccessToken)) return;
            if (MessageBox.Show(
                    Loc.T("Converter.Msg.ClearAccess"), Loc.T("Common.Confirm"),
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
                        Loc.T("Converter.Msg.OverrideRefresh"), Loc.T("Common.Confirm"),
                        MessageBoxButton.YesNo, MessageBoxImage.Question)
                    != MessageBoxResult.Yes) return;
            }

            if (!looksValid)
            {
                if (MessageBox.Show(
                        Loc.T("Converter.Msg.NotLikeToken"), Loc.T("Common.Warning"),
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
                        Loc.T("Converter.Msg.CustomNotConfigured"));
                return CustomClient;
            }

            return ClientMap[SelectedClientIndex];
        }

        /// <summary>
        /// Maps a stored profile's <c>ClientId</c> display name (e.g. "Vanilla",
        /// "HMCL") back to its <see cref="ClientIdentification"/>. Falls back to
        /// <see cref="ClientIdentification.Vanilla"/> when the name is unknown,
        /// empty, or "Custom" (whose secret isn't persisted with the profile).
        /// </summary>
        public static ClientIdentification ResolveClientByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return ClientIdentification.Vanilla;

            for (int i = 0; i < ClientMap.Length; i++)
                if (string.Equals(ClientNames[i], name, StringComparison.OrdinalIgnoreCase))
                    return ClientMap[i];

            return ClientIdentification.Vanilla;
        }
    }
}
