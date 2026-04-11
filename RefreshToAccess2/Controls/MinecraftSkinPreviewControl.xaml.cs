using RefreshToAccess2.Models;
using RefreshToAccess2.Rendering;
using System;
using System.Windows;
using System.Windows.Controls;

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

        public void Dispose()
        {
            _renderer.Dispose();
        }
    }
}
