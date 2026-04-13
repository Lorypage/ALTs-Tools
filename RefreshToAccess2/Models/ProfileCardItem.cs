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

        public ProfileCardItem(ProfileDataBlock block) => Block = block;

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
