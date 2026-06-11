using System.ComponentModel;
using MaterialDesignThemes.Wpf;
using RefreshToAccess2.Localization;

namespace RefreshToAccess2
{
    public class NavItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public NavItem()
        {
            // Refresh displayed text when the language changes at runtime.
            LocalizationManager.Instance.LanguageChanged += () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconTooltip)));
            };
        }

        /// <summary>Localization key for the nav label.</summary>
        public string LabelKey { get; set; } = string.Empty;

        /// <summary>Localization key for the tooltip.</summary>
        public string TooltipKey { get; set; } = string.Empty;

        /// <summary>Optional localization key for the description.</summary>
        public string DescriptionKey { get; set; } = string.Empty;

        public string Label => string.IsNullOrEmpty(LabelKey) ? string.Empty : Loc.T(LabelKey);
        public string IconTooltip => string.IsNullOrEmpty(TooltipKey) ? string.Empty : Loc.T(TooltipKey);
        public string Description => string.IsNullOrEmpty(DescriptionKey) ? string.Empty : Loc.T(DescriptionKey);

        public PackIconKind SelectedIcon { get; set; }
        public PackIconKind UnselectedIcon { get; set; }
    }
}
