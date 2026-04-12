using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RefreshToAccess2.Helpers
{
    public static class TitleBarHelper
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attribute, ref int value, int size);

        // Dark title bar text/icons (Win 10 1809+)
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE     = 20;
        // Title bar & border colour (Win 11 22000+ — silently ignored on Win 10)
        private const int DWMWA_BORDER_COLOR  = 34;
        private const int DWMWA_CAPTION_COLOR = 35;

        /// <summary>
        /// Colours the native title bar and border to match the
        /// MaterialDesign background. Call from SourceInitialized.
        /// </summary>
        public static void Apply(Window window)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
                if (hwnd == IntPtr.Zero) return;

                Color bg = ResolveBackground(window);
                bool dark = IsDark(bg);

                // Caption button icon colour (light-on-dark or dark-on-light)
                int mode = dark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref mode, sizeof(int));
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD,
                    ref mode, sizeof(int));

                // Title bar fill and 1 px window border (Win 11 only)
                int colRef = ToColorRef(bg);
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR,
                    ref colRef, sizeof(int));
                DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR,
                    ref colRef, sizeof(int));
            }
            catch
            {
                // Unsupported OS — fall back to default frame
            }
        }

        private static Color ResolveBackground(Window window)
        {
            if (window.TryFindResource("MaterialDesign.Brush.Background")
                    is SolidColorBrush mdBrush)
                return mdBrush.Color;

            if (window.Background is SolidColorBrush wb)
                return wb.Color;

            return Colors.White;
        }

        private static bool IsDark(Color c) =>
            (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) < 128;

        // COLORREF = 0x00BBGGRR
        private static int ToColorRef(Color c) =>
            c.R | (c.G << 8) | (c.B << 16);
    }
}
