using MaterialDesignThemes.Wpf;

namespace RefreshToAccess2
{
    public class NavItem
    {
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconTooltip { get; set; } = string.Empty;
        public PackIconKind SelectedIcon { get; set; }
        public PackIconKind UnselectedIcon { get; set; }
    }
}