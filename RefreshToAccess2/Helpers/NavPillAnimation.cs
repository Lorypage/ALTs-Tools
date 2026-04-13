using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace RefreshToAccess2.Helpers
{
    public static class NavPillAnimation
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(NavPillAnimation),
                new PropertyMetadata(false, OnEnabledChanged));

        public static bool GetIsEnabled(DependencyObject d) => (bool)d.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject d, bool v) => d.SetValue(IsEnabledProperty, v);

        private static readonly DependencyProperty PillRefProperty =
            DependencyProperty.RegisterAttached(
                "PillRef", typeof(FrameworkElement), typeof(NavPillAnimation));

        // Track whether an animation is active so we can cancel cleanly
        private static readonly DependencyProperty IsAnimatingProperty =
            DependencyProperty.RegisterAttached(
                "IsAnimating", typeof(bool), typeof(NavPillAnimation),
                new PropertyMetadata(false));

        private static readonly CubicEase _easeOut =
            new() { EasingMode = EasingMode.EaseOut };

        // ── Wiring ─────────────────────────────────────────────────

        private static void OnEnabledChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBoxItem item) return;
            if ((bool)e.NewValue)
            {
                item.Loaded += OnLoaded;
                item.Selected += OnSelected;
                item.Unselected += OnUnselected;
            }
            else
            {
                item.Loaded -= OnLoaded;
                item.Selected -= OnSelected;
                item.Unselected -= OnUnselected;
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ListBoxItem item) return;
            item.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                new Action(() => SetupPill(item)));
        }

        private static void OnSelected(object sender, RoutedEventArgs e)
        {
            if (sender is not ListBoxItem item) return;

            // Immediately cancel any running animation and hide flash
            var pill = item.GetValue(PillRefProperty) as FrameworkElement;
            if (pill != null)
            {
                CancelRunning(item, pill);
                HoldOpacity(pill);
            }

            item.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    var p = GetOrFindPill(item);
                    if (p == null) return;
                    HoldOpacity(p);
                    AnimateScale(item, p, true);
                }));
        }

        private static void OnUnselected(object sender, RoutedEventArgs e)
        {
            if (sender is not ListBoxItem item) return;

            // Immediately cancel any running animation
            var pill = item.GetValue(PillRefProperty) as FrameworkElement;
            if (pill != null)
            {
                CancelRunning(item, pill);
                HoldOpacity(pill);
            }

            item.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    var p = GetOrFindPill(item);
                    if (p == null) return;
                    HoldOpacity(p);
                    AnimateScale(item, p, false);
                }));
        }

        // ── Setup ──────────────────────────────────────────────────

        private static void SetupPill(ListBoxItem item)
        {
            var pill = FindPill(item);
            if (pill == null) return;

            item.SetValue(PillRefProperty, pill);

            var scale = EnsureScale(pill);
            HoldOpacity(pill);

            // Set initial state without animation
            ClearScaleAnims(scale);
            double v = item.IsSelected ? 1 : 0;
            scale.ScaleX = v;
            scale.ScaleY = v;
        }

        // ── Cancel ─────────────────────────────────────────────────

        private static void CancelRunning(ListBoxItem item, FrameworkElement pill)
        {
            bool wasAnimating = (bool)item.GetValue(IsAnimatingProperty);
            if (!wasAnimating) return;

            item.SetValue(IsAnimatingProperty, false);

            if (pill.RenderTransform is ScaleTransform scale)
            {
                // Snapshot current animated value, then clear animation
                double curX = scale.ScaleX;
                double curY = scale.ScaleY;
                ClearScaleAnims(scale);
                scale.ScaleX = curX;
                scale.ScaleY = curY;
            }
        }

        // ── Animation ──────────────────────────────────────────────

        private static void HoldOpacity(FrameworkElement pill)
        {
            pill.BeginAnimation(UIElement.OpacityProperty, null);
            pill.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(1, TimeSpan.Zero)
                { FillBehavior = FillBehavior.HoldEnd });
        }

        private static void AnimateScale(
            ListBoxItem item, FrameworkElement pill, bool zoomIn)
        {
            var scale = EnsureScale(pill);

            // Snapshot current value for smooth transition from interrupted state
            double currentX = scale.ScaleX;
            ClearScaleAnims(scale);

            item.SetValue(IsAnimatingProperty, true);

            var dur = TimeSpan.FromMilliseconds(zoomIn ? 380 : 260);
            IEasingFunction ease = zoomIn
                ? new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.2 }
                : _easeOut;

            // Animate from wherever it currently is
            double from = zoomIn
                ? Math.Min(currentX, 0.3)  // Don't start above 0.3 for zoom-in
                : currentX;                 // Start from current for zoom-out
            double target = zoomIn ? 1.0 : 0.0;

            var sx = new DoubleAnimation(from, target, dur) { EasingFunction = ease };
            var sy = new DoubleAnimation(from, target, dur) { EasingFunction = ease };

            sy.Completed += (_, __) =>
            {
                item.SetValue(IsAnimatingProperty, false);
                ClearScaleAnims(scale);
                scale.ScaleX = target;
                scale.ScaleY = target;
                HoldOpacity(pill);
            };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
        }

        private static void ClearScaleAnims(ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }

        // ── Helpers ────────────────────────────────────────────────

        private static FrameworkElement? GetOrFindPill(ListBoxItem item)
        {
            var pill = item.GetValue(PillRefProperty) as FrameworkElement;
            if (pill != null) return pill;
            SetupPill(item);
            return item.GetValue(PillRefProperty) as FrameworkElement;
        }

        private static ScaleTransform EnsureScale(FrameworkElement fe)
        {
            if (fe.RenderTransform is ScaleTransform existing)
                return existing;
            var s = new ScaleTransform(1, 1);
            fe.RenderTransform = s;
            fe.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            return s;
        }

        private static FrameworkElement? FindPill(ListBoxItem item)
        {
            string[] names =
            {
                "SelectedIndicator",
                "SelectionIndicator",
                "SelectionIndicatorBorder",
                "IndicatorBorder",
                "PART_Indicator",
                "PART_SelectionIndicator"
            };

            foreach (var name in names)
            {
                if (item.Template?.FindName(name, item) is FrameworkElement fe)
                    return fe;
            }

            return FindPillByTree(item);
        }

        private static FrameworkElement? FindPillByTree(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Border b &&
                    b.CornerRadius.TopLeft >= 14 &&
                    HasVisibleBackground(b.Background))
                    return b;

                var result = FindPillByTree(child);
                if (result != null) return result;
            }
            return null;
        }

        private static bool HasVisibleBackground(Brush? brush)
        {
            if (brush == null) return false;
            if (ReferenceEquals(brush, Brushes.Transparent)) return false;
            if (brush is SolidColorBrush scb && scb.Color.A == 0) return false;
            return true;
        }
    }
}
