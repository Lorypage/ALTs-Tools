using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using Point = System.Windows.Point;

namespace RefreshToAccess2.Helpers
{
    /// <summary>
    /// Attached behavior adding a lively hover-pop / press-dip scale animation
    /// to any element. Apply with <c>helpers:InteractionAnimation.Pop="True"</c>.
    /// Uses a centered ScaleTransform and a BackEase for a springy feel.
    /// </summary>
    public static class InteractionAnimation
    {
        // Per-element peak hover scale (default 1.05). Lets large buttons pop less.
        public static readonly DependencyProperty HoverScaleProperty =
            DependencyProperty.RegisterAttached(
                "HoverScale", typeof(double), typeof(InteractionAnimation),
                new PropertyMetadata(1.05));

        public static double GetHoverScale(DependencyObject d) => (double)d.GetValue(HoverScaleProperty);
        public static void SetHoverScale(DependencyObject d, double v) => d.SetValue(HoverScaleProperty, v);

        public static readonly DependencyProperty PopProperty =
            DependencyProperty.RegisterAttached(
                "Pop", typeof(bool), typeof(InteractionAnimation),
                new PropertyMetadata(false, OnPopChanged));

        public static bool GetPop(DependencyObject d) => (bool)d.GetValue(PopProperty);
        public static void SetPop(DependencyObject d, bool v) => d.SetValue(PopProperty, v);

        private static readonly BackEase PopEase =
            new() { EasingMode = EasingMode.EaseOut, Amplitude = 0.45 };
        private static readonly CubicEase SoftEase =
            new() { EasingMode = EasingMode.EaseOut };

        private static void OnPopChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;

            if ((bool)e.NewValue)
            {
                fe.MouseEnter += OnEnter;
                fe.MouseLeave += OnLeave;
                fe.PreviewMouseLeftButtonDown += OnDown;
                fe.PreviewMouseLeftButtonUp += OnUp;
            }
            else
            {
                fe.MouseEnter -= OnEnter;
                fe.MouseLeave -= OnLeave;
                fe.PreviewMouseLeftButtonDown -= OnDown;
                fe.PreviewMouseLeftButtonUp -= OnUp;
            }
        }

        private static ScaleTransform EnsureScale(FrameworkElement fe)
        {
            if (fe.RenderTransform is ScaleTransform s) return s;
            var scale = new ScaleTransform(1, 1);
            fe.RenderTransform = scale;
            fe.RenderTransformOrigin = new Point(0.5, 0.5);
            return scale;
        }

        private static void AnimateTo(FrameworkElement fe, double to, int ms, IEasingFunction ease)
        {
            var scale = EnsureScale(fe);
            var ax = new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease };
            var ay = new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, ax);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, ay);
        }

        private static void OnEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement fe)
                AnimateTo(fe, GetHoverScale(fe), 220, PopEase);
        }

        private static void OnLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement fe)
                AnimateTo(fe, 1.0, 240, SoftEase);
        }

        private static void OnDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe)
                AnimateTo(fe, 0.93, 90, SoftEase);
        }

        private static void OnUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe)
                // Spring back to hover scale (cursor is still over the element).
                AnimateTo(fe, GetHoverScale(fe), 300, PopEase);
        }
    }
}
