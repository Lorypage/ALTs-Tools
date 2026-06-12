using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace RefreshToAccess2.Helpers
{
    /// <summary>
    /// Attached behaviour that turns the default "step" mouse-wheel scrolling
    /// of a <see cref="ScrollViewer"/> into an animated, eased glide.
    ///
    /// WPF's stock wheel handling jumps the offset in discrete chunks with no
    /// interpolation, which feels choppy. Here we intercept the wheel, keep a
    /// running target offset, and animate a proxy property that drives
    /// <see cref="ScrollViewer.ScrollToVerticalOffset"/> each frame.
    /// </summary>
    public static class SmoothScroll
    {
        // Pixels travelled per wheel notch. A notch is 120 delta units.
        private const double WheelStep = 90.0;
        private static readonly Duration GlideDuration =
            new Duration(TimeSpan.FromMilliseconds(380));

        // ── IsEnabled ───────────────────────────────────────────────
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(SmoothScroll),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject d) => (bool)d.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject d, bool v) => d.SetValue(IsEnabledProperty, v);

        // ── Proxy offset that we actually animate ───────────────────
        private static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "VerticalOffset", typeof(double), typeof(SmoothScroll),
                new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        // Where the in-flight animation is heading, so successive notches stack.
        private static readonly DependencyProperty TargetOffsetProperty =
            DependencyProperty.RegisterAttached(
                "TargetOffset", typeof(double), typeof(SmoothScroll),
                new PropertyMetadata(0.0));

        private static void OnIsEnabledChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer sv) return;

            if ((bool)e.NewValue)
                sv.PreviewMouseWheel += OnPreviewMouseWheel;
            else
                sv.PreviewMouseWheel -= OnPreviewMouseWheel;
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;
            if (e.Handled) return;

            // Let Ctrl+wheel (zoom etc.) and horizontal cases fall through.
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;

            // Seed the target from the live offset the first time, or whenever
            // the user dragged the thumb / scrolled by other means in between.
            double current = (double)sv.GetValue(TargetOffsetProperty);
            if (Math.Abs(sv.VerticalOffset - (double)sv.GetValue(VerticalOffsetProperty)) > 1.0)
                current = sv.VerticalOffset;

            double target = current - (e.Delta / 120.0) * WheelStep;
            target = Math.Max(0, Math.Min(target, sv.ScrollableHeight));

            // At a boundary with nowhere to go — release the event to let an
            // outer ScrollViewer (if any) take over instead of swallowing it.
            bool atTop = target <= 0 && e.Delta > 0;
            bool atBottom = target >= sv.ScrollableHeight && e.Delta < 0;
            if ((atTop || atBottom) && Math.Abs(target - sv.VerticalOffset) < 0.5)
                return;

            sv.SetValue(TargetOffsetProperty, target);
            sv.SetValue(VerticalOffsetProperty, sv.VerticalOffset);

            var anim = new DoubleAnimation
            {
                To = target,
                Duration = GlideDuration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            };

            sv.BeginAnimation(VerticalOffsetProperty, anim);
            e.Handled = true;
        }

        private static void OnVerticalOffsetChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
                sv.ScrollToVerticalOffset((double)e.NewValue);
        }
    }
}
