using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RefreshToAccess2.Helpers
{
    /// <summary>
    /// Attached behaviour that turns the default "step" mouse-wheel scrolling
    /// of a <see cref="ScrollViewer"/> into a brief eased glide.
    ///
    /// Rather than animating a proxy dependency property (which snaps back to its
    /// base value when the animation ends, and fights itself on rapid scrolls),
    /// this drives <see cref="ScrollViewer.ScrollToVerticalOffset"/> directly each
    /// frame via <see cref="CompositionTarget.Rendering"/>, moving the live offset
    /// toward a running target. There is no proxy value to fall back to, so the
    /// view never jumps back to where the scroll started.
    /// </summary>
    public static class SmoothScroll
    {
        // Pixels travelled per wheel notch (a notch is 120 delta units).
        private const double WheelStep = 60.0;
        // Fraction of the remaining distance covered per frame. Higher = snappier
        // / less "floaty". Lower = smoother but slower to settle.
        private const double Approach = 0.34;
        // Minimum pixels per frame so the tail doesn't crawl.
        private const double MinStep = 2.0;
        // Within this distance we snap to the target and stop animating.
        private const double SnapEpsilon = 0.5;

        // ── IsEnabled ───────────────────────────────────────────────
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(SmoothScroll),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject d) => (bool)d.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject d, bool v) => d.SetValue(IsEnabledProperty, v);

        // Running target offset the live position is gliding toward.
        private static readonly DependencyProperty TargetProperty =
            DependencyProperty.RegisterAttached(
                "Target", typeof(double), typeof(SmoothScroll),
                new PropertyMetadata(0.0));

        // ScrollViewers currently mid-glide, driven by one shared per-frame hook.
        private static readonly HashSet<ScrollViewer> _active = new();
        private static bool _hooked;

        private static void OnIsEnabledChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer sv) return;

            if ((bool)e.NewValue)
            {
                // Pixel-based scrolling so offsets are in device pixels, not items
                // — required for the per-frame interpolation to look smooth.
                sv.CanContentScroll = false;
                sv.PreviewMouseWheel += OnPreviewMouseWheel;
            }
            else
            {
                sv.PreviewMouseWheel -= OnPreviewMouseWheel;
                _active.Remove(sv);
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;
            if (e.Handled) return;

            // Let Ctrl+wheel (zoom etc.) fall through.
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;

            // Continue stacking onto the in-flight target; otherwise (re)seed from
            // the live offset so dragging the thumb in between is respected.
            double baseOffset = _active.Contains(sv)
                ? (double)sv.GetValue(TargetProperty)
                : sv.VerticalOffset;

            double target = baseOffset - (e.Delta / 120.0) * WheelStep;
            target = Math.Max(0, Math.Min(target, sv.ScrollableHeight));

            // At a boundary with nowhere to go — release the event so an outer
            // ScrollViewer (if any) can take over instead of swallowing it.
            bool atTop = target <= 0 && e.Delta > 0;
            bool atBottom = target >= sv.ScrollableHeight && e.Delta < 0;
            if ((atTop || atBottom) && Math.Abs(target - sv.VerticalOffset) < 0.5)
                return;

            sv.SetValue(TargetProperty, target);
            _active.Add(sv);
            EnsureHook();
            e.Handled = true;
        }

        private static void EnsureHook()
        {
            if (_hooked) return;
            CompositionTarget.Rendering += OnRendering;
            _hooked = true;
        }

        private static void OnRendering(object? sender, EventArgs e)
        {
            if (_active.Count == 0)
            {
                CompositionTarget.Rendering -= OnRendering;
                _hooked = false;
                return;
            }

            // Snapshot so we can remove finished viewers while iterating.
            var svs = new ScrollViewer[_active.Count];
            _active.CopyTo(svs);

            foreach (var sv in svs)
            {
                double target = (double)sv.GetValue(TargetProperty);
                double current = sv.VerticalOffset;
                double diff = target - current;

                if (Math.Abs(diff) <= SnapEpsilon)
                {
                    sv.ScrollToVerticalOffset(target);
                    _active.Remove(sv);
                    continue;
                }

                double step = diff * Approach;
                if (Math.Abs(step) < MinStep)
                    step = Math.Sign(diff) * Math.Min(MinStep, Math.Abs(diff));

                sv.ScrollToVerticalOffset(current + step);
            }
        }
    }
}
