using RefreshToAccess2.Helpers;
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
        public sealed record PanoramaPresetOption(string Key, string DisplayName);

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
        private string _statusMessage = "Ready.";

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
                ClearProfileState("Preview other players freely, or sign in to manage your own account skin.");
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
                ClearProfileState("Preview other players freely, or sign in to manage your own account skin.");
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
        }

        private string RequireAccessToken()
        {
            if (!string.IsNullOrWhiteSpace(_converter.AccessToken))
                return _converter.AccessToken;

            throw new InvalidOperationException(
                "No Minecraft access token is available. Convert a refresh token first.");
        }

        public async Task RefreshProfileAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Fetching current Minecraft profile...";
                await LoadProfileAsync();
                StatusMessage = "Profile loaded.";
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
                StatusMessage = "Fetching player skin...";

                bool success = await TryDownloadPreviewSkinAsync(CurrentSkinUrl);
                StatusMessage = success
                    ? "Player skin loaded."
                    : "No active player skin URL is available.";
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
                StatusMessage = "Looking up player skin...";

                NamedPlayerSkinLookupResult lookup = await ResolveOtherPlayerSkinAsync();

                PreviewSkinPng = await _skinService.DownloadSkinByUrlAsync(lookup.SkinUrl);
                SelectedVariant = lookup.Variant;

                StatusMessage = $"Loaded {lookup.Name}'s skin preview.";
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
                StatusMessage = "Resolving player skin...";

                NamedPlayerSkinLookupResult lookup = await ResolveOtherPlayerSkinAsync();

                SaveFileDialog dlg = new()
                {
                    Filter = "PNG image (*.png)|*.png|All files (*.*)|*.*",
                    Title = "Save player skin",
                    FileName = $"{SanitizeFileName(lookup.Name)}.png"
                };

                if (dlg.ShowDialog() != true)
                {
                    StatusMessage = "Download cancelled.";
                    return;
                }

                byte[] png = await _skinService.DownloadSkinByUrlAsync(lookup.SkinUrl);
                await File.WriteAllBytesAsync(dlg.FileName, png);

                StatusMessage = $"Saved {lookup.Name}'s skin.";
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
                StatusMessage = "Uploading skin file...";

                string token = RequireAccessToken();
                await _skinService.SetSkinFromFileAsync(token, LocalSkinPath!, SelectedVariant);

                PreviewSkinPng = await File.ReadAllBytesAsync(LocalSkinPath!);
                await LoadProfileAsync();

                StatusMessage = "Skin uploaded successfully.";
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
                StatusMessage = "Applying skin from URL...";

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

                StatusMessage = "Skin URL applied successfully.";
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
                Title = "Select Minecraft Skin PNG"
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
                throw new InvalidOperationException("Enter a Minecraft player name.");

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
                return "The Minecraft access token is invalid or expired. Convert the refresh token again.";

            if (m.Contains("429"))
                return "Minecraft Services is rate-limiting this account right now. Wait a moment and try again.";

            if (m.Contains("was not found", StringComparison.OrdinalIgnoreCase))
                return m;

            if (m.Contains("Enter a Minecraft player name", StringComparison.OrdinalIgnoreCase))
                return m;

            if (m.Contains("No Minecraft access token", StringComparison.OrdinalIgnoreCase))
                return "Sign in first to manage your own account skin. Previewing other players works without login.";

            return m;
        }
    }
}
