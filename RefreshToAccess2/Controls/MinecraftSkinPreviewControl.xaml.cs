using RefreshToAccess2.Models;
using RefreshToAccess2.Rendering;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace RefreshToAccess2.Controls
{
    public partial class MinecraftSkinPreviewControl : System.Windows.Controls.UserControl, IDisposable
    {
        private readonly PlayerPreviewRenderControl _renderer = new();

        public MinecraftSkinPreviewControl()
        {
            InitializeComponent();
            Host.Child = _renderer;
            Unloaded += (_, _) => _renderer.Dispose();
        }

        public static readonly DependencyProperty SkinBytesProperty =
            DependencyProperty.Register(
                nameof(SkinBytes),
                typeof(byte[]),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(null, OnSkinBytesChanged));

        public static readonly DependencyProperty SkinVariantProperty =
            DependencyProperty.Register(
                nameof(SkinVariant),
                typeof(MinecraftSkinVariant),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(MinecraftSkinVariant.Classic, OnSkinVariantChanged));

        public static readonly DependencyProperty AnimationModeProperty =
            DependencyProperty.Register(
                nameof(AnimationMode),
                typeof(PreviewAnimationMode),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(PreviewAnimationMode.Auto, OnAnimationModeChanged));

        public static readonly DependencyProperty BackgroundModeProperty =
            DependencyProperty.Register(
                nameof(BackgroundMode),
                typeof(PreviewBackgroundMode),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(PreviewBackgroundMode.Bright, OnBackgroundModeChanged));

        public static readonly DependencyProperty PanoramaSourcePathProperty =
            DependencyProperty.Register(
                nameof(PanoramaSourcePath),
                typeof(string),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(null, OnPanoramaChanged));

        public static readonly DependencyProperty DlssEnabledProperty =
            DependencyProperty.Register(
                nameof(DlssEnabled),
                typeof(bool),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(false, OnDlssParameterChanged));

        public static readonly DependencyProperty DlssStyleIndexProperty =
            DependencyProperty.Register(
                nameof(DlssStyleIndex),
                typeof(int),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(0, OnDlssParameterChanged));

        public static readonly DependencyProperty DlssIntensityProperty =
            DependencyProperty.Register(
                nameof(DlssIntensity),
                typeof(double),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(0.75, OnDlssParameterChanged));

        public static readonly DependencyProperty DlssToneProperty =
            DependencyProperty.Register(
                nameof(DlssTone),
                typeof(double),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(0.50, OnDlssParameterChanged));

        public static readonly DependencyProperty DlssStructureStrengthProperty =
            DependencyProperty.Register(
                nameof(DlssStructureStrength),
                typeof(double),
                typeof(MinecraftSkinPreviewControl),
                new PropertyMetadata(0.85, OnDlssParameterChanged));

        public byte[]? SkinBytes
        {
            get => (byte[]?)GetValue(SkinBytesProperty);
            set => SetValue(SkinBytesProperty, value);
        }

        public MinecraftSkinVariant SkinVariant
        {
            get => (MinecraftSkinVariant)GetValue(SkinVariantProperty);
            set => SetValue(SkinVariantProperty, value);
        }

        public PreviewAnimationMode AnimationMode
        {
            get => (PreviewAnimationMode)GetValue(AnimationModeProperty);
            set => SetValue(AnimationModeProperty, value);
        }

        public PreviewBackgroundMode BackgroundMode
        {
            get => (PreviewBackgroundMode)GetValue(BackgroundModeProperty);
            set => SetValue(BackgroundModeProperty, value);
        }

        public string? PanoramaSourcePath
        {
            get => (string?)GetValue(PanoramaSourcePathProperty);
            set => SetValue(PanoramaSourcePathProperty, value);
        }

        public bool DlssEnabled
        {
            get => (bool)GetValue(DlssEnabledProperty);
            set => SetValue(DlssEnabledProperty, value);
        }

        public int DlssStyleIndex
        {
            get => (int)GetValue(DlssStyleIndexProperty);
            set => SetValue(DlssStyleIndexProperty, value);
        }

        public double DlssIntensity
        {
            get => (double)GetValue(DlssIntensityProperty);
            set => SetValue(DlssIntensityProperty, value);
        }

        public double DlssTone
        {
            get => (double)GetValue(DlssToneProperty);
            set => SetValue(DlssToneProperty, value);
        }

        public double DlssStructureStrength
        {
            get => (double)GetValue(DlssStructureStrengthProperty);
            set => SetValue(DlssStructureStrengthProperty, value);
        }

        public void ResetCamera(bool snap = true)
        {
            _renderer.ResetCamera(snap);
        }

        private static void OnSkinBytesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((MinecraftSkinPreviewControl)d)._renderer.SetSkinPng((byte[]?)e.NewValue);

        private static void OnSkinVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((MinecraftSkinPreviewControl)d)._renderer.SetSkinVariant((MinecraftSkinVariant)e.NewValue);

        private static void OnAnimationModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((MinecraftSkinPreviewControl)d)._renderer.SetAnimationMode((PreviewAnimationMode)e.NewValue);

        private static void OnBackgroundModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((MinecraftSkinPreviewControl)d)._renderer.SetBackgroundMode((PreviewBackgroundMode)e.NewValue);

        private static void OnPanoramaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((MinecraftSkinPreviewControl)d)._renderer.SetPanoramaSource((string?)e.NewValue);

        private static void OnDlssParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((MinecraftSkinPreviewControl)d).ApplyDlssParameters();

        private void ApplyDlssParameters()
        {
            _renderer.SetDlssNeuralRendering(
                DlssEnabled,
                DlssStyleIndex,
                (float)DlssIntensity,
                (float)DlssTone,
                (float)DlssStructureStrength);

            if (!DlssEnabled)
            {
                Effect = null;
                return;
            }

            // Software D3D11 fallback glow/tone effect when nvngx_dlssnr.dll is absent.
            byte r = (byte)Math.Clamp(80 + DlssTone * 160, 0, 255);
            byte g = (byte)Math.Clamp(210 - Math.Abs(DlssTone - 0.5) * 90, 0, 255);
            byte b = (byte)Math.Clamp(120 + (1.0 - DlssTone) * 120, 0, 255);
            Effect = new DropShadowEffect
            {
                Color = System.Windows.Media.Color.FromRgb(r, g, b),
                BlurRadius = 12 + DlssIntensity * 24,
                ShadowDepth = 0,
                Opacity = Math.Clamp(0.25 + DlssStructureStrength * 0.55, 0.0, 0.95)
            };
        }

        public void Dispose()
        {
            _renderer.Dispose();
        }
    }
}
