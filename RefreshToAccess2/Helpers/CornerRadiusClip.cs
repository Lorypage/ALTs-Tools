using System.Windows;
using System.Windows.Media;

namespace RefreshToAccess2.Helpers
{
    /// <summary>
    /// Lightweight attached property that clips a FrameworkElement
    /// to a rounded rectangle matching its actual size.
    /// Replaces the heavy OpacityMask+VisualBrush approach.
    /// </summary>
    public static class CornerRadiusClip
    {
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.RegisterAttached(
                "Radius", typeof(double), typeof(CornerRadiusClip),
                new PropertyMetadata(0.0, OnChanged));

        public static double GetRadius(DependencyObject obj) =>
            (double)obj.GetValue(RadiusProperty);

        public static void SetRadius(DependencyObject obj, double value) =>
            obj.SetValue(RadiusProperty, value);

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;

            fe.SizeChanged -= OnSizeChanged;

            if ((double)e.NewValue > 0)
            {
                fe.SizeChanged += OnSizeChanged;
                UpdateClip(fe);
            }
            else
            {
                fe.Clip = null;
            }
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e) =>
            UpdateClip((FrameworkElement)sender);

        private static void UpdateClip(FrameworkElement fe)
        {
            double r = GetRadius(fe);
            double w = fe.ActualWidth;
            double h = fe.ActualHeight;

            if (w <= 0 || h <= 0) return;

            // Skip if geometry already matches
            if (fe.Clip is RectangleGeometry existing
                && existing.RadiusX == r
                && existing.Rect.Width == w
                && existing.Rect.Height == h)
                return;

            var geo = new RectangleGeometry(new Rect(0, 0, w, h), r, r);
            geo.Freeze(); // Frozen = no change tracking overhead
            fe.Clip = geo;
        }
    }
}
