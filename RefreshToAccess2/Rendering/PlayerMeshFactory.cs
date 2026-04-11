using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace RefreshToAccess2.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PreviewVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;

        public PreviewVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            Uv = uv;
        }
    }

    internal sealed class PreviewMeshData
    {
        public List<PreviewVertex> Vertices { get; } = new();
        public List<uint> Indices { get; } = new();
    }

    internal sealed class PlayerRigDefinition
    {
        public PreviewMeshData HeadBase { get; set; } = new();
        public PreviewMeshData HeadLayer { get; set; } = new();

        public PreviewMeshData BodyBase { get; set; } = new();
        public PreviewMeshData BodyLayer { get; set; } = new();

        public PreviewMeshData RightArmBase { get; set; } = new();
        public PreviewMeshData RightArmLayer { get; set; } = new();

        public PreviewMeshData LeftArmBase { get; set; } = new();
        public PreviewMeshData LeftArmLayer { get; set; } = new();

        public PreviewMeshData RightLegBase { get; set; } = new();
        public PreviewMeshData RightLegLayer { get; set; } = new();

        public PreviewMeshData LeftLegBase { get; set; } = new();
        public PreviewMeshData LeftLegLayer { get; set; } = new();
    }

    internal static class PlayerMeshFactory
    {
        private readonly record struct UvRect(float X, float Y, float W, float H);
        private readonly record struct CubeUv(
            UvRect Right,
            UvRect Left,
            UvRect Top,
            UvRect Bottom,
            UvRect Front,
            UvRect Back);

        private const float TexSize = 64f;

        public static PreviewMeshData BuildPlane()
        {
            PreviewMeshData mesh = new();
            float s = 400f;

            uint start = 0;
            mesh.Vertices.Add(new PreviewVertex(new Vector3(-s, 0, -s), Vector3.UnitY, new Vector2(0, 0)));
            mesh.Vertices.Add(new PreviewVertex(new Vector3(-s, 0,  s), Vector3.UnitY, new Vector2(0, 1)));
            mesh.Vertices.Add(new PreviewVertex(new Vector3( s, 0, -s), Vector3.UnitY, new Vector2(1, 0)));
            mesh.Vertices.Add(new PreviewVertex(new Vector3( s, 0,  s), Vector3.UnitY, new Vector2(1, 1)));

            mesh.Indices.Add(start + 0);
            mesh.Indices.Add(start + 1);
            mesh.Indices.Add(start + 2);
            mesh.Indices.Add(start + 2);
            mesh.Indices.Add(start + 1);
            mesh.Indices.Add(start + 3);

            return mesh;
        }

        public static PlayerRigDefinition BuildPlayerRig(bool slimArms, bool legacySkin)
        {
            const float px = 1f / 16f;
            float armW = slimArms ? 3f * px : 4f * px;
            float inflate = 0.25f * px;

            Vector3 headCenter = new(0, 4 * px, 0);   // pivot at neck base
            Vector3 bodyCenter = new(0, 6 * px, 0);   // pivot at body bottom
            Vector3 limbCenter = new(0, -6 * px, 0);  // pivot at top of limb

            PlayerRigDefinition rig = new()
            {
                HeadBase = CreateCuboidMesh(headCenter, new Vector3(8 * px, 8 * px, 8 * px), HeadBase()),
                HeadLayer = CreateCuboidMesh(headCenter, new Vector3(8 * px, 8 * px, 8 * px), HeadLayer(), inflate),

                BodyBase = CreateCuboidMesh(bodyCenter, new Vector3(8 * px, 12 * px, 4 * px), BodyBase()),
                BodyLayer = CreateCuboidMesh(bodyCenter, new Vector3(8 * px, 12 * px, 4 * px), BodyLayer(), inflate),

                RightArmBase = CreateCuboidMesh(limbCenter, new Vector3(armW, 12 * px, 4 * px), RightArmBase(slimArms)),
                RightArmLayer = CreateCuboidMesh(limbCenter, new Vector3(armW, 12 * px, 4 * px), RightArmLayer(slimArms), inflate),

                LeftArmBase = CreateCuboidMesh(limbCenter, new Vector3(armW, 12 * px, 4 * px),
                    legacySkin ? RightArmBase(slimArms) : LeftArmBase(slimArms)),
                LeftArmLayer = CreateCuboidMesh(limbCenter, new Vector3(armW, 12 * px, 4 * px), LeftArmLayer(slimArms), inflate),

                RightLegBase = CreateCuboidMesh(limbCenter, new Vector3(4 * px, 12 * px, 4 * px), RightLegBase()),
                RightLegLayer = CreateCuboidMesh(limbCenter, new Vector3(4 * px, 12 * px, 4 * px), RightLegLayer(), inflate),

                LeftLegBase = CreateCuboidMesh(limbCenter, new Vector3(4 * px, 12 * px, 4 * px),
                    legacySkin ? RightLegBase() : LeftLegBase()),
                LeftLegLayer = CreateCuboidMesh(limbCenter, new Vector3(4 * px, 12 * px, 4 * px), LeftLegLayer(), inflate)
            };

            return rig;
        }

        private static PreviewMeshData CreateCuboidMesh(Vector3 center, Vector3 size, CubeUv uv, float inflate = 0f)
        {
            PreviewMeshData mesh = new();
            AddCuboid(mesh, center, size, uv, inflate);
            return mesh;
        }

        private static void AddCuboid(PreviewMeshData mesh, Vector3 center, Vector3 size, CubeUv uv, float inflate = 0f)
        {
            Vector3 half = size * 0.5f + new Vector3(inflate);

            float minX = center.X - half.X;
            float maxX = center.X + half.X;
            float minY = center.Y - half.Y;
            float maxY = center.Y + half.Y;
            float minZ = center.Z - half.Z;
            float maxZ = center.Z + half.Z;

            AddFace(mesh,
                new Vector3(maxX, maxY, maxZ), new Vector3(maxX, minY, maxZ),
                new Vector3(maxX, maxY, minZ), new Vector3(maxX, minY, minZ),
                Vector3.UnitX, uv.Right);

            AddFace(mesh,
                new Vector3(minX, maxY, minZ), new Vector3(minX, minY, minZ),
                new Vector3(minX, maxY, maxZ), new Vector3(minX, minY, maxZ),
                -Vector3.UnitX, uv.Left);

            AddFace(mesh,
                new Vector3(minX, maxY, minZ), new Vector3(minX, maxY, maxZ),
                new Vector3(maxX, maxY, minZ), new Vector3(maxX, maxY, maxZ),
                Vector3.UnitY, uv.Top);

            AddFace(mesh,
                new Vector3(minX, minY, maxZ), new Vector3(minX, minY, minZ),
                new Vector3(maxX, minY, maxZ), new Vector3(maxX, minY, minZ),
                -Vector3.UnitY, uv.Bottom);

            AddFace(mesh,
                new Vector3(minX, maxY, maxZ), new Vector3(minX, minY, maxZ),
                new Vector3(maxX, maxY, maxZ), new Vector3(maxX, minY, maxZ),
                Vector3.UnitZ, uv.Front);

            AddFace(mesh,
                new Vector3(maxX, maxY, minZ), new Vector3(maxX, minY, minZ),
                new Vector3(minX, maxY, minZ), new Vector3(minX, minY, minZ),
                -Vector3.UnitZ, uv.Back);
        }

        private static void AddFace(
            PreviewMeshData mesh,
            Vector3 tl, Vector3 bl, Vector3 tr, Vector3 br,
            Vector3 normal,
            UvRect rect)
        {
            uint start = (uint)mesh.Vertices.Count;

            Vector2 uv0 = new(rect.X / TexSize, rect.Y / TexSize);
            Vector2 uv1 = new((rect.X + rect.W) / TexSize, (rect.Y + rect.H) / TexSize);

            mesh.Vertices.Add(new PreviewVertex(tl, normal, new Vector2(uv0.X, uv0.Y)));
            mesh.Vertices.Add(new PreviewVertex(bl, normal, new Vector2(uv0.X, uv1.Y)));
            mesh.Vertices.Add(new PreviewVertex(tr, normal, new Vector2(uv1.X, uv0.Y)));
            mesh.Vertices.Add(new PreviewVertex(br, normal, new Vector2(uv1.X, uv1.Y)));

            mesh.Indices.Add(start + 0);
            mesh.Indices.Add(start + 1);
            mesh.Indices.Add(start + 2);
            mesh.Indices.Add(start + 2);
            mesh.Indices.Add(start + 1);
            mesh.Indices.Add(start + 3);
        }

        private static CubeUv HeadBase() => new(
            new UvRect(0, 8, 8, 8),
            new UvRect(16, 8, 8, 8),
            new UvRect(8, 0, 8, 8),
            new UvRect(16, 0, 8, 8),
            new UvRect(8, 8, 8, 8),
            new UvRect(24, 8, 8, 8));

        private static CubeUv HeadLayer() => new(
            new UvRect(32, 8, 8, 8),
            new UvRect(48, 8, 8, 8),
            new UvRect(40, 0, 8, 8),
            new UvRect(48, 0, 8, 8),
            new UvRect(40, 8, 8, 8),
            new UvRect(56, 8, 8, 8));

        private static CubeUv BodyBase() => new(
            new UvRect(16, 20, 4, 12),
            new UvRect(28, 20, 4, 12),
            new UvRect(20, 16, 8, 4),
            new UvRect(28, 16, 8, 4),
            new UvRect(20, 20, 8, 12),
            new UvRect(32, 20, 8, 12));

        private static CubeUv BodyLayer() => new(
            new UvRect(16, 36, 4, 12),
            new UvRect(28, 36, 4, 12),
            new UvRect(20, 32, 8, 4),
            new UvRect(28, 32, 8, 4),
            new UvRect(20, 36, 8, 12),
            new UvRect(32, 36, 8, 12));

        private static CubeUv RightLegBase() => new(
            new UvRect(0, 20, 4, 12),
            new UvRect(8, 20, 4, 12),
            new UvRect(4, 16, 4, 4),
            new UvRect(8, 16, 4, 4),
            new UvRect(4, 20, 4, 12),
            new UvRect(12, 20, 4, 12));

        private static CubeUv RightLegLayer() => new(
            new UvRect(0, 36, 4, 12),
            new UvRect(8, 36, 4, 12),
            new UvRect(4, 32, 4, 4),
            new UvRect(8, 32, 4, 4),
            new UvRect(4, 36, 4, 12),
            new UvRect(12, 36, 4, 12));

        private static CubeUv LeftLegBase() => new(
            new UvRect(16, 52, 4, 12),
            new UvRect(24, 52, 4, 12),
            new UvRect(20, 48, 4, 4),
            new UvRect(24, 48, 4, 4),
            new UvRect(20, 52, 4, 12),
            new UvRect(28, 52, 4, 12));

        private static CubeUv LeftLegLayer() => new(
            new UvRect(0, 52, 4, 12),
            new UvRect(8, 52, 4, 12),
            new UvRect(4, 48, 4, 4),
            new UvRect(8, 48, 4, 4),
            new UvRect(4, 52, 4, 12),
            new UvRect(12, 52, 4, 12));

        private static CubeUv RightArmBase(bool slim) => slim
            ? new CubeUv(
                new UvRect(40, 20, 4, 12),
                new UvRect(47, 20, 4, 12),
                new UvRect(44, 16, 3, 4),
                new UvRect(47, 16, 3, 4),
                new UvRect(44, 20, 3, 12),
                new UvRect(51, 20, 3, 12))
            : new CubeUv(
                new UvRect(40, 20, 4, 12),
                new UvRect(48, 20, 4, 12),
                new UvRect(44, 16, 4, 4),
                new UvRect(48, 16, 4, 4),
                new UvRect(44, 20, 4, 12),
                new UvRect(52, 20, 4, 12));

        private static CubeUv RightArmLayer(bool slim) => slim
            ? new CubeUv(
                new UvRect(40, 36, 4, 12),
                new UvRect(47, 36, 4, 12),
                new UvRect(44, 32, 3, 4),
                new UvRect(47, 32, 3, 4),
                new UvRect(44, 36, 3, 12),
                new UvRect(51, 36, 3, 12))
            : new CubeUv(
                new UvRect(40, 36, 4, 12),
                new UvRect(48, 36, 4, 12),
                new UvRect(44, 32, 4, 4),
                new UvRect(48, 32, 4, 4),
                new UvRect(44, 36, 4, 12),
                new UvRect(52, 36, 4, 12));

        private static CubeUv LeftArmBase(bool slim) => slim
            ? new CubeUv(
                new UvRect(32, 52, 4, 12),
                new UvRect(39, 52, 4, 12),
                new UvRect(36, 48, 3, 4),
                new UvRect(39, 48, 3, 4),
                new UvRect(36, 52, 3, 12),
                new UvRect(43, 52, 3, 12))
            : new CubeUv(
                new UvRect(32, 52, 4, 12),
                new UvRect(40, 52, 4, 12),
                new UvRect(36, 48, 4, 4),
                new UvRect(40, 48, 4, 4),
                new UvRect(36, 52, 4, 12),
                new UvRect(44, 52, 4, 12));

        private static CubeUv LeftArmLayer(bool slim) => slim
            ? new CubeUv(
                new UvRect(48, 52, 4, 12),
                new UvRect(55, 52, 4, 12),
                new UvRect(52, 48, 3, 4),
                new UvRect(55, 48, 3, 4),
                new UvRect(52, 52, 3, 12),
                new UvRect(59, 52, 3, 12))
            : new CubeUv(
                new UvRect(48, 52, 4, 12),
                new UvRect(56, 52, 4, 12),
                new UvRect(52, 48, 4, 4),
                new UvRect(56, 48, 4, 4),
                new UvRect(52, 52, 4, 12),
                new UvRect(60, 52, 4, 12));
    }
}
