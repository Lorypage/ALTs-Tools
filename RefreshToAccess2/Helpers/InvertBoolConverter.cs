using System;
using System.Globalization;
using System.Windows.Data;

namespace RefreshToAccess2.Helpers
{
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b ? !b : value;

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => value is bool b ? !b : value;
    }
}
