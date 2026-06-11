using RefreshToAccess2.Helpers;
using RefreshToAccess2.Localization;
using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RefreshToAccess2.ViewModels
{
    public sealed class SkinChangerViewModel : ViewModelBase
    {
        public sealed record PanoramaPresetOption(string Key, string Fallback)
        {
            /// <summary>Localized name shown in the panorama combo box.</summary>
            public string DisplayName
            {
                get
                {
                    string localized = Localization.Loc.T($"Skin.Pano.{Key}");
                    // If the key is missing, Loc returns the key itself — fall back.
                    return localized == $"Skin.Pano.{Key}" ? Fallback : localized;
                }
            }
        }

        private readonly TokenConverterViewModel _converter;
        private readonly MinecraftSkinService _skinService = new();

        private readonly PanoramaPresetOption[] _availablePanoramaPresets =
        {
            new("old", "Old"),
            new("aquatic", "Aquatic"),
            new("village_and_pillage", "Village & Pillage"),
            new("buzzy_bees", "Buzzy Bees"),
            new("nether", "Nether"),
            new("caves_and_cliffs_old", "Caves & Cliffs Old"),
            new("caves_and_cliffs_new", "Caves & Cliffs New"),
            new("the_wild", "The Wild"),
            new("trails_and_tales", "Trails & Tales"),
            new("tricky_trials", "Tricky Trials"),
            new("the_garden_awakens", "The Garden Awakens"),
            new("spring_to_life", "Spring to Life"),
            new("chase_the_skies", "Chase the Skies"),
            new("tiny_takeover", "Tiny Takeover"),
        };

        private bool _isBusy;
        private bool _loadedOnce;
        private string _lastLoadedToken = string.Empty;

        private string _profileName = "-";
        private string _profileId = "-";
        private string _currentSkinUrl = "-";
        private string _statusMessage = Loc.T("Common.Ready");
        private string _newName = "";

        private string? _localSkinPath;
        private string? _remoteSkinUrl;
        private byte[]? _previewSkinPng;

        private string? _panoramaSourcePath = "old";
        private PanoramaPresetOption? _selectedPanoramaPreset;

        private MinecraftSkinVariant _selectedVariant = MinecraftSkinVariant.Classic;
        private PreviewBackgroundMode _selectedBackgroundMode = PreviewBackgroundMode.Bright;
        private PreviewAnimationMode _selectedAnimationMode = PreviewAnimationMode.Auto;
        private int _cameraResetNonce;

        private string? _otherPlayerName;
        private string _otherPlayerResolvedName = "-";
        private string _otherPlayerResolvedId = "-";
        private string _otherPlayerSkinUrl = "-";
        private NamedPlayerSkinLookupResult? _cachedOtherPlayerLookup;
        private string _cachedOtherPlayerLookupQuery = string.Empty;

        public Array AvailableVariants { get; } = Enum.GetValues(typeof(MinecraftSkinVariant));
        public Array AvailableBackgroundModes { get; } = Enum.GetValues(typeof(PreviewBackgroundMode));
        public Array AvailableAnimationModes { get; } = Enum.GetValues(typeof(PreviewAnimationMode));
        public IReadOnlyList<PanoramaPresetOption> AvailablePanoramaPresets => _availablePanoramaPresets;

        public RelayCommand BrowseSkinCommand { get; }
        public RelayCommand ClearPanoramaCommand { get; }
        public RelayCommand ResetCameraCommand { get; }

        public AsyncRelayCommand RefreshProfileCommand { get; }
        public AsyncRelayCommand FetchPlayerSkinCommand { get; }
        public AsyncRelayCommand ApplyFileSkinCommand { get; }
        public AsyncRelayCommand ApplyUrlSkinCommand { get; }
        public AsyncRelayCommand PreviewOtherPlayerSkinCommand { get; }
        public AsyncRelayCommand DownloadOtherPlayerSkinCommand { get; }
        public AsyncRelayCommand RenameCommand { get; }

        public SkinChangerViewModel(TokenConverterViewModel converter)
        {
            _converter = converter;
            _selectedPanoramaPreset = _availablePanoramaPresets.First(p => p.Key == "old");

            BrowseSkinCommand = new RelayCommand(BrowseSkin, () => !IsBusy);
            ClearPanoramaCommand = new RelayCommand(ClearPanorama, () => !IsBusy);
            ResetCameraCommand = new RelayCommand(() => CameraResetNonce++);

            RefreshProfileCommand = new AsyncRelayCommand(RefreshProfileAsync, CanUseAuthenticatedEndpoints);
            FetchPlayerSkinCommand = new AsyncRelayCommand(FetchPlayerSkinAsync, CanFetchPlayerSkin);
            ApplyFileSkinCommand = new AsyncRelayCommand(ApplyFileSkinAsync, CanApplyFile);
            ApplyUrlSkinCommand = new AsyncRelayCommand(ApplyUrlSkinAsync, CanApplyUrl);
            PreviewOtherPlayerSkinCommand = new AsyncRelayCommand(PreviewOtherPlayerSkinAsync, CanPreviewOtherPlayerSkin);
            DownloadOtherPlayerSkinCommand = new AsyncRelayCommand(DownloadOtherPlayerSkinAsync, CanDownloadOtherPlayerSkin);
            RenameCommand = new AsyncRelayCommand(RenameAsync, CanUseAuthenticatedEndpoints);

            if (_converter is INotifyPropertyChanged npc)
                npc.PropertyChanged += ConverterOnPropertyChanged;
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetField(ref _isBusy, value))
                    NotifyCommandStates();
            }
        }

        public string ProfileName
        {
            get => _profileName;
            set => SetField(ref _profileName, value);
        }

        /// <summary>New IGN entered by the user for the rename action.</summary>
        public string NewName
        {
            get => _newName;
            set => SetField(ref _newName, value);
        }

        public string ProfileId
        {
            get => _profileId;
            set => SetField(ref _profileId, value);
        }

        public string CurrentSkinUrl
        {
            get => _currentSkinUrl;
            set
            {
                if (SetField(ref _currentSkinUrl, value))
                    NotifyCommandStates();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        /// <summary>
        /// After a language switch, reset the idle status line to the localized
        /// prompt. A transient operation message in flight is left untouched.
        /// </summary>
        public void RefreshLocalizedText()
        {
            if (!_converter.LoggedIn || string.IsNullOrWhiteSpace(_converter.AccessToken))
                StatusMessage = Loc.T("Skin.Status.PreviewOrSignIn");

            // Re-raise the option collections so ComboBox items re-run their
            // localized templates/converter with the new language.
            OnPropertyChanged(nameof(AvailableVariants));
            OnPropertyChanged(nameof(AvailableAnimationModes));
            OnPropertyChanged(nameof(AvailableBackgroundModes));
            OnPropertyChanged(nameof(AvailablePanoramaPresets));
        }

        public string? LocalSkinPath
        {
            get => _localSkinPath;
            set
            {
                if (SetField(ref _localSkinPath, value))
                {
                    NotifyCommandStates();
                    _ = LoadPreviewFromLocalFileAsync(value);
                }
            }
        }

        public string? RemoteSkinUrl
        {
            get => _remoteSkinUrl;
            set
            {
                if (SetField(ref _remoteSkinUrl, value))
                    NotifyCommandStates();
            }
        }

        public byte[]? PreviewSkinPng
        {
            get => _previewSkinPng;
            set => SetField(ref _previewSkinPng, value);
        }

        public string? PanoramaSourcePath
        {
            get => _panoramaSourcePath;
            set => SetField(ref _panoramaSourcePath, value);
        }

        public PanoramaPresetOption? SelectedPanoramaPreset
        {
            get => _selectedPanoramaPreset;
            set
            {
                if (!SetField(ref _selectedPanoramaPreset, value) || value == null)
                    return;

                PanoramaSourcePath = value.Key;

                if (SelectedBackgroundMode != PreviewBackgroundMode.Panorama)
                    SelectedBackgroundMode = PreviewBackgroundMode.Panorama;
            }
        }

        public MinecraftSkinVariant SelectedVariant
        {
            get => _selectedVariant;
            set => SetField(ref _selectedVariant, value);
        }

        public PreviewBackgroundMode SelectedBackgroundMode
        {
            get => _selectedBackgroundMode;
            set => SetField(ref _selectedBackgroundMode, value);
        }

        public PreviewAnimationMode SelectedAnimationMode
        {
            get => _selectedAnimationMode;
            set => SetField(ref _selectedAnimationMode, value);
        }

        public int CameraResetNonce
        {
            get => _cameraResetNonce;
            set => SetField(ref _cameraResetNonce, value);
        }

        public string? OtherPlayerName
        {
            get => _otherPlayerName;
            set
            {
                if (SetField(ref _otherPlayerName, value))
                {
                    InvalidateOtherPlayerLookup();
                    NotifyCommandStates();
                }
            }
        }

        public string OtherPlayerResolvedName
        {
            get => _otherPlayerResolvedName;
            set => SetField(ref _otherPlayerResolvedName, value);
        }

        public string OtherPlayerResolvedId
        {
            get => _otherPlayerResolvedId;
            set => SetField(ref _otherPlayerResolvedId, value);
        }

        public string OtherPlayerSkinUrl
        {
            get => _otherPlayerSkinUrl;
            set => SetField(ref _otherPlayerSkinUrl, value);
        }

        public async Task EnsureLoadedAsync()
        {
            if (!_converter.LoggedIn || string.IsNullOrWhiteSpace(_converter.AccessToken))
            {
                ClearProfileState(Loc.T("Skin.Status.PreviewOrSignIn"));
                return;
            }

            if (_loadedOnce && string.Equals(_lastLoadedToken, _converter.AccessToken, StringComparison.Ordinal))
                return;

            _loadedOnce = true;
            await RefreshProfileAsync();
        }

        private void ConverterOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(TokenConverterViewModel.LoggedIn) &&
                e.PropertyName != nameof(TokenConverterViewModel.AccessToken))
                return;

            NotifyCommandStates();

            if (!_converter.LoggedIn || string.IsNullOrWhiteSpace(_converter.AccessToken))
            {
                ClearProfileState(Loc.T("Skin.Status.PreviewOrSignIn"));
                return;
            }

            if (!IsBusy)
                _ = EnsureLoadedAsync();
        }

        private bool CanUseAuthenticatedEndpoints()
            => !IsBusy &&
               _converter.LoggedIn &&
               !string.IsNullOrWhiteSpace(_converter.AccessToken);

        private bool CanFetchPlayerSkin()
            => CanUseAuthenticatedEndpoints() && IsValidWebUrl(CurrentSkinUrl);

        private bool CanApplyFile()
            => CanUseAuthenticatedEndpoints() &&
               !string.IsNullOrWhiteSpace(LocalSkinPath) &&
               File.Exists(LocalSkinPath);

        private bool CanApplyUrl()
            => CanUseAuthenticatedEndpoints() && IsValidWebUrl(RemoteSkinUrl);

        private bool CanPreviewOtherPlayerSkin()
            => !IsBusy && !string.IsNullOrWhiteSpace(OtherPlayerName);

        private bool CanDownloadOtherPlayerSkin()
            => !IsBusy && !string.IsNullOrWhiteSpace(OtherPlayerName);

        private void NotifyCommandStates()
        {
            BrowseSkinCommand.NotifyCanExecuteChanged();
            ClearPanoramaCommand.NotifyCanExecuteChanged();
            RefreshProfileCommand.NotifyCanExecuteChanged();
            FetchPlayerSkinCommand.NotifyCanExecuteChanged();
            ApplyFileSkinCommand.NotifyCanExecuteChanged();
            ApplyUrlSkinCommand.NotifyCanExecuteChanged();
            PreviewOtherPlayerSkinCommand.NotifyCanExecuteChanged();
            DownloadOtherPlayerSkinCommand.NotifyCanExecuteChanged();
            RenameCommand.NotifyCanExecuteChanged();
        }

        private string RequireAccessToken()
        {
            if (!string.IsNullOrWhiteSpace(_converter.AccessToken))
                return _converter.AccessToken;

            throw new InvalidOperationException(
                Loc.T("Skin.Status.NoToken"));
        }

        public async Task RefreshProfileAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = Loc.T("Skin.Status.FetchingProfile");
                await LoadProfileAsync();
                StatusMessage = Loc.T("Skin.Status.ProfileLoaded");
            }
            catch (Exception ex)
            {
                StatusMessage = ToFriendlyError(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task RenameAsync()
        {
            if (IsBusy)
                return;

            string target = (NewName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(target) || target == ProfileName)
            {
                StatusMessage = Loc.T("Rename.Msg.NoChange");
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = Loc.T("Skin.Status.Renaming");

                string token = RequireAccessToken();
                await IGNRenameService.RenameAsync(target, token);

                // Reflect the new name everywhere it is shown.
                _converter.ProfileName = target;
                ProfileName = target;
                NewName = string.Empty;

                StatusMessage = Loc.T("Rename.Msg.Success", target);
            }
            catch (Exception ex)
            {
                StatusMessage = ToFriendlyError(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task FetchPlayerSkinAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = Loc.T("Skin.Status.FetchingSkin");

                bool success = await TryDownloadPreviewSkinAsync(CurrentSkinUrl);
                StatusMessage = success
                    ? Loc.T("Skin.Status.SkinLoaded")
                    : Loc.T("Skin.Status.NoActiveSkin");
            }
            catch (Exception ex)
            {
                StatusMessage = ToFriendlyError(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PreviewOtherPlayerSkinAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = Loc.T("Skin.Status.LookingUp");

                NamedPlayerSkinLookupResult lookup = await ResolveOtherPlayerSkinAsync();

                PreviewSkinPng = await _skinService.DownloadSkinByUrlAsync(lookup.SkinUrl);
                SelectedVariant = lookup.Variant;

                StatusMessage = Loc.T("Skin.Status.LoadedPreview", lookup.Name);
            }
            catch (Exception ex)
            {
                StatusMessage = ToFriendlyError(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DownloadOtherPlayerSkinAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = Loc.T("Skin.Status.Resolving");

                NamedPlayerSkinLookupResult lookup = await ResolveOtherPlayerSkinAsync();

                SaveFileDialog dlg = new()
                {
                    Filter = "PNG image (*.png)|*.png|All files (*.*)|*.*",
                    Title = Loc.T("Skin.Status.SaveTitle"),
                    FileName = $"{SanitizeFileName(lookup.Name)}.png"
                };

                if (dlg.ShowDialog() != true)
                {
                    StatusMessage = Loc.T("Skin.Status.DownloadCancelled");
                    return;
                }

                byte[] png = await _skinService.DownloadSkinByUrlAsync(lookup.SkinUrl);
                await File.WriteAllBytesAsync(dlg.FileName, png);

                StatusMessage = Loc.T("Skin.Status.Saved", lookup.Name);
            }
            catch (Exception ex)
            {
                StatusMessage = ToFriendlyError(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadProfileAsync()
        {
            string token = RequireAccessToken();
            var profile = await _skinService.GetProfileAsync(token);

            _lastLoadedToken = token;

            ProfileName = string.IsNullOrWhiteSpace(profile.Name) ? "-" : profile.Name;
            ProfileId = string.IsNullOrWhiteSpace(profile.Id) ? "-" : profile.Id;

            var activeSkin =
                profile.Skins.FirstOrDefault(s => s.State.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                ?? profile.Skins.FirstOrDefault();

            if (activeSkin == null)
            {
                CurrentSkinUrl = "-";
                PreviewSkinPng = null;
                return;
            }

            CurrentSkinUrl = string.IsNullOrWhiteSpace(activeSkin.Url) ? "-" : activeSkin.Url;
            SelectedVariant = activeSkin.VariantKind;

            try
            {
                bool loaded = await TryDownloadPreviewSkinAsync(activeSkin.Url);
                if (!loaded)
                    PreviewSkinPng = null;
            }
            catch
            {
                PreviewSkinPng = null;
            }
        }

        private async Task ApplyFileSkinAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = Loc.T("Skin.Status.Uploading");

                string token = RequireAccessToken();
                await _skinService.SetSkinFromFileAsync(token, LocalSkinPath!, SelectedVariant);

                PreviewSkinPng = await File.ReadAllBytesAsync(LocalSkinPath!);
                await LoadProfileAsync();

                StatusMessage = Loc.T("Skin.Status.Uploaded");
            }
            catch (Exception ex)
            {
                StatusMessage = ToFriendlyError(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ApplyUrlSkinAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = Loc.T("Skin.Status.ApplyingUrl");

                string token = RequireAccessToken();
                await _skinService.SetSkinFromUrlAsync(token, RemoteSkinUrl!, SelectedVariant);

                try
                {
                    PreviewSkinPng = await _skinService.DownloadSkinByUrlAsync(RemoteSkinUrl!);
                }
                catch
                {
                }

                await LoadProfileAsync();

                StatusMessage = Loc.T("Skin.Status.UrlApplied");
            }
            catch (Exception ex)
            {
                StatusMessage = ToFriendlyError(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void BrowseSkin()
        {
            OpenFileDialog dlg = new()
            {
                Filter = "PNG skin (*.png)|*.png|All files (*.*)|*.*",
                Title = Loc.T("Skin.Status.SelectPng")
            };

            if (dlg.ShowDialog() == true)
                LocalSkinPath = dlg.FileName;
        }

        private void ClearPanorama()
        {
            if (SelectedBackgroundMode == PreviewBackgroundMode.Panorama)
                SelectedBackgroundMode = PreviewBackgroundMode.Bright;
        }

        private async Task LoadPreviewFromLocalFileAsync(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return;

                PreviewSkinPng = await File.ReadAllBytesAsync(path);
            }
            catch
            {
            }
        }

        private async Task<bool> TryDownloadPreviewSkinAsync(string? url)
        {
            if (!IsValidWebUrl(url))
                return false;

            PreviewSkinPng = await _skinService.DownloadSkinByUrlAsync(url!);
            return true;
        }

        private async Task<NamedPlayerSkinLookupResult> ResolveOtherPlayerSkinAsync()
        {
            string requestedName = OtherPlayerName?.Trim() ?? string.Empty;

            if (requestedName.Length == 0)
                throw new InvalidOperationException(Loc.T("Skin.Status.EnterName"));

            if (_cachedOtherPlayerLookup != null &&
                string.Equals(_cachedOtherPlayerLookupQuery, requestedName, StringComparison.OrdinalIgnoreCase))
            {
                return _cachedOtherPlayerLookup;
            }

            NamedPlayerSkinLookupResult lookup =
                await _skinService.LookupPlayerSkinByNameAsync(requestedName);

            _cachedOtherPlayerLookup = lookup;
            _cachedOtherPlayerLookupQuery = requestedName;

            OtherPlayerResolvedName = string.IsNullOrWhiteSpace(lookup.Name) ? "-" : lookup.Name;
            OtherPlayerResolvedId = string.IsNullOrWhiteSpace(lookup.Id) ? "-" : lookup.Id;
            OtherPlayerSkinUrl = string.IsNullOrWhiteSpace(lookup.SkinUrl) ? "-" : lookup.SkinUrl;

            return lookup;
        }

        private void InvalidateOtherPlayerLookup()
        {
            _cachedOtherPlayerLookup = null;
            _cachedOtherPlayerLookupQuery = string.Empty;
            OtherPlayerResolvedName = "-";
            OtherPlayerResolvedId = "-";
            OtherPlayerSkinUrl = "-";
        }

        private void ClearProfileState(string message)
        {
            ProfileName = "-";
            ProfileId = "-";
            CurrentSkinUrl = "-";
            StatusMessage = message;
            _lastLoadedToken = string.Empty;
        }

        private static bool IsValidWebUrl(string? raw)
        {
            if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri? uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private static string SanitizeFileName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "skin";

            char[] invalid = Path.GetInvalidFileNameChars();
            return string.Concat(raw.Select(c => invalid.Contains(c) ? '_' : c));
        }

        private static string ToFriendlyError(Exception ex)
        {
            string m = ex.Message;

            if (m.Contains("401") || m.Contains("403"))
                return Loc.T("Skin.Status.InvalidToken");

            if (m.Contains("429"))
                return Loc.T("Skin.Status.RateLimited");

            if (m.Contains("was not found", StringComparison.OrdinalIgnoreCase))
                return m;

            // These markers are produced via Loc.T at the throw site, so compare
            // against the same localized values to stay language-agnostic.
            if (m == Loc.T("Skin.Status.EnterName"))
                return m;

            if (m == Loc.T("Skin.Status.NoToken"))
                return Loc.T("Skin.Status.SignInFirst");

            return m;
        }
    }
}
