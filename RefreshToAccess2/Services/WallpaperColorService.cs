using System;
using System.IO;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;

namespace RefreshToAccess2.Services
{
    /// <summary>
    /// Extracts a dominant "seed" color from the current Windows desktop
    /// wallpaper — the basis for Material You-style dynamic theming.
    /// Downsamples the wallpaper and picks the most saturated/representative
    /// hue so the generated accent feels vivid rather than muddy.
    /// </summary>
    public static class WallpaperColorService
    {
        /// <summary>
        /// Returns the wallpaper-derived seed color, or null if the wallpaper
        /// can't be read (no path, unsupported format, decode failure).
        /// </summary>
        public static Color? TryGetSeedColor()
        {
            string? path = GetWallpaperPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            try
            {
                return ExtractSeed(path);
            }
            catch
            {
                return null;
            }
        }

        private static string? GetWallpaperPath()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
                return key?.GetValue("WallPaper") as string;
            }
            catch
            {
                return null;
            }
        }

        private static Color ExtractSeed(string path)
        {
            // Decode at a small fixed width — plenty for color statistics, cheap.
            var bmp = new System.Drawing.Bitmap(path);
            using (bmp)
            {
                const int sample = 48;
                using var small = new System.Drawing.Bitmap(bmp, new System.Drawing.Size(sample, sample));

                double bestScore = -1;
                Color best = Color.FromRgb(0x67, 0x50, 0xA4); // fallback purple

                // Also track an average as a secondary fallback.
                long ar = 0, ag = 0, ab = 0, n = 0;

                for (int y = 0; y < sample; y++)
                {
                    for (int x = 0; x < sample; x++)
                    {
                        var px = small.GetPixel(x, y);
                        if (px.A < 16) continue;

                        ar += px.R; ag += px.G; ab += px.B; n++;

                        RgbToHsv(px.R, px.G, px.B, out _, out double s, out double v);

                        // Favor saturated, mid-to-bright pixels — the "accent"
                        // a person would name when describing the wallpaper.
                        double score = s * (1 - Math.Abs(v - 0.62));
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = Color.FromRgb(px.R, px.G, px.B);
                        }
                    }
                }

                // If the wallpaper is basically grayscale, fall back to its average.
                if (bestScore < 0.08 && n > 0)
                    best = Color.FromRgb((byte)(ar / n), (byte)(ag / n), (byte)(ab / n));

                return Vivify(best);
            }
        }

        // Nudge the seed toward a usable accent: ensure enough saturation and a
        // balanced brightness so both light and dark themes read well.
        private static Color Vivify(Color c)
        {
            RgbToHsv(c.R, c.G, c.B, out double h, out double s, out double v);
            s = Math.Clamp(s < 0.35 ? 0.45 : s, 0.35, 0.95);
            v = Math.Clamp(v, 0.45, 0.85);
            HsvToRgb(h, s, v, out byte r, out byte g, out byte b);
            return Color.FromRgb(r, g, b);
        }

        private static void RgbToHsv(byte r, byte g, byte b,
            out double h, out double s, out double v)
        {
            double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double d = max - min;

            v = max;
            s = max <= 0 ? 0 : d / max;

            if (d <= 0) { h = 0; return; }
            if (max == rd) h = 60 * (((gd - bd) / d) % 6);
            else if (max == gd) h = 60 * (((bd - rd) / d) + 2);
            else h = 60 * (((rd - gd) / d) + 4);
            if (h < 0) h += 360;
        }

        private static void HsvToRgb(double h, double s, double v,
            out byte r, out byte g, out byte b)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60 % 2) - 1));
            double m = v - c;
            double rd, gd, bd;

            if (h < 60) { rd = c; gd = x; bd = 0; }
            else if (h < 120) { rd = x; gd = c; bd = 0; }
            else if (h < 180) { rd = 0; gd = c; bd = x; }
            else if (h < 240) { rd = 0; gd = x; bd = c; }
            else if (h < 300) { rd = x; gd = 0; bd = c; }
            else { rd = c; gd = 0; bd = x; }

            r = (byte)Math.Round((rd + m) * 255);
            g = (byte)Math.Round((gd + m) * 255);
            b = (byte)Math.Round((bd + m) * 255);
        }
    }
}
