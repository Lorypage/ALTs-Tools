using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using RefreshToAccess2.Services;

namespace RefreshToAccess2.Localization
{
    /// <summary>
    /// Runtime localization core. Singleton exposing a string indexer that
    /// XAML binds to (via <see cref="TrExtension"/>) and C# reads via
    /// <see cref="Loc"/>. Switching <see cref="CurrentLanguage"/> raises
    /// PropertyChanged for the indexer so every bound element refreshes
    /// without an app restart.
    /// </summary>
    public sealed class LocalizationManager : INotifyPropertyChanged
    {
        public const string English = "en";
        public const string Chinese = "zh-CN";

        private const string RegistryKey = "Language";

        public static LocalizationManager Instance { get; } = new();

        private readonly Dictionary<string, Dictionary<string, string>> _tables;
        private string _currentLanguage = English;

        public event PropertyChangedEventHandler? PropertyChanged;

        private LocalizationManager()
        {
            _tables = new Dictionary<string, Dictionary<string, string>>
            {
                [English] = Strings.En,
                [Chinese] = Strings.Zh,
            };
        }

        /// <summary>
        /// Resolves the initial language from the registry, falling back to the
        /// OS UI culture. Call once at startup before the first window renders.
        /// </summary>
        public void Initialize()
        {
            string saved = RegistryService.Read(RegistryKey);

            if (!string.IsNullOrEmpty(saved) && _tables.ContainsKey(saved))
            {
                _currentLanguage = saved;
                return;
            }

            // No saved preference — guess from OS culture.
            string ui = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            _currentLanguage = ui.Equals("zh", System.StringComparison.OrdinalIgnoreCase)
                ? Chinese
                : English;
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage == value || !_tables.ContainsKey(value))
                    return;

                _currentLanguage = value;
                RegistryService.Write(RegistryKey, value);

                // Refresh every {loc:Tr} binding and the language property itself.
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
                LanguageChanged?.Invoke();
            }
        }

        /// <summary>Raised after the language changes — used to refresh non-binding text.</summary>
        public event System.Action? LanguageChanged;

        /// <summary>
        /// Looks up <paramref name="key"/> in the current language table,
        /// falling back to English, then to the key itself.
        /// </summary>
        public string this[string key]
        {
            get
            {
                if (_tables.TryGetValue(_currentLanguage, out var table)
                    && table.TryGetValue(key, out var value))
                    return value;

                if (_tables[English].TryGetValue(key, out var fallback))
                    return fallback;

                return key;
            }
        }
    }
}
