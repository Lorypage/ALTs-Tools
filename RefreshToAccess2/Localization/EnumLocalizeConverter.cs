using System;
using System.Globalization;
using System.Windows.Data;
using RefreshToAccess2.Models;
using Binding = System.Windows.Data.Binding;
using IValueConverter = System.Windows.Data.IValueConverter;

namespace RefreshToAccess2.Localization
{
    /// <summary>
    /// Converts a skin-related enum value to its localized display string.
    /// Used by ComboBoxes bound directly to enum collections. Returns a OneWay
    /// display string; the bound SelectedItem still round-trips the enum value.
    /// </summary>
    public sealed class EnumLocalizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = value switch
            {
                MinecraftSkinVariant v => $"Skin.Variant.{v}",
                PreviewAnimationMode a => $"Skin.Anim.{a}",
                PreviewBackgroundMode b => $"Skin.Bg.{b}",
                _ => value?.ToString() ?? string.Empty
            };

            return LocalizationManager.Instance[key];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
