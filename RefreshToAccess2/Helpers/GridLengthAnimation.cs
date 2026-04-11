using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace RefreshToAccess2.Helpers
{
    /// <summary>
    /// Animates a <see cref="GridLength"/> value, enabling smooth
    /// column / row width transitions in a <see cref="Grid"/>.
    /// </summary>
    public sealed class GridLengthAnimation : AnimationTimeline
    {
        public override Type TargetPropertyType => typeof(GridLength);

        protected override Freezable CreateInstanceCore()
            => new GridLengthAnimation();

        // ── From ───────────────────────────────────────────────────────
        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register(nameof(From), typeof(GridLength),
                typeof(GridLengthAnimation));

        // ── To ─────────────────────────────────────────────────────────
        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }
        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register(nameof(To), typeof(GridLength),
                typeof(GridLengthAnimation));

        // ── Easing ─────────────────────────────────────────────────────
        public IEasingFunction? EasingFunction
        {
            get => (IEasingFunction?)GetValue(EasingFunctionProperty);
            set => SetValue(EasingFunctionProperty, value);
        }
        public static readonly DependencyProperty EasingFunctionProperty =
            DependencyProperty.Register(nameof(EasingFunction),
                typeof(IEasingFunction), typeof(GridLengthAnimation));

        // ── Interpolation ──────────────────────────────────────────────
        public override object GetCurrentValue(
            object defaultOriginValue,
            object defaultDestinationValue,
            AnimationClock animationClock)
        {
            double progress = animationClock.CurrentProgress ?? 0;

            if (EasingFunction != null)
                progress = EasingFunction.Ease(progress);

            double from = From.Value;
            double to   = To.Value;
            double current = from + (to - from) * progress;

            return new GridLength(current, GridUnitType.Pixel);
        }
    }
}
