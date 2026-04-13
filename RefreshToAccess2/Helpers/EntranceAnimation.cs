using RefreshToAccess2.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Point = System.Windows.Point;

namespace RefreshToAccess2.Helpers
{
    public static class EntranceAnimation
    {
        // ── Attached properties ────────────────────────────────────

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(EntranceAnimation),
                new PropertyMetadata(false, OnEnabledChanged));

        public static bool GetIsEnabled(DependencyObject d) => (bool)d.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject d, bool v) => d.SetValue(IsEnabledProperty, v);

        // Tracks which generation the element was last animated in.
        private static readonly DependencyProperty AnimGenProperty =
            DependencyProperty.RegisterAttached(
                "AnimGen", typeof(int), typeof(EntranceAnimation),
                new PropertyMetadata(-1));

        // Stores scroll subscription info so we can detach even after
        // the element leaves the visual tree.
        private static readonly DependencyProperty ScrollInfoProperty =
            DependencyProperty.RegisterAttached(
                "ScrollInfo", typeof(object), typeof(EntranceAnimation));

        // ── Static state ───────────────────────────────────────────

        private static int _gen;
        private static int _counter;
        private static long _lastTick;

        private const long BatchTicks = 6_000_000; // 600 ms window
        private const int StaggerMs = 28;
        private const int MaxStaggerMs = 450;

        /// <summary>
        /// Call before making the page visible.
        /// Resets the stagger counter and bumps the generation
        /// so every element re-animates on the next visibility pass.
        /// </summary>
        public static void BumpGeneration()
        {
            _gen++;
            _counter = 0;
            _lastTick = 0;
        }

        // ── Wiring ─────────────────────────────────────────────────

        private static void OnEnabledChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;

            if ((bool)e.NewValue)
            {
                fe.Loaded += OnLoaded;
                fe.Unloaded += OnUnloaded;
                fe.DataContextChanged += OnDCChanged;
                fe.IsVisibleChanged += OnVisChanged;
            }
            else
            {
                fe.Loaded -= OnLoaded;
                fe.Unloaded -= OnUnloaded;
                fe.DataContextChanged -= OnDCChanged;
                fe.IsVisibleChanged -= OnVisChanged;
                DetachScroll(fe);
            }
        }

        // ── Event handlers ─────────────────────────────────────────

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
                fe.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    new Action(() => Schedule(fe)));
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe) DetachScroll(fe);
        }

        private static void OnDCChanged(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;

            if (e.NewValue == null)
            {
                // Element is being cleared — clean up
                DetachScroll(fe);
                return;
            }

            // Reset generation so recycled containers re-animate
            fe.SetValue(AnimGenProperty, -1);

            if (fe.IsLoaded)
                fe.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    new Action(() => Schedule(fe)));
        }

        private static void OnVisChanged(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is FrameworkElement fe && (bool)e.NewValue && fe.IsLoaded)
                fe.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    new Action(() => Schedule(fe)));
        }

        // ── Core logic ─────────────────────────────────────────────

        private static void Schedule(FrameworkElement fe)
        {
            if (!fe.IsVisible || !fe.IsLoaded) return;

            int gen = (int)fe.GetValue(AnimGenProperty);
            if (gen == _gen) return; // Already animated this generation

            ScrollViewer? sv = FindParent<ScrollViewer>(fe);

            if (sv != null && !IsInViewport(fe, sv))
            {
                // Off-screen: hide and wait for scroll
                fe.Opacity = 0;
                AttachScroll(fe, sv);
                return;
            }

            // In viewport (or no ScrollViewer) — animate now
            fe.SetValue(AnimGenProperty, _gen);
            DetachScroll(fe);
            Animate(fe);
        }

        // ── Scroll subscription ────────────────────────────────────

        private sealed class ScrollSub
        {
            public ScrollViewer Sv = null!;
            public ScrollChangedEventHandler Handler = null!;
        }

        private static void AttachScroll(FrameworkElement fe, ScrollViewer sv)
        {
            DetachScroll(fe);

            ScrollChangedEventHandler handler = null!;
            handler = (_, __) =>
            {
                if (!fe.IsVisible || !fe.IsLoaded) return;
                if (!IsInViewport(fe, sv)) return;

                sv.ScrollChanged -= handler;
                fe.ClearValue(ScrollInfoProperty);

                int g = (int)fe.GetValue(AnimGenProperty);
                if (g == _gen) return;
                fe.SetValue(AnimGenProperty, _gen);
                Animate(fe);
            };

            fe.SetValue(ScrollInfoProperty, new ScrollSub { Sv = sv, Handler = handler });
            sv.ScrollChanged += handler;
        }

        private static void DetachScroll(FrameworkElement fe)
        {
            if (fe.GetValue(ScrollInfoProperty) is ScrollSub sub)
            {
                sub.Sv.ScrollChanged -= sub.Handler;
                fe.ClearValue(ScrollInfoProperty);
            }
        }

        // ── Viewport check ─────────────────────────────────────────

        private static bool IsInViewport(FrameworkElement fe, ScrollViewer sv)
        {
            try
            {
                if (fe.ActualWidth <= 0 || fe.ActualHeight <= 0)
                    return false;

                GeneralTransform transform = fe.TransformToAncestor(sv);
                Point pt = transform.Transform(new Point(0, 0));

                Rect bounds = new Rect(pt, new System.Windows.Size(fe.ActualWidth, fe.ActualHeight));
                Rect viewport = new Rect(0, 0, sv.ViewportWidth, sv.ViewportHeight);

                return viewport.IntersectsWith(bounds);
            }
            catch
            {
                // Transform failed — assume visible to avoid stuck items
                return true;
            }
        }

        // ── Animation ──────────────────────────────────────────────

        private static void Animate(FrameworkElement fe)
        {
            long now = DateTime.UtcNow.Ticks;
            if (now - _lastTick > BatchTicks) _counter = 0;
            _lastTick = now;

            int idx = _counter++;
            int delayMs = Math.Min(idx * StaggerMs, MaxStaggerMs);
            TimeSpan delay = TimeSpan.FromMilliseconds(delayMs);
            TimeSpan dur = TimeSpan.FromMilliseconds(350);

            ScaleTransform scale = EnsureScale(fe);

            // Start state
            scale.ScaleX = 0.82;
            scale.ScaleY = 0.82;
            fe.Opacity = 0;

            var bounce = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            var soft = new CubicEase { EasingMode = EasingMode.EaseOut };

            var sx = new DoubleAnimation(0.82, 1, dur)
            { BeginTime = delay, EasingFunction = bounce };
            var sy = new DoubleAnimation(0.82, 1, dur)
            { BeginTime = delay, EasingFunction = bounce };
            var op = new DoubleAnimation(0, 1, dur)
            { BeginTime = delay, EasingFunction = soft };

            // Release animation holds on completion to avoid stale values
            op.Completed += (_, __) =>
            {
                fe.BeginAnimation(UIElement.OpacityProperty, null);
                fe.Opacity = 1;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            fe.BeginAnimation(UIElement.OpacityProperty, op);

            // Trigger lazy head-image load
            if (fe.DataContext is ProfileCardItem card && !card.HasHeadImage)
                _ = card.LoadHeadAsync();
        }

        // ── Helpers ────────────────────────────────────────────────

        private static ScaleTransform EnsureScale(FrameworkElement fe)
        {
            if (fe.RenderTransform is ScaleTransform existing)
                return existing;

            var scale = new ScaleTransform(1, 1);
            fe.RenderTransform = scale;
            fe.RenderTransformOrigin = new Point(0.5, 0.5);
            return scale;
        }

        private static T? FindParent<T>(DependencyObject d) where T : DependencyObject
        {
            DependencyObject? p = VisualTreeHelper.GetParent(d);
            while (p != null)
            {
                if (p is T t) return t;
                p = VisualTreeHelper.GetParent(p);
            }
            return null;
        }
    }
}
