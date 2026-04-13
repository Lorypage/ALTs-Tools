using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace RefreshToAccess2.Helpers
{
    /// <summary>
    /// Walks the ListBoxItem visual tree, finds each md:Ripple,
    /// then clips its nearest rounded-corner ancestor Border.
    /// This clips EVERYTHING inside the pill (ripple + hover overlay)
    /// without touching the ListBoxItem's own layout.
    /// </summary>
    public static class NavRippleClip
    {
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.RegisterAttached(
                "Radius", typeof(double), typeof(NavRippleClip),
                new PropertyMetadata(0.0, OnChanged));

        public static double GetRadius(DependencyObject d) =>
            (double)d.GetValue(RadiusProperty);
        public static void SetRadius(DependencyObject d, double v) =>
            d.SetValue(RadiusProperty, v);

        private static void OnChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;
            double r = (double)e.NewValue;

            fe.Loaded -= OnLoaded;
            if (r > 0)
            {
                fe.Loaded += OnLoaded;
                if (fe.IsLoaded)
                    fe.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                        new Action(() => ApplyToTree(fe, r)));
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            double r = GetRadius(fe);
            // Delay so template is fully built
            fe.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                new Action(() => ApplyToTree(fe, r)));
        }

        private static void ApplyToTree(DependencyObject parent, double fallback)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is MaterialDesignThemes.Wpf.Ripple &&
                    child is FrameworkElement rfe)
                {
                    // Walk UP from the Ripple to find the pill Border
                    Border? pill = FindRoundedAncestor(rfe);
                    if (pill != null)
                    {
                        // Clip the pill — clips everything inside:
                        // ripple circle, hover overlay, selection fill
                        CornerRadiusClip.SetRadius(pill, pill.CornerRadius.TopLeft);
                    }
                    else
                    {
                        // Fallback: clip the Ripple itself
                        CornerRadiusClip.SetRadius(rfe, fallback);
                    }
                    // Don't recurse into Ripple children
                    continue;
                }

                ApplyToTree(child, fallback);
            }
        }

        /// <summary>
        /// Walks up the visual tree from the Ripple to find the
        /// nearest Border with CornerRadius > 0 (the pill indicator).
        /// Stops at the ListBoxItem boundary.
        /// </summary>
        private static Border? FindRoundedAncestor(DependencyObject child)
        {
            var p = VisualTreeHelper.GetParent(child);
            while (p != null)
            {
                if (p is Border b && b.CornerRadius.TopLeft > 0)
                    return b;
                if (p is ListBoxItem)
                    break; // Don't go beyond the item
                p = VisualTreeHelper.GetParent(p);
            }
            return null;
        }
    }
}
