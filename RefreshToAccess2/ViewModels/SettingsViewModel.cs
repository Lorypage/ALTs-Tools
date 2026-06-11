using System.Collections.Generic;
using RefreshToAccess2.Localization;
using RefreshToAccess2.Theming;

namespace RefreshToAccess2.ViewModels
{
    /// <summary>
    /// One selectable UI language. <see cref="Code"/> is the culture code stored
    /// in the registry; <see cref="DisplayName"/> is shown in the combo box.
    /// </summary>
    public sealed class LanguageOption
    {
        public string Code { get; init; } = "";
        public string DisplayName { get; init; } = "";
    }

    public sealed class SettingsViewModel : ViewModelBase
    {
        public IReadOnlyList<LanguageOption> AvailableLanguages { get; } = new[]
        {
            new LanguageOption { Code = LocalizationManager.English, DisplayName = "English" },
            new LanguageOption { Code = LocalizationManager.Chinese, DisplayName = "简体中文" },
        };

        private LanguageOption _selectedLanguage;
        private bool _isDarkMode;
        private AccentColor _selectedAccent;
        private bool _isDynamic;

        public SettingsViewModel()
        {
            string current = LocalizationManager.Instance.CurrentLanguage;
            _selectedLanguage = FindByCode(current);

            _isDarkMode = ThemeManager.Instance.IsDark;
            _selectedAccent = FindAccent(ThemeManager.Instance.AccentHex);
            _isDynamic = ThemeManager.Instance.IsDynamic;
        }

        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (value == null || !SetField(ref _selectedLanguage, value))
                    return;

                LocalizationManager.Instance.CurrentLanguage = value.Code;
            }
        }

        private LanguageOption FindByCode(string code)
        {
            foreach (var lang in AvailableLanguages)
                if (lang.Code == code) return lang;
            return AvailableLanguages[0];
        }

        // ── Appearance ─────────────────────────────────────────────

        /// <summary>Selectable accent swatches.</summary>
        public IReadOnlyList<AccentColor> AvailableAccents => ThemeManager.Accents;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (!SetField(ref _isDarkMode, value)) return;
                ThemeManager.Instance.SetDarkMode(value);
            }
        }

        public AccentColor SelectedAccent
        {
            get => _selectedAccent;
            set
            {
                if (value == null || !SetField(ref _selectedAccent, value)) return;
                ThemeManager.Instance.SetAccent(value.Hex);
                // Picking a preset turns dynamic off.
                if (_isDynamic)
                {
                    _isDynamic = false;
                    OnPropertyChanged(nameof(IsDynamic));
                }
            }
        }

        /// <summary>Material You-style wallpaper-derived dynamic color.</summary>
        public bool IsDynamic
        {
            get => _isDynamic;
            set
            {
                if (!SetField(ref _isDynamic, value)) return;
                if (value)
                    ThemeManager.Instance.EnableDynamic();
                else
                    // Turning dynamic off reverts to the selected preset.
                    ThemeManager.Instance.SetAccent(_selectedAccent.Hex);
            }
        }

        private AccentColor FindAccent(string hex)
        {
            foreach (var a in ThemeManager.Accents)
                if (string.Equals(a.Hex, hex, System.StringComparison.OrdinalIgnoreCase))
                    return a;
            return ThemeManager.Accents[0];
        }
    }
}
