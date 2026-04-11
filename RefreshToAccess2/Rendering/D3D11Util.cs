using System;
using Vortice.D3DCompiler;
using Vortice.Direct3D11;

namespace RefreshToAccess2.Rendering
{
    internal static unsafe class D3D11Util
    {
        public static ID3D11Buffer CreateImmutableBuffer<T>(
            ID3D11Device device,
            BindFlags bindFlags,
            T[] data) where T : unmanaged
        {
            fixed (T* pData = data)
            {
                BufferDescription desc = new(
                    (uint)(sizeof(T) * data.Length),
                    bindFlags,
                    ResourceUsage.Immutable,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    0);

                SubresourceData sub = new((IntPtr)pData, 0u, 0u);
                return device.CreateBuffer(desc, sub);
            }
        }

        public static ID3D11Buffer CreateDynamicConstantBuffer<T>(ID3D11Device device)
            where T : unmanaged
        {
            BufferDescription desc = new(
                (uint)Align16(sizeof(T)),
                BindFlags.ConstantBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write,
                ResourceOptionFlags.None,
                0);

            return device.CreateBuffer(desc);
        }

        public static void UpdateDynamicBuffer<T>(
            ID3D11DeviceContext context,
            ID3D11Buffer buffer,
            in T value) where T : unmanaged
        {
            MappedSubresource mapped = context.Map(buffer, 0, MapMode.WriteDiscard, MapFlags.None);
            *(T*)mapped.DataPointer = value;
            context.Unmap(buffer, 0);
        }

        public static byte[] Compile(string source, string entry, string profile)
        {
            return Compiler
                .Compile(source, entry, "shader.hlsl", profile, ShaderFlags.None, EffectFlags.None)
                .ToArray();
        }

        public static int Align16(int value) => (value + 15) & ~15;
    }
}
