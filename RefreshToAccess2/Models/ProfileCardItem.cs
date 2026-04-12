using RefreshToAccess2.ViewModels;
using System;

namespace RefreshToAccess2.Models
{
    public class ProfileCardItem : ViewModelBase
    {
        public ProfileDataBlock Block { get; }

        private bool _isSelected;
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

        public event EventHandler? SelectionChanged;

        public string IGN         => Block.profileData?.IGN ?? "Unknown";
        public string LoginDate   => Block.loginDate ?? "N/A";
        public string UUID        => Block.profileData?.UUID ?? "";
        public string UUIDShort   => Trunc(Block.profileData?.UUID, 20);
        public string ClientId    => Block.profileData?.ClientId ?? "";
        public string RefToken    => Block.profileData?.RefToken ?? "";
        public string RefShort    => Trunc(Block.profileData?.RefToken, 28);
        public string AccToken    => Block.profileData?.AccToken ?? "";
        public string AccShort    => Trunc(Block.profileData?.AccToken, 28);

        public string Initial =>
            !string.IsNullOrEmpty(Block.profileData?.IGN)
                ? Block.profileData!.IGN[0].ToString().ToUpper()
                : "?";

        public ProfileCardItem(ProfileDataBlock block) => Block = block;

        private static string Trunc(string? v, int n)
        {
            if (string.IsNullOrEmpty(v)) return "N/A";
            return v.Length > n ? v[..n] + "…" : v;
        }
    }
}
