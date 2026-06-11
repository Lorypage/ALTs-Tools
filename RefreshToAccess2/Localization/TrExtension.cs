using System;
using System.Windows.Markup;
using Binding = System.Windows.Data.Binding;
using BindingMode = System.Windows.Data.BindingMode;

namespace RefreshToAccess2.Localization
{
    /// <summary>
    /// XAML markup extension: <c>{loc:Tr KeyName}</c>.
    /// Produces a OneWay binding to <see cref="LocalizationManager.Instance"/>'s
    /// string indexer, so the bound property updates live whenever the language
    /// changes.
    /// </summary>
    public sealed class TrExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public TrExtension() { }

        public TrExtension(string key) => Key = key;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationManager.Instance,
                Mode = BindingMode.OneWay,
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
