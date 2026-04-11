using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;

namespace RefreshToAccess2.Rendering
{
    internal sealed class ResolvedSkyboxSource
    {
        // logical order: front, right, back, left, up, down
        public string[] FaceFiles { get; init; } = Array.Empty<string>();
        public bool Rotate { get; init; }
        public Vector3 RotationAxis { get; init; } = Vector3.UnitY;
        public float RotationSpeedDegreesPerSecond { get; init; }
        public string DebugName { get; init; } = string.Empty;
    }

    internal static class MinecraftSkyboxResolver
    {
        private sealed class OptiFineSkyProperties
        {
            public string? Source { get; set; }
            public bool Rotate { get; set; }
            public float Speed { get; set; } = 1.0f;
            public Vector3 Axis { get; set; } = Vector3.UnitY;
        }

        private readonly record struct FaceSet(
            string Front,
            string Right,
            string Back,
            string Left,
            string Up,
            string Down);

        private enum Edge
        {
            Left,
            Right,
            Top,
            Bottom
        }

        private static readonly FaceSet[] CandidateSets =
        {
            new("panorama_0.png", "panorama_1.png", "panorama_2.png", "panorama_3.png", "panorama_4.png", "panorama_5.png"),
            new("0.png", "1.png", "2.png", "3.png", "4.png", "5.png"),
            new("1.png", "2.png", "3.png", "4.png", "5.png", "6.png"),
            new("front.png", "right.png", "back.png", "left.png", "top.png", "bottom.png"),
            new("pz.png", "px.png", "nz.png", "nx.png", "py.png", "ny.png"),
            new("posz.png", "posx.png", "negz.png", "negx.png", "posy.png", "negy.png")
        };

        private static readonly string[] ImageExtensions =
        {
            ".png", ".jpg", ".jpeg", ".bmp"
        };

        private static readonly RotateFlipType[][] OrientationPresets =
        {
            new[]
            {
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipNone,
            },

            new[]
            {
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.Rotate180FlipNone,
            },

            new[]
            {
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.Rotate180FlipNone,
            },

            new[]
            {
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.Rotate180FlipNone,
            },

            new[]
            {
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.Rotate180FlipNone,
            },

            new[]
            {
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipNone,
            }
        };

        public static bool TryResolve(string sourcePath, out ResolvedSkyboxSource result)
        {
            result = new ResolvedSkyboxSource();

            if (string.IsNullOrWhiteSpace(sourcePath))
                return false;

            if (File.Exists(sourcePath))
            {
                string ext = Path.GetExtension(sourcePath);

                if (IsSupportedImageExtension(ext))
                    return TryResolveImageFile(sourcePath, null, out result);

                if (ext.Equals(".properties", StringComparison.OrdinalIgnoreCase))
                    return TryResolveLoosePropertiesFile(sourcePath, out result);

                if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".jar", StringComparison.OrdinalIgnoreCase))
                {
                    return TryResolveArchive(sourcePath, out result);
                }

                return false;
            }

            if (Directory.Exists(sourcePath))
                return TryResolveFolder(sourcePath, out result);

            return false;
        }

        private static bool TryResolveLoosePropertiesFile(string propertiesPath, out ResolvedSkyboxSource result)
        {
            string packRoot = GuessPackRootFromFile(propertiesPath);
            return TryResolveFolderProperties(packRoot, propertiesPath, out result);
        }

        private static string GuessPackRootFromFile(string filePath)
        {
            DirectoryInfo? dir = new FileInfo(filePath).Directory;

            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "assets")))
                    return dir.FullName;

                dir = dir.Parent;
            }

            return Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory;
        }

        private static bool TryResolveFolder(string root, out ResolvedSkyboxSource result)
        {
            result = new ResolvedSkyboxSource();

            if (TryResolveDirectUserFolder(root, out result))
                return true;

            if (TryResolveKnownPackFolder(root, out result))
                return true;

            return false;
        }

        private static bool TryResolveDirectUserFolder(string folder, out ResolvedSkyboxSource result)
        {
            result = new ResolvedSkyboxSource();

            if (TryResolveFaceFolder(folder, out string[] faceFiles))
            {
                result = BuildResultFromFaceFiles(faceFiles, null, folder);
                return true;
            }

            string[] directProps;
            try
            {
                directProps = Directory.GetFiles(folder, "*.properties", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                directProps = Array.Empty<string>();
            }

            foreach (string propsPath in directProps.OrderBy(ExtractTrailingNumber).ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                if (TryResolveFolderProperties(folder, propsPath, out result))
                    return true;
            }

            string[] directImages = GetDirectImages(folder);

            bool canTryLooseImages =
                LooksLikeSkyFolderName(folder) ||
                (!IsPackRoot(folder) && directImages.Length == 1);

            if (canTryLooseImages)
            {
                foreach (string imagePath in OrderCandidateImages(directImages))
                {
                    if (TryResolveImageFile(imagePath, null, out result))
                        return true;
                }
            }

            return false;
        }

        private static bool TryResolveKnownPackFolder(string root, out ResolvedSkyboxSource result)
        {
            result = new ResolvedSkyboxSource();

            string vanillaPath = Path.Combine(
                root, "assets", "minecraft", "textures", "gui", "title", "background");

            if (Directory.Exists(vanillaPath))
            {
                if (TryResolveFaceFolder(vanillaPath, out string[] vanillaFaces))
                {
                    result = BuildResultFromFaceFiles(vanillaFaces, null, vanillaPath);
                    return true;
                }

                foreach (string imagePath in OrderCandidateImages(GetDirectImages(vanillaPath)))
                {
                    if (TryResolveImageFile(imagePath, null, out result))
                        return true;
                }
            }

            string[] skyRoots =
            {
                Path.Combine(root, "assets", "minecraft", "optifine", "sky"),
                Path.Combine(root, "assets", "minecraft", "mcpatcher", "sky")
            };

            foreach (string skyRoot in skyRoots)
            {
                if (!Directory.Exists(skyRoot))
                    continue;

                foreach (string propsPath in EnumerateFilesSafe(skyRoot, "*.properties")
                    .OrderBy(ExtractTrailingNumber)
                    .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
                {
                    if (TryResolveFolderProperties(root, propsPath, out result))
                        return true;
                }

                foreach (string dir in EnumerateDirectoriesAndSelfSafe(skyRoot))
                {
                    if (TryResolveFaceFolder(dir, out string[] faceFiles))
                    {
                        result = BuildResultFromFaceFiles(faceFiles, null, dir);
                        return true;
                    }

                    if (LooksLikeSkyFolderName(dir))
                    {
                        foreach (string imagePath in OrderCandidateImages(GetDirectImages(dir)))
                        {
                            if (TryResolveImageFile(imagePath, null, out result))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool TryResolveArchive(string archivePath, out ResolvedSkyboxSource result)
        {
            result = new ResolvedSkyboxSource();

            try
            {
                using ZipArchive archive = ZipFile.OpenRead(archivePath);

                Dictionary<string, ZipArchiveEntry> entryMap =
                    archive.Entries
                           .Where(e => !string.IsNullOrWhiteSpace(Path.GetFileName(e.FullName)))
                           .ToDictionary(
                                e => NormalizeArchivePath(e.FullName),
                                e => e,
                                StringComparer.OrdinalIgnoreCase);

                if (TryResolveArchiveFaceSet(entryMap, "assets/minecraft/textures/gui/title/background", out ZipArchiveEntry[] vanillaEntries))
                {
                    result = BuildResultFromFaceFiles(ExtractEntries(vanillaEntries), null, archivePath);
                    return true;
                }

                string[] skyRoots =
                {
                    "assets/minecraft/optifine/sky",
                    "assets/minecraft/mcpatcher/sky"
                };

                foreach (string skyRoot in skyRoots)
                {
                    string[] propsKeys = entryMap.Keys
                        .Where(k => k.StartsWith(skyRoot + "/", StringComparison.OrdinalIgnoreCase))
                        .Where(k => k.EndsWith(".properties", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(ExtractTrailingNumber)
                        .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    foreach (string propKey in propsKeys)
                    {
                        if (TryResolveArchiveProperties(entryMap, propKey, out result))
                            return true;
                    }

                    string[] dirs = entryMap.Keys
                        .Select(GetDirectoryPart)
                        .Where(d => d.Equals(skyRoot, StringComparison.OrdinalIgnoreCase) ||
                                    d.StartsWith(skyRoot + "/", StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(s => s.Length)
                        .ToArray();

                    foreach (string dir in dirs)
                    {
                        if (TryResolveArchiveFaceSet(entryMap, dir, out ZipArchiveEntry[] faceEntries))
                        {
                            result = BuildResultFromFaceFiles(ExtractEntries(faceEntries), null, $"{archivePath}:{dir}");
                            return true;
                        }

                        if (LooksLikeSkyFolderName(dir))
                        {
                            if (TryResolveArchiveImagesInSingleDir(entryMap, dir, out result))
                                return true;
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryResolveArchiveImagesInSingleDir(
            Dictionary<string, ZipArchiveEntry> entryMap,
            string dir,
            out ResolvedSkyboxSource result)
        {
            result = new ResolvedSkyboxSource();

            string[] images = entryMap.Keys
                .Where(k => string.Equals(GetDirectoryPart(k), dir, StringComparison.OrdinalIgnoreCase))
                .Where(k => IsSupportedImageExtension(Path.GetExtension(k)))
                .ToArray();

            if (images.Length == 0)
                return false;

            bool allow =
                LooksLikeSkyFolderName(dir) ||
                images.Length == 1 ||
                images.Any(LooksLikeSkyImageName);

            if (!allow)
                return false;

            foreach (string key in images
                .OrderByDescending(ScoreImageCandidate)
                .ThenByDescending(k => entryMap[k].Length))
            {
                string tempImage = ExtractEntry(entryMap[key]);
                if (TryResolveImageFile(tempImage, null, out result))
                    return true;
            }

            return false;
        }

        private static bool TryResolveFolderProperties(string packRoot, string propertiesPath, out ResolvedSkyboxSource result)
        {
            result = new ResolvedSkyboxSource();

            try
            {
                string text = File.ReadAllText(propertiesPath);
                OptiFineSkyProperties props = ParseProperties(text);

                string propertiesDir = Path.GetDirectoryName(propertiesPath) ?? packRoot;
                string propertiesBaseName = Path.GetFileNameWithoutExtension(propertiesPath);

                foreach (string candidate in BuildSourceCandidates(props, propertiesBaseName))
                {
                    string resolvedPath;

                    if (candidate.StartsWith("/") || candidate.StartsWith("\\"))
                    {
                        string trimmed = candidate.TrimStart('/', '\\')
                            .Replace('/', Path.DirectorySeparatorChar)
                            .Replace('\\', Path.DirectorySeparatorChar);

                        resolvedPath = Path.GetFullPath(Path.Combine(packRoot, trimmed));
                    }
                    else
                    {
                        string relative = candidate
                            .Replace('/', Path.DirectorySeparatorChar)
                            .Replace('\\', Path.DirectorySeparatorChar);

                        resolvedPath = Path.GetFullPath(Path.Combine(propertiesDir, relative));
                    }

                    if (File.Exists(resolvedPath))
                    {
                        if (IsSupportedImageExtension(Path.GetExtension(resolvedPath)) &&
                            TryResolveImageFile(resolvedPath, props, out result))
                        {
                            return true;
                        }
                    }

                    if (Directory.Exists(resolvedPath))
                    {
                        if (TryResolveFaceFolder(resolvedPath, out string[] faceFiles))
                        {
                            result = BuildResultFromFaceFiles(faceFiles, props, resolvedPath);
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryResolveArchiveProperties(
            Dictionary<string, ZipArchiveEntry> entryMap,
            string propertiesKey,
            out ResolvedSkyboxSource result)
        {
            result = new ResolvedSkyboxSource();

            try
            {
                using Stream s = entryMap[propertiesKey].Open();
                using StreamReader sr = new(s);
                string text = sr.ReadToEnd();

                OptiFineSkyProperties props = ParseProperties(text);

                string propertiesDir = GetDirectoryPart(propertiesKey);
                string propertiesBaseName = Path.GetFileNameWithoutExtension(propertiesKey);

                foreach (string candidate in BuildSourceCandidates(props, propertiesBaseName))
                {
                    string normalized = NormalizeArchiveCombined(propertiesDir, candidate);

                    if (entryMap.TryGetValue(normalized, out ZipArchiveEntry? imageEntry) &&
                        IsSupportedImageExtension(Path.GetExtension(normalized)))
                    {
                        string tempImage = ExtractEntry(imageEntry);
                        if (TryResolveImageFile(tempImage, props, out result))
                            return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static ResolvedSkyboxSource BuildResult(string[] faces, OptiFineSkyProperties? props, string debugName)
        {
            Vector3 axis = props?.Axis ?? Vector3.UnitY;
            if (axis.LengthSquared() < 0.0001f)
                axis = Vector3.UnitY;

            axis = Vector3.Normalize(axis);

            float rotationDps = props is not null && props.Rotate
                ? MathF.Max(0f, props.Speed) * 8.0f
                : 0f;

            return new ResolvedSkyboxSource
            {
                FaceFiles = faces,
                Rotate = rotationDps > 0.0001f,
                RotationAxis = axis,
                RotationSpeedDegreesPerSecond = rotationDps,
                DebugName = debugName
            };
        }

        private static ResolvedSkyboxSource BuildResultFromFaceFiles(
            string[] faceFiles,
            OptiFineSkyProperties? props,
            string debugName)
        {
            Bitmap[] rawFaces = faceFiles.Select(path =>
            {
                using Bitmap src = new(path);
                return CopyToArgb32(src);
            }).ToArray();

            Bitmap[] canonical = ChooseBestCanonicalFaces(new List<Bitmap[]> { rawFaces });
            string[] saved = SaveFaceBitmaps(canonical);

            return BuildResult(saved, props, debugName);
        }

        private static IEnumerable<string> BuildSourceCandidates(OptiFineSkyProperties props, string defaultBaseName)
        {
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(props.Source))
            {
                foreach (string c in ExpandWithExtFallback(props.Source!, seen))
                    yield return c;
            }
            else
            {
                foreach (string c in ExpandWithExtFallback(defaultBaseName, seen))
                    yield return c;
            }
        }

        private static IEnumerable<string> ExpandWithExtFallback(string raw, HashSet<string> seen)
        {
            if (string.IsNullOrWhiteSpace(raw))
                yield break;

            if (seen.Add(raw))
                yield return raw;

            if (!Path.HasExtension(raw))
            {
                foreach (string ext in ImageExtensions)
                {
                    string v = raw + ext;
                    if (seen.Add(v))
                        yield return v;
                }
            }
        }

        private static OptiFineSkyProperties ParseProperties(string text)
        {
            Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);

            using StringReader reader = new(text);

            while (reader.ReadLine() is string line)
            {
                line = line.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                int idx = line.IndexOf('=');
                if (idx <= 0)
                    continue;

                string key = line[..idx].Trim();
                string value = line[(idx + 1)..].Trim();
                map[key] = value;
            }

            OptiFineSkyProperties p = new();

            if (map.TryGetValue("source", out string? source))
                p.Source = source;

            if (map.TryGetValue("rotate", out string? rotate))
                p.Rotate = ParseBool(rotate);

            if (map.TryGetValue("speed", out string? speed) &&
                float.TryParse(speed, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
            {
                p.Speed = f;
            }

            if (map.TryGetValue("axis", out string? axis))
                p.Axis = ParseAxis(axis);

            return p;
        }

        private static bool ParseBool(string value)
        {
            value = value.Trim().ToLowerInvariant();
            return value is "true" or "1" or "yes" or "on";
        }

        private static Vector3 ParseAxis(string raw)
        {
            string[] parts = raw
                .Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
                return Vector3.UnitY;

            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                Vector3 v = new(x, y, z);
                return v.LengthSquared() < 0.0001f ? Vector3.UnitY : Vector3.Normalize(v);
            }

            return Vector3.UnitY;
        }

        private static bool TryResolveFaceFolder(string dir, out string[] faceFiles)
        {
            foreach (FaceSet set in CandidateSets)
            {
                string[] files =
                {
                    Path.Combine(dir, set.Front),
                    Path.Combine(dir, set.Right),
                    Path.Combine(dir, set.Back),
                    Path.Combine(dir, set.Left),
                    Path.Combine(dir, set.Up),
                    Path.Combine(dir, set.Down)
                };

                if (files.All(File.Exists))
                {
                    faceFiles = files;
                    return true;
                }
            }

            faceFiles = Array.Empty<string>();
            return false;
        }

        private static bool TryResolveArchiveFaceSet(
            Dictionary<string, ZipArchiveEntry> entryMap,
            string dir,
            out ZipArchiveEntry[] entries)
        {
            foreach (FaceSet set in CandidateSets)
            {
                string[] keys =
                {
                    CombineArchivePath(dir, set.Front),
                    CombineArchivePath(dir, set.Right),
                    CombineArchivePath(dir, set.Back),
                    CombineArchivePath(dir, set.Left),
                    CombineArchivePath(dir, set.Up),
                    CombineArchivePath(dir, set.Down)
                };

                if (keys.All(k => entryMap.ContainsKey(k)))
                {
                    entries = keys.Select(k => entryMap[k]).ToArray();
                    return true;
                }
            }

            entries = Array.Empty<ZipArchiveEntry>();
            return false;
        }

        private static bool TryResolveImageFile(
            string imagePath,
            OptiFineSkyProperties? props,
            out ResolvedSkyboxSource result)
        {
            result = new ResolvedSkyboxSource();

            try
            {
                using Bitmap srcRaw = new(imagePath);
                using Bitmap src = CopyToArgb32(srcRaw);

                if (!TryConvertBitmapToFaceCandidates(src, out List<Bitmap[]> candidates))
                    return false;

                Bitmap[] canonical = ChooseBestCanonicalFaces(candidates);
                string[] files = SaveFaceBitmaps(canonical);

                result = BuildResult(files, props, imagePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryConvertBitmapToFaceCandidates(Bitmap src, out List<Bitmap[]> candidates)
        {
            candidates = new();

            int w = src.Width;
            int h = src.Height;

            if (w == h * 6)
            {
                int s = h;
                candidates.Add(new[]
                {
                    CloneRect(src, 0 * s, 0, s, s),
                    CloneRect(src, 1 * s, 0, s, s),
                    CloneRect(src, 2 * s, 0, s, s),
                    CloneRect(src, 3 * s, 0, s, s),
                    CloneRect(src, 4 * s, 0, s, s),
                    CloneRect(src, 5 * s, 0, s, s),
                });

                return true;
            }

            if (h == w * 6)
            {
                int s = w;
                candidates.Add(new[]
                {
                    CloneRect(src, 0, 0 * s, s, s),
                    CloneRect(src, 0, 1 * s, s, s),
                    CloneRect(src, 0, 2 * s, s, s),
                    CloneRect(src, 0, 3 * s, s, s),
                    CloneRect(src, 0, 4 * s, s, s),
                    CloneRect(src, 0, 5 * s, s, s),
                });

                return true;
            }

            if (w % 4 == 0 && h % 3 == 0 && w / 4 == h / 3)
            {
                int s = w / 4;

                candidates.Add(new[]
                {
                    CloneRect(src, 1 * s, 1 * s, s, s), // front
                    CloneRect(src, 2 * s, 1 * s, s, s), // right
                    CloneRect(src, 3 * s, 1 * s, s, s), // back
                    CloneRect(src, 0 * s, 1 * s, s, s), // left
                    CloneRect(src, 1 * s, 0 * s, s, s), // up
                    CloneRect(src, 1 * s, 2 * s, s, s), // down
                });

                return true;
            }

            if (w % 3 == 0 && h % 2 == 0 && w / 3 == h / 2)
            {
                int s = w / 3;

                // candidate A: common cubemap layout
                candidates.Add(new[]
                {
                    CloneRect(src, 1 * s, 0 * s, s, s), // front
                    CloneRect(src, 2 * s, 0 * s, s, s), // right
                    CloneRect(src, 1 * s, 1 * s, s, s), // back
                    CloneRect(src, 0 * s, 0 * s, s, s), // left
                    CloneRect(src, 2 * s, 1 * s, s, s), // up
                    CloneRect(src, 0 * s, 1 * s, s, s), // down
                });

                // candidate B: common OptiFine-like alternate top/down placement
                candidates.Add(new[]
                {
                    CloneRect(src, 1 * s, 0 * s, s, s), // front
                    CloneRect(src, 2 * s, 0 * s, s, s), // right
                    CloneRect(src, 1 * s, 1 * s, s, s), // back
                    CloneRect(src, 0 * s, 0 * s, s, s), // left
                    CloneRect(src, 0 * s, 1 * s, s, s), // up
                    CloneRect(src, 2 * s, 1 * s, s, s), // down
                });

                return true;
            }

            if (w == h * 2)
            {
                candidates.Add(ConvertEquirectangularToFaces(src));
                return true;
            }

            return false;
        }

        private static Bitmap[] ConvertEquirectangularToFaces(Bitmap src)
        {
            int faceSize = Math.Max(64, Math.Min(1024, Math.Min(src.Width / 4, src.Height / 2)));
            Bitmap[] faces =
            {
                new Bitmap(faceSize, faceSize, PixelFormat.Format32bppArgb),
                new Bitmap(faceSize, faceSize, PixelFormat.Format32bppArgb),
                new Bitmap(faceSize, faceSize, PixelFormat.Format32bppArgb),
                new Bitmap(faceSize, faceSize, PixelFormat.Format32bppArgb),
                new Bitmap(faceSize, faceSize, PixelFormat.Format32bppArgb),
                new Bitmap(faceSize, faceSize, PixelFormat.Format32bppArgb),
            };

            using LockedBitmapSampler sampler = new(src);

            for (int face = 0; face < 6; face++)
            {
                Bitmap dst = faces[face];

                for (int y = 0; y < faceSize; y++)
                {
                    float v = 1f - (2f * (y + 0.5f) / faceSize);

                    for (int x = 0; x < faceSize; x++)
                    {
                        float u = (2f * (x + 0.5f) / faceSize) - 1f;

                        Vector3 dir = GetFaceDirection(face, u, v);

                        float lon = MathF.Atan2(dir.X, dir.Z);
                        float lat = MathF.Asin(Math.Clamp(dir.Y, -1f, 1f));

                        float srcU = 0.5f + lon / (2f * MathF.PI);
                        float srcV = 0.5f - lat / MathF.PI;

                        Color c = sampler.SampleWrappedLinear(srcU, srcV);
                        dst.SetPixel(x, y, c);
                    }
                }
            }

            return faces;
        }

        private static Vector3 GetFaceDirection(int face, float u, float v)
        {
            Vector3 d = face switch
            {
                0 => new Vector3(u, v, 1f),
                1 => new Vector3(1f, v, -u),
                2 => new Vector3(-u, v, -1f),
                3 => new Vector3(-1f, v, u),
                4 => new Vector3(u, 1f, -v),
                5 => new Vector3(u, -1f, v),
                _ => Vector3.UnitZ
            };

            return Vector3.Normalize(d);
        }

        private static Bitmap[] ChooseBestCanonicalFaces(List<Bitmap[]> rawCandidates)
        {
            Bitmap[]? bestFaces = null;
            double bestScore = double.MaxValue;

            try
            {
                foreach (Bitmap[] raw in rawCandidates)
                {
                    foreach (Bitmap[] ordered in ExpandSideRotations(raw))
                    {
                        foreach (RotateFlipType[] preset in OrientationPresets)
                        {
                            Bitmap[] transformed = ApplyPreset(ordered, preset);
                            double score = ComputeSeamScore(transformed);

                            if (score < bestScore)
                            {
                                if (bestFaces != null)
                                {
                                    foreach (Bitmap oldBmp in bestFaces)
                                        oldBmp.Dispose();
                                }

                                bestScore = score;
                                bestFaces = CloneFaces(transformed);
                            }

                            foreach (Bitmap bmp in transformed)
                                bmp.Dispose();
                        }
                    }
                }

                if (bestFaces != null)
                    return bestFaces;

                throw new InvalidOperationException("No valid skybox face candidates were generated.");
            }
            finally
            {
                foreach (Bitmap[] candidate in rawCandidates)
                {
                    foreach (Bitmap bmp in candidate)
                        bmp.Dispose();
                }
            }
        }

        private static IEnumerable<Bitmap[]> ExpandSideRotations(Bitmap[] faces)
        {
            // faces are in logical order: front, right, back, left, up, down
            for (int shift = 0; shift < 4; shift++)
            {
                yield return new[]
                {
                    faces[(0 + shift) % 4],
                    faces[(1 + shift) % 4],
                    faces[(2 + shift) % 4],
                    faces[(3 + shift) % 4],
                    faces[4],
                    faces[5]
                };
            }
        }

        private static Bitmap[] ApplyPreset(Bitmap[] faces, RotateFlipType[] preset)
        {
            Bitmap[] result = new Bitmap[6];

            for (int i = 0; i < 6; i++)
            {
                Bitmap clone = CopyToArgb32(faces[i]);
                clone.RotateFlip(preset[i]);
                result[i] = clone;
            }

            return result;
        }

        private static Bitmap[] CloneFaces(Bitmap[] faces)
        {
            Bitmap[] result = new Bitmap[faces.Length];
            for (int i = 0; i < faces.Length; i++)
                result[i] = CopyToArgb32(faces[i]);
            return result;
        }

        private static double ComputeSeamScore(Bitmap[] f)
        {
            // logical order:
            // 0 front, 1 right, 2 back, 3 left, 4 up, 5 down
            double score = 0;

            score += CompareEdges(f[0], Edge.Top,    f[4], Edge.Bottom);
            score += CompareEdges(f[0], Edge.Bottom, f[5], Edge.Top);
            score += CompareEdges(f[0], Edge.Left,   f[3], Edge.Right);
            score += CompareEdges(f[0], Edge.Right,  f[1], Edge.Left);

            score += CompareEdges(f[2], Edge.Top,    f[4], Edge.Top);
            score += CompareEdges(f[2], Edge.Bottom, f[5], Edge.Bottom);
            score += CompareEdges(f[2], Edge.Left,   f[1], Edge.Right);
            score += CompareEdges(f[2], Edge.Right,  f[3], Edge.Left);

            score += CompareEdges(f[1], Edge.Top,    f[4], Edge.Right);
            score += CompareEdges(f[1], Edge.Bottom, f[5], Edge.Right);

            score += CompareEdges(f[3], Edge.Top,    f[4], Edge.Left);
            score += CompareEdges(f[3], Edge.Bottom, f[5], Edge.Left);

            return score;
        }

        private static double CompareEdges(Bitmap a, Edge ea, Bitmap b, Edge eb)
        {
            int n = Math.Min(GetEdgeLength(a, ea), GetEdgeLength(b, eb));
            int step = Math.Max(1, n / 128);

            double direct = 0;
            double reversed = 0;
            int count = 0;

            for (int i = 0; i < n; i += step)
            {
                Color ca = GetEdgePixel(a, ea, i);
                Color cb = GetEdgePixel(b, eb, i);
                Color cbr = GetEdgePixel(b, eb, n - 1 - i);

                direct += ColorDiff(ca, cb);
                reversed += ColorDiff(ca, cbr);
                count++;
            }

            if (count == 0)
                return 0;

            return Math.Min(direct, reversed) / count;
        }

        private static int GetEdgeLength(Bitmap bmp, Edge edge)
            => edge is Edge.Left or Edge.Right ? bmp.Height : bmp.Width;

        private static Color GetEdgePixel(Bitmap bmp, Edge edge, int i)
        {
            return edge switch
            {
                Edge.Left => bmp.GetPixel(0, i),
                Edge.Right => bmp.GetPixel(bmp.Width - 1, i),
                Edge.Top => bmp.GetPixel(i, 0),
                Edge.Bottom => bmp.GetPixel(i, bmp.Height - 1),
                _ => Color.Black
            };
        }

        private static double ColorDiff(Color a, Color b)
        {
            int dr = a.R - b.R;
            int dg = a.G - b.G;
            int db = a.B - b.B;
            return dr * dr + dg * dg + db * db;
        }

        private static string[] SaveFaceBitmaps(Bitmap[] faces)
        {
            string tempDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RefreshToAccess2",
                "SkyboxCache",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(tempDir);

            string[] files = new string[faces.Length];

            try
            {
                for (int i = 0; i < faces.Length; i++)
                {
                    string path = Path.Combine(tempDir, $"face_{i}.png");
                    files[i] = path;
                    faces[i].Save(path, ImageFormat.Png);
                }
            }
            finally
            {
                foreach (Bitmap bmp in faces)
                    bmp.Dispose();
            }

            return files;
        }

        private static string[] ExtractEntries(ZipArchiveEntry[] entries)
        {
            string tempDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RefreshToAccess2",
                "SkyboxCache",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(tempDir);

            string[] files = new string[entries.Length];

            for (int i = 0; i < entries.Length; i++)
            {
                string outPath = Path.Combine(tempDir, $"face_{i}.png");
                files[i] = outPath;

                using Stream src = entries[i].Open();
                using FileStream dst = File.Create(outPath);
                src.CopyTo(dst);
            }

            return files;
        }

        private static string ExtractEntry(ZipArchiveEntry entry)
        {
            string tempDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RefreshToAccess2",
                "SkyboxCache",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(tempDir);

            string ext = Path.GetExtension(entry.Name);
            string outPath = Path.Combine(tempDir, $"source{ext}");

            using Stream src = entry.Open();
            using FileStream dst = File.Create(outPath);
            src.CopyTo(dst);

            return outPath;
        }

        private static Bitmap CloneRect(Bitmap src, int x, int y, int w, int h)
            => src.Clone(new Rectangle(x, y, w, h), PixelFormat.Format32bppArgb);

        private static Bitmap CopyToArgb32(Bitmap src)
        {
            Bitmap dst = new(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(dst);
            g.DrawImage(src, 0, 0, src.Width, src.Height);
            return dst;
        }

        private static bool IsSupportedImageExtension(string ext)
            => ImageExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));

        private static bool LooksLikeSkyFolderName(string path)
        {
            string name = Path.GetFileName(path).ToLowerInvariant();
            return name.Contains("sky") || name.Contains("panorama") || name.Contains("background");
        }

        private static bool LooksLikeSkyImageName(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            return name.Contains("sky") ||
                   name.Contains("panorama") ||
                   name.Contains("background") ||
                   name.Contains("cubemap") ||
                   name.Contains("cube") ||
                   name.Contains("cloud");
        }

        private static bool IsPackRoot(string folder)
        {
            try
            {
                return File.Exists(Path.Combine(folder, "pack.mcmeta")) ||
                       Directory.Exists(Path.Combine(folder, "assets"));
            }
            catch
            {
                return false;
            }
        }

        private static string[] GetDirectImages(string folder)
        {
            try
            {
                return Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => IsSupportedImageExtension(Path.GetExtension(f)))
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static IEnumerable<string> OrderCandidateImages(IEnumerable<string> images)
        {
            return images
                .OrderByDescending(ScoreImageCandidate)
                .ThenByDescending(GetFileSizeSafe)
                .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeArchivePath(string path)
            => path.Replace('\\', '/').Trim();

        private static string NormalizeArchiveCombined(string baseDir, string path)
        {
            string raw = path.Replace('\\', '/');

            string combined = raw.StartsWith("/")
                ? raw.TrimStart('/')
                : CombineArchivePath(baseDir, raw);

            List<string> parts = new();

            foreach (string part in combined.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part == ".")
                    continue;

                if (part == "..")
                {
                    if (parts.Count > 0)
                        parts.RemoveAt(parts.Count - 1);

                    continue;
                }

                parts.Add(part);
            }

            return string.Join("/", parts);
        }

        private static string CombineArchivePath(string dir, string file)
        {
            if (string.IsNullOrWhiteSpace(dir))
                return NormalizeArchivePath(file).TrimStart('/');

            return $"{NormalizeArchivePath(dir).TrimEnd('/')}/{NormalizeArchivePath(file).TrimStart('/')}";
        }

        private static string GetDirectoryPart(string path)
        {
            string n = NormalizeArchivePath(path);
            int idx = n.LastIndexOf('/');
            return idx < 0 ? string.Empty : n[..idx];
        }

        private static IEnumerable<string> EnumerateDirectoriesSafe(string root)
        {
            Queue<string> queue = new();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();

                string[] subdirs;
                try
                {
                    subdirs = Directory.GetDirectories(current);
                }
                catch
                {
                    continue;
                }

                foreach (string subdir in subdirs)
                {
                    yield return subdir;
                    queue.Enqueue(subdir);
                }
            }
        }

        private static IEnumerable<string> EnumerateDirectoriesAndSelfSafe(string root)
        {
            yield return root;

            foreach (string dir in EnumerateDirectoriesSafe(root))
                yield return dir;
        }

        private static IEnumerable<string> EnumerateFilesSafe(string root, string pattern)
        {
            Queue<string> queue = new();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();

                string[] files = Array.Empty<string>();
                string[] subdirs = Array.Empty<string>();

                try { files = Directory.GetFiles(current, pattern); } catch { }
                try { subdirs = Directory.GetDirectories(current); } catch { }

                foreach (string file in files)
                    yield return file;

                foreach (string subdir in subdirs)
                    queue.Enqueue(subdir);
            }
        }

        private static int ScoreImageCandidate(string path)
        {
            string s = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            int score = 0;

            if (s.Contains("panorama")) score += 100;
            if (s.Contains("sky")) score += 80;
            if (s.Contains("background")) score += 60;
            if (s.Contains("cubemap")) score += 40;
            if (s.Contains("cube")) score += 25;
            if (s.Contains("cloud")) score += 15;
            if (s.Contains("glass")) score -= 100;
            if (s.Contains("gui")) score -= 50;

            return score;
        }

        private static long GetFileSizeSafe(string path)
        {
            try { return new FileInfo(path).Length; }
            catch { return 0; }
        }

        private static int ExtractTrailingNumber(string path)
        {
            string s = Path.GetFileNameWithoutExtension(path);
            int i = s.Length - 1;

            while (i >= 0 && char.IsDigit(s[i]))
                i--;

            string digits = s[(i + 1)..];
            return int.TryParse(digits, out int n) ? n : int.MaxValue;
        }

        private sealed unsafe class LockedBitmapSampler : IDisposable
        {
            private readonly Bitmap _bitmap;
            private readonly BitmapData _data;
            public int Width { get; }
            public int Height { get; }

            public LockedBitmapSampler(Bitmap bitmap)
            {
                _bitmap = bitmap;
                Width = bitmap.Width;
                Height = bitmap.Height;
                _data = bitmap.LockBits(
                    new Rectangle(0, 0, Width, Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);
            }

            public Color SampleWrappedLinear(float u, float v)
            {
                while (u < 0f) u += 1f;
                while (u >= 1f) u -= 1f;

                v = Math.Clamp(v, 0f, 1f);

                float fx = u * (Width - 1);
                float fy = v * (Height - 1);

                int x0 = (int)MathF.Floor(fx);
                int y0 = (int)MathF.Floor(fy);
                int x1 = (x0 + 1) % Width;
                int y1 = Math.Min(y0 + 1, Height - 1);

                float tx = fx - x0;
                float ty = fy - y0;

                Color c00 = GetPixel(x0, y0);
                Color c10 = GetPixel(x1, y0);
                Color c01 = GetPixel(x0, y1);
                Color c11 = GetPixel(x1, y1);

                Color cx0 = Lerp(c00, c10, tx);
                Color cx1 = Lerp(c01, c11, tx);
                return Lerp(cx0, cx1, ty);
            }

            private Color GetPixel(int x, int y)
            {
                byte* p = (byte*)_data.Scan0 + y * _data.Stride + x * 4;
                return Color.FromArgb(p[3], p[2], p[1], p[0]);
            }

            private static Color Lerp(Color a, Color b, float t)
            {
                int aa = (int)MathF.Round(a.A + (b.A - a.A) * t);
                int rr = (int)MathF.Round(a.R + (b.R - a.R) * t);
                int gg = (int)MathF.Round(a.G + (b.G - a.G) * t);
                int bb = (int)MathF.Round(a.B + (b.B - a.B) * t);
                return Color.FromArgb(aa, rr, gg, bb);
            }

            public void Dispose()
            {
                _bitmap.UnlockBits(_data);
            }
        }
    }
}
