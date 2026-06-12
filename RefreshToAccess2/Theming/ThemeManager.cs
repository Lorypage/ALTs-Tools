using System;
using System.Collections.Generic;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using RefreshToAccess2.Services;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace RefreshToAccess2.Theming
{
    /// <summary>
    /// Runtime appearance manager (Material You-style). Controls the base theme
    /// (light/dark) and the primary accent color via MaterialDesign's
    /// <see cref="PaletteHelper"/>. Persists choices to the registry and applies
    /// them instantly — no restart.
    /// </summary>
    public sealed class ThemeManager
    {
        private const string DarkKey    = "ThemeDark";
        private const string ColorKey   = "ThemeColor";
        private const string DynamicKey = "ThemeDynamic";

        public static ThemeManager Instance { get; } = new();

        /// <summary>
        /// Raised after the theme (base/accent) has been applied. The custom
        /// title bar listens to this to re-colour the DWM caption/border, which
        /// otherwise keeps the colour it was given at startup.
        /// </summary>
        public event EventHandler? ThemeChanged;

        private readonly PaletteHelper _palette = new();

        private bool _isDark;
        private string _accentHex = DefaultAccent;
        private bool _dynamic;

        public const string DefaultAccent = "#6750A4"; // Material You purple

        /// <summary>Selectable accent swatches (name + hex), Material You-ish.</summary>
        public static IReadOnlyList<AccentColor> Accents { get; } = new[]
        {
            new AccentColor("Purple", "#6750A4"),
            new AccentColor("Indigo", "#3F51B5"),
            new AccentColor("Blue",   "#1976D2"),
            new AccentColor("Teal",   "#00897B"),
            new AccentColor("Green",  "#43A047"),
            new AccentColor("Lime",   "#AFB42B"),
            new AccentColor("Amber",  "#FF8F00"),
            new AccentColor("Orange", "#F4511E"),
            new AccentColor("Red",    "#E53935"),
            new AccentColor("Pink",   "#D81B60"),
        };

        private ThemeManager() { }

        public bool IsDark => _isDark;
        public string AccentHex => _accentHex;

        /// <summary>True when the accent is derived from the desktop wallpaper.</summary>
        public bool IsDynamic => _dynamic;

        /// <summary>Reads saved appearance from the registry and applies it. Call once at startup.</summary>
        public void Initialize()
        {
            _isDark = RegistryService.Read(DarkKey) == "1";
            _dynamic = RegistryService.Read(DynamicKey) == "1";

            string savedColor = RegistryService.Read(ColorKey);
            _accentHex = string.IsNullOrWhiteSpace(savedColor) ? DefaultAccent : savedColor;

            Apply();
        }

        public void SetDarkMode(bool dark)
        {
            if (_isDark == dark) return;
            _isDark = dark;
            RegistryService.Write(DarkKey, dark ? "1" : "0");
            Apply();
        }

        /// <summary>Picks a fixed preset accent — turns dynamic mode off.</summary>
        public void SetAccent(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return;
            if (!_dynamic && _accentHex == hex) return;

            _dynamic = false;
            _accentHex = hex;
            RegistryService.Write(DynamicKey, "0");
            RegistryService.Write(ColorKey, hex);
            Apply();
        }

        /// <summary>Enables wallpaper-derived dynamic theming (Material You style).</summary>
        public void EnableDynamic()
        {
            _dynamic = true;
            RegistryService.Write(DynamicKey, "1");
            Apply();
        }

        /// <summary>Re-reads the wallpaper and re-applies, if in dynamic mode.</summary>
        public void RefreshDynamic()
        {
            if (_dynamic) Apply();
        }

        private void Apply()
        {
            var theme = _palette.GetTheme();

            theme.SetBaseTheme(_isDark ? BaseTheme.Dark : BaseTheme.Light);

            Color accent;
            if (_dynamic)
            {
                accent = WallpaperColorService.TryGetSeedColor()
                         ?? (Color)ColorConverter.ConvertFromString(DefaultAccent);
            }
            else
            {
                try { accent = (Color)ColorConverter.ConvertFromString(_accentHex); }
                catch { accent = (Color)ColorConverter.ConvertFromString(DefaultAccent); }
            }

            theme.SetPrimaryColor(accent);
            theme.SetSecondaryColor(accent);

            _palette.SetTheme(theme);

            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>A selectable accent swatch.</summary>
    public sealed class AccentColor
    {
        public string Name { get; }
        public string Hex { get; }

        public AccentColor(string name, string hex)
        {
            Name = name;
            Hex = hex;
        }
    }
}
