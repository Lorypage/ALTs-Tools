using RefreshToAccess2.Localization;
using RefreshToAccess2.Services;
using RefreshToAccess2.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RefreshToAccess2.Models
{
    public class ProfileCardItem : ViewModelBase
    {
        public ProfileDataBlock Block { get; }

        private bool _isSelected;
        private BitmapImage? _headImage;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                SetField(ref _isSelected, value);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public BitmapImage? HeadImage
        {
            get => _headImage;
            private set
            {
                SetField(ref _headImage, value);
                OnPropertyChanged(nameof(HasHeadImage));
            }
        }

        public bool HasHeadImage => _headImage != null;

        public event EventHandler? SelectionChanged;

        /// <summary>
        /// Raised after headSkinBase64 is written into the Block,
        /// signalling that the profile list should be persisted.
        /// </summary>
        public event EventHandler? HeadUpdated;

        public string IGN        => Block.profileData?.IGN ?? "Unknown";
        public string LoginDate  => Block.loginDate ?? "N/A";
        public string UUID       => Block.profileData?.UUID ?? "";
        public string UUIDShort  => Trunc(Block.profileData?.UUID, 20);
        public string ClientId   => Block.profileData?.ClientId ?? "";
        public string RefToken   => Block.profileData?.RefToken ?? "";
        public string RefShort   => Trunc(Block.profileData?.RefToken, 28);
        public string AccToken   => Block.profileData?.AccToken ?? "";
        public string AccShort   => Trunc(Block.profileData?.AccToken, 28);

        public string Initial =>
            !string.IsNullOrEmpty(Block.profileData?.IGN)
                ? Block.profileData!.IGN[0].ToString().ToUpper()
                : "?";

        // ── Hypixel ban status ─────────────────────────────────────
        private bool _isCheckingBan;

        /// <summary>True while a ban check is running (drives the card spinner).</summary>
        public bool IsCheckingBan
        {
            get => _isCheckingBan;
            set { SetField(ref _isCheckingBan, value); OnPropertyChanged(nameof(ShowBanBadge)); }
        }

        public string? BanStatus => Block.banStatus;

        /// <summary>True once this account has been checked at least once.</summary>
        public bool HasBanInfo => !string.IsNullOrEmpty(Block.banStatus);

        /// <summary>Show the badge only when checked and not currently re-checking.</summary>
        public bool ShowBanBadge => HasBanInfo && !_isCheckingBan;

        public bool IsBanned =>
            Block.banStatus is HypixelBanService.StatusTemp or HypixelBanService.StatusPerm;

        /// <summary>Localized badge caption for the current ban status.</summary>
        public string BanBadgeText => Block.banStatus switch
        {
            HypixelBanService.StatusClean => Loc.T("AltMgr.BanClean"),
            HypixelBanService.StatusTemp  => Loc.T("AltMgr.BanTemp", Block.banDaysLeft ?? 0),
            HypixelBanService.StatusPerm  => Loc.T("AltMgr.BanPerm"),
            HypixelBanService.StatusError => Loc.T("AltMgr.BanError"),
            HypixelBanService.StatusUnknown => Loc.T("AltMgr.BanUnknown"),
            _ => ""
        };

        /// <summary>Semantic color key: "clean" | "temp" | "perm" | "neutral".</summary>
        public string BanBadgeKind => Block.banStatus switch
        {
            HypixelBanService.StatusClean => "clean",
            HypixelBanService.StatusTemp  => "temp",
            HypixelBanService.StatusPerm  => "perm",
            _ => "neutral"
        };

        public string BanLastCheckedText
        {
            get
            {
                if (string.IsNullOrEmpty(Block.banCheckedUtc)) return "";
                return DateTime.TryParse(Block.banCheckedUtc, out var dt)
                    ? Loc.T("AltMgr.BanLastChecked", dt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"))
                    : "";
            }
        }

        /// <summary>Raw server ban/disconnect text; shown in the detail overlay for banned accounts.</summary>
        public string BanRawMessage => Block.banRawMessage ?? "";

        /// <summary>Show the raw-message block only when banned and we captured text.</summary>
        public bool HasBanRawMessage => IsBanned && !string.IsNullOrWhiteSpace(Block.banRawMessage);

        // ── Hypixel stats (fetched on demand, not persisted) ───────
        private HypixelStats? _stats;
        private bool _isLoadingStats;
        private string? _statsError;

        /// <summary>Loaded Hypixel stats, or null until fetched.</summary>
        public HypixelStats? Stats
        {
            get => _stats;
            private set
            {
                SetField(ref _stats, value);
                OnPropertyChanged(nameof(HasStats));
            }
        }

        public bool HasStats => _stats != null && _statsError == null;

        public bool IsLoadingStats
        {
            get => _isLoadingStats;
            private set => SetField(ref _isLoadingStats, value);
        }

        public string? StatsError
        {
            get => _statsError;
            private set
            {
                SetField(ref _statsError, value);
                OnPropertyChanged(nameof(HasStatsError));
                OnPropertyChanged(nameof(HasStats));
            }
        }

        public bool HasStatsError => !string.IsNullOrEmpty(_statsError);

        public ProfileCardItem(ProfileDataBlock block) => Block = block;

        /// <summary>
        /// Fetches Hypixel stats for this account from the khadow.lol API and exposes
        /// them via <see cref="Stats"/>. Safe to call repeatedly; sets loading / error
        /// state for the UI. Never throws.
        /// </summary>
        public async Task LoadStatsAsync(bool force = false)
        {
            if (_isLoadingStats) return;
            if (Stats != null && !force) return;

            IsLoadingStats = true;
            StatsError = null;
            try
            {
                var stats = await HypixelStatsService.FetchAsync(IGN);
                StatsError = stats.Status switch
                {
                    "not_found"   => Loc.T("AltMgr.StatsNotFound"),
                    "ratelimited" => Loc.T("AltMgr.StatsRateLimited"),
                    "error"       => Loc.T("AltMgr.StatsFetchError"),
                    _             => null
                };
                Stats = stats;
            }
            catch (Exception ex)
            {
                StatsError = ex.Message;
                Stats = null;
            }
            finally
            {
                IsLoadingStats = false;
            }
        }

        /// <summary>Re-raises ban-related display props after a check writes into the block.</summary>
        public void RaiseBanChanged()
        {
            OnPropertyChanged(nameof(BanStatus));
            OnPropertyChanged(nameof(HasBanInfo));
            OnPropertyChanged(nameof(ShowBanBadge));
            OnPropertyChanged(nameof(IsBanned));
            OnPropertyChanged(nameof(BanBadgeText));
            OnPropertyChanged(nameof(BanBadgeKind));
            OnPropertyChanged(nameof(BanLastCheckedText));
            OnPropertyChanged(nameof(BanRawMessage));
            OnPropertyChanged(nameof(HasBanRawMessage));
        }

        /// <summary>
        /// Re-raises all computed display properties. Call after the underlying
        /// <see cref="Block"/> data changes (e.g. after a token refresh / login)
        /// so the card and detail overlay reflect the new values.
        /// </summary>
        public void RaiseAllChanged()
        {
            OnPropertyChanged(nameof(IGN));
            OnPropertyChanged(nameof(LoginDate));
            OnPropertyChanged(nameof(UUID));
            OnPropertyChanged(nameof(UUIDShort));
            OnPropertyChanged(nameof(ClientId));
            OnPropertyChanged(nameof(RefToken));
            OnPropertyChanged(nameof(RefShort));
            OnPropertyChanged(nameof(AccToken));
            OnPropertyChanged(nameof(AccShort));
            OnPropertyChanged(nameof(Initial));
            RaiseBanChanged();
        }

        /// <summary>
        /// Load head from base64 cache in the profile block,
        /// or fetch from Mojang if missing. Non-blocking.
        /// </summary>
        public async Task LoadHeadAsync(bool force = false)
        {
            try
            {
                string? before = Block.headSkinBase64;

                var img = await HeadSkinCacheService.GetHeadAsync(Block, force);
                HeadImage = img;

                // If the base64 was written/changed, signal a save is needed
                if (img != null && Block.headSkinBase64 != before)
                    HeadUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch { /* fallback to initial letter */ }
        }

        /// <summary>
        /// Invalidate cached head and re-fetch from Mojang.
        /// Call after token refresh or skin change.
        /// </summary>
        public async Task RefreshHeadAsync()
        {
            HeadSkinCacheService.Invalidate(Block);
            await LoadHeadAsync(force: true);
        }

        private static string Trunc(string? v, int n)
        {
            if (string.IsNullOrEmpty(v)) return "N/A";
            return v.Length > n ? v[..n] + "…" : v;
        }
    }
}
