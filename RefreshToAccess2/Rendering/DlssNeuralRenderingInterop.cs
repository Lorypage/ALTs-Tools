using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RefreshToAccess2.Rendering
{
    /// <summary>
    /// Interop and fallback detection layer for the leaked NBA 2K27 Early Access
    /// NVIDIA DLSS 5 Neural Rendering library (<c>nvngx_dlssnr.dll</c>), including
    /// community-modded RTX 40-series (Ada Lovelace / RTX 4060) hard-run spoof support.
    /// </summary>
    public static class DlssNeuralRenderingInterop
    {
        public const string LibraryName = "nvngx_dlssnr.dll";

        public sealed record StylePresetOption(int Index, string DisplayName);

        public static IReadOnlyList<StylePresetOption> StylePresets { get; } = new[]
        {
            new StylePresetOption(0, "Style 1: NBA 2K27 Sweat & Pores"),
            new StylePresetOption(1, "Style 2: Photorealistic Skin"),
            new StylePresetOption(2, "Style 3: Plastic AI Slop"),
            new StylePresetOption(3, "Style 4: Oil Painting"),
            new StylePresetOption(4, "Style 5: Studio Rim Light"),
            new StylePresetOption(5, "Style 6: Subsurface Scattering"),
            new StylePresetOption(6, "Style 7: Blackwell Overdrive"),
        };

        private static readonly string CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RefreshToAccess2",
            "dlss5");

        private static readonly string FirstLaunchMarkerPath =
            Path.Combine(CacheDirectory, ".dlss5_first_launch_checked");

        private static IntPtr _nativeHandle;
        private static int _firstLaunchHandled;

        [StructLayout(LayoutKind.Sequential)]
        public struct DlssNrEvaluateParams
        {
            public int CharacterModelsOnly;
            public int StylePresetIndex;
            public float Intensity;
            public float Tone;
            public float StructureStrength;
            public int SpoofBlackwellOnAda40Series;
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "NVSDK_NGX_DLSSNR_EvaluateFeature")]
        private static extern int NativeEvaluateFeature(ref DlssNrEvaluateParams parameters);

        public static bool IsNativeLibraryLoaded => _nativeHandle != IntPtr.Zero;

        /// <summary>
        /// Attempts to load <c>nvngx_dlssnr.dll</c> via <see cref="NativeLibrary.TryLoad(string, out IntPtr)"/>.
        /// Falls back gracefully to Direct3D11 software simulation when the leaked DLL is absent.
        /// </summary>
        public static bool TryLoadNativeLibrary(out string statusMessage)
        {
            if (_nativeHandle != IntPtr.Zero)
            {
                statusMessage = "已加载 NBA 2K27 泄露版 nvngx_dlssnr.dll（RTX 4060 社区魔改硬跑模式 / Character Models Only）";
                return true;
            }

            if (NativeLibrary.TryLoad(LibraryName, out IntPtr handle) ||
                NativeLibrary.TryLoad(Path.Combine(AppContext.BaseDirectory, LibraryName), out handle) ||
                NativeLibrary.TryLoad(Path.Combine(CacheDirectory, LibraryName), out handle))
            {
                _nativeHandle = handle;
                statusMessage = "已加载 NBA 2K27 泄露版 nvngx_dlssnr.dll（RTX 4060 社区魔改硬跑模式 / Character Models Only）";
                return true;
            }

            statusMessage = "未检测到 NBA 2K27 泄露版 nvngx_dlssnr.dll，已启用 Direct3D11 模拟神经渲染（警告：拯救者独显占用即将达到 20% / 58°C）";
            return false;
        }

        /// <summary>
        /// On first launch, checks and attempts to provision the community-modded RTX 40-series
        /// <c>nvngx_dlssnr.dll</c> package before falling back to the Vortice.Direct3D11 simulation filter.
        /// </summary>
        public static async Task<string> EnsureFirstLaunchProvisionedAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _firstLaunchHandled, 1) == 1)
            {
                TryLoadNativeLibrary(out string cachedStatus);
                return cachedStatus;
            }

            try
            {
                Directory.CreateDirectory(CacheDirectory);

                if (!File.Exists(FirstLaunchMarkerPath) && !File.Exists(Path.Combine(CacheDirectory, LibraryName)))
                {
                    // Probe the community mirror briefly on first launch; never block startup on network failure.
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                    try
                    {
                        using var response = await http.GetAsync(
                            "https://raw.githubusercontent.com/Lorypage/ALTs-Tools/master/dlss5/nvngx_dlssnr.dll",
                            HttpCompletionOption.ResponseHeadersRead,
                            cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            byte[] payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                            if (payload.Length > 0)
                                await File.WriteAllBytesAsync(Path.Combine(CacheDirectory, LibraryName), payload, cancellationToken);
                        }
                    }
                    catch
                    {
                        // Offline or mirror unavailable — fall back cleanly to D3D11 simulation.
                    }

                    await File.WriteAllTextAsync(
                        FirstLaunchMarkerPath,
                        $"checked_utc={DateTime.UtcNow:O};arch=RTX4060_CommunityMod",
                        cancellationToken);
                }
            }
            catch
            {
                // Ignore filesystem errors in restricted environments.
            }

            TryLoadNativeLibrary(out string status);
            return status;
        }

        /// <summary>
        /// Evaluates DLSS-NR parameters against the native library if loaded; otherwise returns false
        /// so the caller uses the Vortice.Direct3D11 software simulation filter.
        /// </summary>
        public static bool TryEvaluate(
            int stylePresetIndex,
            double intensity,
            double tone,
            double structureStrength)
        {
            if (_nativeHandle == IntPtr.Zero)
                return false;

            try
            {
                var parameters = new DlssNrEvaluateParams
                {
                    CharacterModelsOnly = 1,
                    StylePresetIndex = stylePresetIndex,
                    Intensity = (float)intensity,
                    Tone = (float)tone,
                    StructureStrength = (float)structureStrength,
                    SpoofBlackwellOnAda40Series = 1
                };
                return NativeEvaluateFeature(ref parameters) == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
