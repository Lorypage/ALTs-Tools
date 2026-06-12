using RefreshToAccess2.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Resources;
using WinForms = System.Windows.Forms;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Color4 = Vortice.Mathematics.Color4;
using Viewport = Vortice.Mathematics.Viewport;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace RefreshToAccess2.Rendering
{
    public sealed class PlayerPreviewRenderControl : WinForms.UserControl
    {
        private IDXGIFactory2? _factory;
        private ID3D11Device? _device;
        private ID3D11DeviceContext? _context;
        private IDXGISwapChain1? _swapChain;

        private ID3D11RenderTargetView? _rtv;
        private ID3D11Texture2D? _depthTex;
        private ID3D11DepthStencilView? _dsv;

        private ID3D11VertexShader? _mainVs;
        private ID3D11PixelShader? _mainPs;
        private ID3D11VertexShader? _shadowVs;
        private ID3D11PixelShader? _shadowPs;
        private ID3D11VertexShader? _gradientVs;
        private ID3D11PixelShader? _gradientPs;
        private ID3D11VertexShader? _skyVs;
        private ID3D11PixelShader? _skyPs;

        private ID3D11InputLayout? _mainLayout;
        private ID3D11InputLayout? _skyLayout;

        private ID3D11Buffer? _frameCb;
        private ID3D11Buffer? _objectCb;

        private ID3D11SamplerState? _pointClamp;
        private ID3D11SamplerState? _linearWrap;
        private ID3D11SamplerState? _linearClamp;
        private ID3D11RasterizerState? _solidRaster;
        private ID3D11RasterizerState? _shadowRaster;
        private ID3D11DepthStencilState? _depthEnabled;
        private ID3D11DepthStencilState? _depthDisabled;

        private ID3D11Texture2D? _shadowTex;
        private ID3D11DepthStencilView? _shadowDsv;
        private ID3D11ShaderResourceView? _shadowSrv;

        private ID3D11ShaderResourceView? _skinSrv;
        private ID3D11ShaderResourceView? _whiteSrv;
        private ID3D11ShaderResourceView? _cubeSrv;

        private MeshBuffer? _headBaseMesh;
        private MeshBuffer? _headLayerMesh;
        private MeshBuffer? _bodyBaseMesh;
        private MeshBuffer? _bodyLayerMesh;
        private MeshBuffer? _rightArmBaseMesh;
        private MeshBuffer? _rightArmLayerMesh;
        private MeshBuffer? _leftArmBaseMesh;
        private MeshBuffer? _leftArmLayerMesh;
        private MeshBuffer? _rightLegBaseMesh;
        private MeshBuffer? _rightLegLayerMesh;
        private MeshBuffer? _leftLegBaseMesh;
        private MeshBuffer? _leftLegLayerMesh;
        private MeshBuffer? _planeMesh;

        private ID3D11Buffer? _skyVb;
        private int _skyVertexCount;

        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private long _lastTicks;
        private bool _initialized;
        private bool _pendingResize = true;

        private byte[]? _pendingSkinPng;
        private string? _pendingPanoramaSource = BuiltInPanoramaCatalog.DefaultKey;
        private MinecraftSkinVariant _skinVariant = MinecraftSkinVariant.Classic;
        private PreviewAnimationMode _animationMode = PreviewAnimationMode.Auto;
        private PreviewBackgroundMode _backgroundMode = PreviewBackgroundMode.Bright;
        private bool _skinIsLegacy;

        private bool _skyRotate;
        private Vector3 _skyRotationAxis = Vector3.UnitY;
        private float _skyRotationSpeedDegreesPerSecond;

        private float _animationTime;

        private const float DefaultYaw = -0.55f;
        private const float DefaultPitch = 0.22f;
        private const float DefaultDistance = 4.25f;
        private const float MinCameraDistance = 1.4f;
        private const float MaxCameraDistance = 15f;
        private const float BaseSceneFov = 0.78f;
        private static readonly Vector3 DefaultFocus = new(0, 1.0f, 0);

        private float _yaw = DefaultYaw;
        private float _pitch = DefaultPitch;
        private float _distance = DefaultDistance;
        private Vector3 _focus = DefaultFocus;

        private float _yawTarget = DefaultYaw;
        private float _pitchTarget = DefaultPitch;
        private float _distanceTarget = DefaultDistance;
        private Vector3 _focusTarget = DefaultFocus;

        private bool _leftDown;
        private bool _rightDown;
        private Point _lastMouse;

        private System.Threading.Timer? _frameTimer;
        private bool _renderLoopActive;
        private int _renderQueued;
        private bool _rendering;

        public PlayerPreviewRenderControl()
        {
            SetStyle(
                WinForms.ControlStyles.AllPaintingInWmPaint |
                WinForms.ControlStyles.Opaque |
                WinForms.ControlStyles.UserPaint,
                true);

            DoubleBuffered = false;
            TabStop = true;

            MouseDown += OnMouseDownInternal;
            MouseUp += OnMouseUpInternal;
            MouseMove += OnMouseMoveInternal;
            MouseWheel += OnMouseWheelInternal;

            Resize += (_, _) => _pendingResize = true;
            VisibleChanged += (_, _) =>
            {
                if (!_initialized)
                    return;

                if (Visible)
                    StartRenderLoop();
                else
                    StopRenderLoop();
            };

            ResetCamera(true);
        }

        public void SetSkinPng(byte[]? pngBytes)
        {
            _pendingSkinPng = pngBytes;

            if (_initialized)
                LoadSkinTexture(_pendingSkinPng);
        }

        public void SetSkinVariant(MinecraftSkinVariant variant)
        {
            if (_skinVariant == variant)
                return;

            _skinVariant = variant;

            if (_initialized)
                RebuildPlayerMesh();
        }

        public void SetAnimationMode(PreviewAnimationMode mode)
        {
            _animationMode = mode;
        }

        public void SetBackgroundMode(PreviewBackgroundMode mode)
        {
            _backgroundMode = mode;

            if (_initialized &&
                mode == PreviewBackgroundMode.Panorama &&
                _cubeSrv == null)
            {
                LoadSkybox(_pendingPanoramaSource);
            }
        }

        public void SetPanoramaSource(string? sourcePath)
        {
            _pendingPanoramaSource = string.IsNullOrWhiteSpace(sourcePath)
                ? BuiltInPanoramaCatalog.DefaultKey
                : sourcePath;

            if (_initialized)
                LoadSkybox(_pendingPanoramaSource);
        }

        public void ResetCamera(bool snap)
        {
            _yawTarget = DefaultYaw;
            _pitchTarget = DefaultPitch;
            _distanceTarget = DefaultDistance;
            _focusTarget = DefaultFocus;

            if (snap)
            {
                _yaw = _yawTarget;
                _pitch = _pitchTarget;
                _distance = _distanceTarget;
                _focus = _focusTarget;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (!_initialized && !DesignMode)
                InitializeRenderer();
        }

        protected override void OnPaint(WinForms.PaintEventArgs e)
        {
        }

        protected override void OnPaintBackground(WinForms.PaintEventArgs pevent)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _initialized = false;
                StopRenderLoop();

                try
                {
                    _frameTimer?.Dispose();
                    _frameTimer = null;
                }
                catch { }

                ReleaseAll();
            }

            base.Dispose(disposing);
        }

        private static uint U(int value) => unchecked((uint)Math.Max(0, value));

        private void BindVertexBuffer(ID3D11Buffer buffer, uint stride)
        {
            _context!.IASetVertexBuffers(
                0,
                new ID3D11Buffer[] { buffer },
                new uint[] { stride },
                new uint[] { 0u });
        }

        // ~60 FPS. The renderer is fully time-step (dt) based, so this only
        // caps how often we draw — the animation speed is unchanged. The old
        // 1 ms period drove ~1000 redraws/sec, pegging a CPU core and flooding
        // the WPF dispatcher (which made the entire UI feel sluggish).
        private const int FrameIntervalMs = 16;

        private void StartRenderLoop()
        {
            _renderLoopActive = true;

            // Reset the dt baseline so a long pause (page hidden) doesn't
            // produce one giant time-step that snaps the animation forward.
            _lastTicks = _clock.ElapsedMilliseconds;

            if (_frameTimer == null)
            {
                _frameTimer = new System.Threading.Timer(
                    OnFrameTimerTick,
                    null,
                    0,
                    FrameIntervalMs);
            }
            else
            {
                _frameTimer.Change(0, FrameIntervalMs);
            }
        }

        private void StopRenderLoop()
        {
            _renderLoopActive = false;

            try
            {
                _frameTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch { }

            Interlocked.Exchange(ref _renderQueued, 0);
        }

        private void OnFrameTimerTick(object? state)
        {
            if (!_renderLoopActive ||
                !_initialized ||
                !IsHandleCreated ||
                IsDisposed ||
                Disposing ||
                !Visible)
            {
                return;
            }

            if (_rendering)
                return;

            if (Interlocked.Exchange(ref _renderQueued, 1) != 0)
                return;

            try
            {
                BeginInvoke((Action)(() =>
                {
                    Interlocked.Exchange(ref _renderQueued, 0);

                    if (!_renderLoopActive ||
                        !_initialized ||
                        IsDisposed ||
                        Disposing ||
                        !Visible ||
                        _rendering)
                    {
                        return;
                    }

                    _rendering = true;
                    try
                    {
                        RenderFrame();
                    }
                    finally
                    {
                        _rendering = false;
                    }
                }));
            }
            catch
            {
                Interlocked.Exchange(ref _renderQueued, 0);
            }
        }

        private void InitializeRenderer()
        {
            try
            {
                try
                {
                    D3D11CreateDevice(
                        null,
                        DriverType.Hardware,
                        DeviceCreationFlags.BgraSupport,
                        null,
                        out _device,
                        out _context);
                }
                catch
                {
                    D3D11CreateDevice(
                        null,
                        DriverType.Warp,
                        DeviceCreationFlags.BgraSupport,
                        null,
                        out _device,
                        out _context);
                }

                _factory = CreateDXGIFactory2<IDXGIFactory2>(false);

                SwapChainDescription1 swapDesc = new()
                {
                    Width = U(Math.Max(1, ClientSize.Width)),
                    Height = U(Math.Max(1, ClientSize.Height)),
                    Format = Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SampleDescription = new SampleDescription(1, 0),
                    BufferUsage = Usage.RenderTargetOutput,
                    BufferCount = 2,
                    Scaling = Scaling.Stretch,
                    SwapEffect = SwapEffect.FlipDiscard,
                    AlphaMode = AlphaMode.Ignore
                };

                _swapChain = _factory.CreateSwapChainForHwnd(_device, Handle, swapDesc);
                _factory.MakeWindowAssociation(Handle, WindowAssociationFlags.IgnoreAll);

                CreateBackBuffer();
                CreateStates();
                CreateShaders();
                CreateConstantBuffers();
                CreateShadowResources();
                CreateDefaultTextures();
                CreateSceneMeshes();

                LoadSkinTexture(_pendingSkinPng);
                LoadSkybox(_pendingPanoramaSource);

                _initialized = true;
                _lastTicks = _clock.ElapsedMilliseconds;

                // Only spin up the render loop if we're actually on-screen.
                // When the Skin page is collapsed, VisibleChanged will start it
                // once the page is shown — otherwise we'd burn CPU rendering a
                // hidden control for the whole session.
                if (Visible)
                    StartRenderLoop();
            }
            catch
            {
                ReleaseAll();
                throw;
            }
        }

        private void CreateBackBuffer()
        {
            DisposeRef(ref _rtv);
            DisposeRef(ref _dsv);
            DisposeRef(ref _depthTex);

            if (_swapChain == null || _device == null)
                return;

            using ID3D11Texture2D backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
            _rtv = _device.CreateRenderTargetView(backBuffer);

            Texture2DDescription depthDesc = new()
            {
                Width = U(Math.Max(1, ClientSize.Width)),
                Height = U(Math.Max(1, ClientSize.Height)),
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = BindFlags.DepthStencil
            };

            _depthTex = _device.CreateTexture2D(depthDesc);
            _dsv = _device.CreateDepthStencilView(_depthTex);
        }

        private void CreateStates()
        {
            if (_device == null)
                return;

            _pointClamp = _device.CreateSamplerState(SamplerDescription.PointClamp);
            _linearWrap = _device.CreateSamplerState(SamplerDescription.LinearWrap);
            _linearClamp = _device.CreateSamplerState(SamplerDescription.LinearClamp);

            RasterizerDescription solid = RasterizerDescription.CullNone;
            solid.FillMode = FillMode.Solid;
            solid.DepthClipEnable = true;
            _solidRaster = _device.CreateRasterizerState(solid);

            RasterizerDescription shadow = RasterizerDescription.CullNone;
            shadow.FillMode = FillMode.Solid;
            shadow.DepthClipEnable = true;
            shadow.DepthBias = 1200;
            shadow.SlopeScaledDepthBias = 2.5f;
            _shadowRaster = _device.CreateRasterizerState(shadow);

            _depthEnabled = _device.CreateDepthStencilState(DepthStencilDescription.Default);

            DepthStencilDescription depthOff = new()
            {
                DepthEnable = false,
                DepthWriteMask = DepthWriteMask.Zero,
                DepthFunc = ComparisonFunction.Always
            };
            _depthDisabled = _device.CreateDepthStencilState(depthOff);
        }

        private void CreateConstantBuffers()
        {
            if (_device == null)
                return;

            _frameCb = D3D11Util.CreateDynamicConstantBuffer<FrameConstants>(_device);
            _objectCb = D3D11Util.CreateDynamicConstantBuffer<ObjectConstants>(_device);
        }

        private void CreateShadowResources()
        {
            if (_device == null)
                return;

            Texture2DDescription shadowDesc = new()
            {
                Width = 2048u,
                Height = 2048u,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32_Typeless,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource
            };

            _shadowTex = _device.CreateTexture2D(shadowDesc);

            DepthStencilViewDescription dsvDesc = new()
            {
                Format = Format.D32_Float,
                ViewDimension = DepthStencilViewDimension.Texture2D,
                Texture2D = new Texture2DDepthStencilView()
                {
                    MipSlice = 0
                }
            };
            _shadowDsv = _device.CreateDepthStencilView(_shadowTex, dsvDesc);

            ShaderResourceViewDescription srvDesc = new()
            {
                Format = Format.R32_Float,
                ViewDimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new Texture2DShaderResourceView()
                {
                    MipLevels = 1,
                    MostDetailedMip = 0
                }
            };
            _shadowSrv = _device.CreateShaderResourceView(_shadowTex, srvDesc);
        }

        private void CreateDefaultTextures()
        {
            _whiteSrv = CreateSolidTexture(Color.White);
            _skinSrv = CreateSolidTexture(Color.LightGray);
        }

        private void CreateSceneMeshes()
        {
            RebuildPlayerMesh();
            RebuildPlaneMesh();
            RebuildSkyCube();
        }

        private void RebuildPlayerMesh()
        {
            DisposeRef(ref _headBaseMesh);
            DisposeRef(ref _headLayerMesh);
            DisposeRef(ref _bodyBaseMesh);
            DisposeRef(ref _bodyLayerMesh);
            DisposeRef(ref _rightArmBaseMesh);
            DisposeRef(ref _rightArmLayerMesh);
            DisposeRef(ref _leftArmBaseMesh);
            DisposeRef(ref _leftArmLayerMesh);
            DisposeRef(ref _rightLegBaseMesh);
            DisposeRef(ref _rightLegLayerMesh);
            DisposeRef(ref _leftLegBaseMesh);
            DisposeRef(ref _leftLegLayerMesh);

            if (_device == null)
                return;

            PlayerRigDefinition rig = PlayerMeshFactory.BuildPlayerRig(
                _skinVariant == MinecraftSkinVariant.Slim,
                _skinIsLegacy);

            _headBaseMesh = new MeshBuffer(_device, rig.HeadBase);
            _headLayerMesh = new MeshBuffer(_device, rig.HeadLayer);
            _bodyBaseMesh = new MeshBuffer(_device, rig.BodyBase);
            _bodyLayerMesh = new MeshBuffer(_device, rig.BodyLayer);
            _rightArmBaseMesh = new MeshBuffer(_device, rig.RightArmBase);
            _rightArmLayerMesh = new MeshBuffer(_device, rig.RightArmLayer);
            _leftArmBaseMesh = new MeshBuffer(_device, rig.LeftArmBase);
            _leftArmLayerMesh = new MeshBuffer(_device, rig.LeftArmLayer);
            _rightLegBaseMesh = new MeshBuffer(_device, rig.RightLegBase);
            _rightLegLayerMesh = new MeshBuffer(_device, rig.RightLegLayer);
            _leftLegBaseMesh = new MeshBuffer(_device, rig.LeftLegBase);
            _leftLegLayerMesh = new MeshBuffer(_device, rig.LeftLegLayer);
        }

        private void RebuildPlaneMesh()
        {
            DisposeRef(ref _planeMesh);

            if (_device == null)
                return;

            PreviewMeshData plane = PlayerMeshFactory.BuildPlane();
            _planeMesh = new MeshBuffer(_device, plane);
        }

        private void RebuildSkyCube()
        {
            DisposeRef(ref _skyVb);

            if (_device == null)
                return;

            Vector3[] verts =
            {
                new(-1,-1,-1), new(-1, 1,-1), new( 1, 1,-1), new(-1,-1,-1), new( 1, 1,-1), new( 1,-1,-1),
                new( 1,-1, 1), new( 1, 1, 1), new(-1, 1, 1), new( 1,-1, 1), new(-1, 1, 1), new(-1,-1, 1),
                new(-1,-1, 1), new(-1, 1, 1), new(-1, 1,-1), new(-1,-1, 1), new(-1, 1,-1), new(-1,-1,-1),
                new( 1,-1,-1), new( 1, 1,-1), new( 1, 1, 1), new( 1,-1,-1), new( 1, 1, 1), new( 1,-1, 1),
                new(-1, 1,-1), new(-1, 1, 1), new( 1, 1, 1), new(-1, 1,-1), new( 1, 1, 1), new( 1, 1,-1),
                new(-1,-1, 1), new(-1,-1,-1), new( 1,-1,-1), new(-1,-1, 1), new( 1,-1,-1), new( 1,-1, 1),
            };

            _skyVb = D3D11Util.CreateImmutableBuffer(_device, BindFlags.VertexBuffer, verts);
            _skyVertexCount = verts.Length;
        }

        private void CreateShaders()
        {
            if (_device == null)
                return;

            string mainShader = @"
cbuffer FrameBuffer : register(b0)
{
    row_major float4x4 ViewProj;
    row_major float4x4 LightViewProj;
    row_major float4x4 SkyRotation;
    float4 LightDir;
    float4 CameraPos;
    float4 SkyTop;
    float4 SkyHorizon;
    float4 AmbientColor;
};

cbuffer ObjectBuffer : register(b1)
{
    row_major float4x4 World;
    float4 Tint;
    float4 Params;
};

struct VSIn
{
    float3 Pos    : POSITION;
    float3 Normal : NORMAL;
    float2 Uv     : TEXCOORD0;
};

struct VSOut
{
    float4 Pos       : SV_POSITION;
    float3 NormalW   : NORMAL0;
    float2 Uv        : TEXCOORD0;
    float3 WorldPos  : TEXCOORD1;
    float4 ShadowPos : TEXCOORD2;
};

Texture2D DiffuseTex : register(t0);
Texture2D ShadowTex  : register(t1);

SamplerState DiffuseSampler : register(s0);
SamplerState ShadowSampler  : register(s1);

VSOut VSMain(VSIn i)
{
    VSOut o;
    float4 worldPos = mul(float4(i.Pos, 1), World);
    o.Pos = mul(worldPos, ViewProj);
    o.ShadowPos = mul(worldPos, LightViewProj);
    o.WorldPos = worldPos.xyz;
    o.NormalW = normalize(mul(float4(i.Normal, 0), World).xyz);
    o.Uv = i.Uv;
    return o;
}

float ComputeShadow(float4 shadowPos, float3 normalW)
{
    float3 p = shadowPos.xyz / max(shadowPos.w, 0.00001);
    float2 uv = float2(p.x * 0.5 + 0.5, -p.y * 0.5 + 0.5);

    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1 || p.z <= 0 || p.z >= 1)
        return 1.0;

    float bias = max(0.0006, 0.004 * (1.0 - saturate(dot(normalize(normalW), -LightDir.xyz))));
    float2 texel = 1.0 / float2(2048.0, 2048.0);

    float accum = 0.0;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float depth = ShadowTex.Sample(ShadowSampler, uv + float2(x, y) * texel).r;
            accum += (p.z - bias <= depth) ? 1.0 : 0.45;
        }
    }

    return accum / 9.0;
}

float4 PSMain(VSOut i) : SV_TARGET
{
    float4 albedo = DiffuseTex.Sample(DiffuseSampler, i.Uv) * Tint;
    if (albedo.a < 0.02) discard;

    float3 n = normalize(i.NormalW);
    float ndl = saturate(dot(n, -LightDir.xyz));
    float shadow = Params.y > 0.5 ? ComputeShadow(i.ShadowPos, n) : 1.0;

    float3 lit = albedo.rgb * (AmbientColor.rgb + ndl * shadow * 0.88);

    if (Params.x > 0.5)
    {
        float dist = length(i.WorldPos.xz - CameraPos.xz);
        float fog = saturate(1.0 - exp(-dist * 0.018));
        lit = lerp(lit, SkyHorizon.rgb, fog);
    }

    return float4(lit, albedo.a);
}";

            string shadowShader = @"
cbuffer FrameBuffer : register(b0)
{
    row_major float4x4 ViewProj;
    row_major float4x4 LightViewProj;
    row_major float4x4 SkyRotation;
    float4 LightDir;
    float4 CameraPos;
    float4 SkyTop;
    float4 SkyHorizon;
    float4 AmbientColor;
};

cbuffer ObjectBuffer : register(b1)
{
    row_major float4x4 World;
    float4 Tint;
    float4 Params;
};

struct VSIn
{
    float3 Pos    : POSITION;
    float3 Normal : NORMAL;
    float2 Uv     : TEXCOORD0;
};

struct VSOut
{
    float4 Pos : SV_POSITION;
    float2 Uv  : TEXCOORD0;
};

Texture2D DiffuseTex : register(t0);
SamplerState DiffuseSampler : register(s0);

VSOut VSMain(VSIn i)
{
    VSOut o;
    float4 worldPos = mul(float4(i.Pos, 1), World);
    o.Pos = mul(worldPos, LightViewProj);
    o.Uv = i.Uv;
    return o;
}

void PSMain(VSOut i)
{
    float a = DiffuseTex.Sample(DiffuseSampler, i.Uv).a;
    clip(a - 0.02);
}";

            string gradientShader = @"
cbuffer FrameBuffer : register(b0)
{
    row_major float4x4 ViewProj;
    row_major float4x4 LightViewProj;
    row_major float4x4 SkyRotation;
    float4 LightDir;
    float4 CameraPos;
    float4 SkyTop;
    float4 SkyHorizon;
    float4 AmbientColor;
};

struct VSOut
{
    float4 Pos : SV_POSITION;
    float2 Uv  : TEXCOORD0;
};

VSOut VSMain(uint id : SV_VertexID)
{
    VSOut o;
    float2 pos = float2((id == 2) ? 3.0 : -1.0, (id == 1) ? 3.0 : -1.0);
    o.Pos = float4(pos, 0, 1);
    o.Uv = float2((pos.x + 1) * 0.5, 1 - ((pos.y + 1) * 0.5));
    return o;
}

float4 PSMain(VSOut i) : SV_TARGET
{
    float t = pow(saturate(i.Uv.y), 1.35);
    return float4(lerp(SkyHorizon.rgb, SkyTop.rgb, t), 1);
}";

            string skyShader = @"
cbuffer FrameBuffer : register(b0)
{
    row_major float4x4 ViewProj;
    row_major float4x4 LightViewProj;
    row_major float4x4 SkyRotation;
    float4 LightDir;
    float4 CameraPos;
    float4 SkyTop;
    float4 SkyHorizon;
    float4 AmbientColor;
};

Texture2DArray SkyTex : register(t0);
SamplerState SkySampler : register(s0);

struct VSIn
{
    float3 Pos : POSITION;
};

struct VSOut
{
    float4 Pos : SV_POSITION;
    float3 Dir : TEXCOORD0;
};

VSOut VSMain(VSIn i)
{
    VSOut o;
    float4 pos = mul(float4(i.Pos, 1), ViewProj);
    o.Pos = pos.xyww;
    o.Dir = mul(float4(i.Pos, 0), SkyRotation).xyz;
    return o;
}

float2 GetMinecraftPanoramaUv(float3 d, out float faceIndex)
{
    float3 a = abs(d);

    if (a.z >= a.x && a.z >= a.y)
    {
        if (d.z >= 0.0)
        {
            faceIndex = 0.0;
            return float2(
                0.5 + d.x / (2.0 * a.z),
                0.5 - d.y / (2.0 * a.z));
        }

        faceIndex = 2.0;
        return float2(
            0.5 - d.x / (2.0 * a.z),
            0.5 - d.y / (2.0 * a.z));
    }

    if (a.x >= a.y)
    {
        if (d.x >= 0.0)
        {
            faceIndex = 1.0;
            return float2(
                0.5 - d.z / (2.0 * a.x),
                0.5 - d.y / (2.0 * a.x));
        }

        faceIndex = 3.0;
        return float2(
            0.5 + d.z / (2.0 * a.x),
            0.5 - d.y / (2.0 * a.x));
    }

    if (d.y >= 0.0)
    {
        faceIndex = 4.0;
        return float2(
            0.5 + d.x / (2.0 * a.y),
            0.5 + d.z / (2.0 * a.y));
    }

    faceIndex = 5.0;
    return float2(
        0.5 + d.x / (2.0 * a.y),
        0.5 - d.z / (2.0 * a.y));
}

float4 PSMain(VSOut i) : SV_TARGET
{
    float3 d = normalize(i.Dir);

    float faceIndex;
    float2 uv = GetMinecraftPanoramaUv(d, faceIndex);

    uv = clamp(uv, 0.001, 0.999);

    return SkyTex.Sample(SkySampler, float3(uv, faceIndex));
}";

            byte[] mainVsBytes = D3D11Util.Compile(mainShader, "VSMain", "vs_5_0");
            byte[] mainPsBytes = D3D11Util.Compile(mainShader, "PSMain", "ps_5_0");
            byte[] shadowVsBytes = D3D11Util.Compile(shadowShader, "VSMain", "vs_5_0");
            byte[] shadowPsBytes = D3D11Util.Compile(shadowShader, "PSMain", "ps_5_0");
            byte[] gradientVsBytes = D3D11Util.Compile(gradientShader, "VSMain", "vs_5_0");
            byte[] gradientPsBytes = D3D11Util.Compile(gradientShader, "PSMain", "ps_5_0");
            byte[] skyVsBytes = D3D11Util.Compile(skyShader, "VSMain", "vs_5_0");
            byte[] skyPsBytes = D3D11Util.Compile(skyShader, "PSMain", "ps_5_0");

            _mainVs = _device.CreateVertexShader(mainVsBytes);
            _mainPs = _device.CreatePixelShader(mainPsBytes);
            _shadowVs = _device.CreateVertexShader(shadowVsBytes);
            _shadowPs = _device.CreatePixelShader(shadowPsBytes);
            _gradientVs = _device.CreateVertexShader(gradientVsBytes);
            _gradientPs = _device.CreatePixelShader(gradientPsBytes);
            _skyVs = _device.CreateVertexShader(skyVsBytes);
            _skyPs = _device.CreatePixelShader(skyPsBytes);

            InputElementDescription[] mainElements =
            {
                new("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new("NORMAL",   0, Format.R32G32B32_Float, 12, 0),
                new("TEXCOORD", 0, Format.R32G32_Float,    24, 0),
            };

            InputElementDescription[] skyElements =
            {
                new("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            };

            _mainLayout = _device.CreateInputLayout(mainElements, mainVsBytes);
            _skyLayout = _device.CreateInputLayout(skyElements, skyVsBytes);
        }

        private void RenderFrame()
        {
            if (!_initialized ||
                _device == null ||
                _context == null ||
                _swapChain == null ||
                _rtv == null ||
                _dsv == null ||
                !Visible ||
                ClientSize.Width <= 0 ||
                ClientSize.Height <= 0)
            {
                return;
            }

            if (_pendingResize)
            {
                _pendingResize = false;
                ResizeSwapChain();
            }

            long now = _clock.ElapsedMilliseconds;
            float dt = Math.Max(0.0005f, (now - _lastTicks) / 1000f);
            _lastTicks = now;

            _animationTime += dt;

            float lerpT = 1f - MathF.Exp(-dt * 10f);
            _yaw = Lerp(_yaw, _yawTarget, lerpT);
            _pitch = Lerp(_pitch, _pitchTarget, lerpT);
            _distance = Lerp(_distance, _distanceTarget, lerpT);
            _focus = Vector3.Lerp(_focus, _focusTarget, lerpT);

            AmbienceTheme theme = AmbienceTheme.From(_backgroundMode);
            PlayerPose pose = EvaluatePlayerPose(_animationTime);

            Vector3 eye = _focus + new Vector3(
                MathF.Cos(_pitch) * MathF.Sin(_yaw),
                MathF.Sin(_pitch),
                MathF.Cos(_pitch) * MathF.Cos(_yaw)) * _distance;

            Vector3 target = _focus + new Vector3(0, 0.1f, 0);

            Matrix4x4 view = CreateLookAtLH(eye, target, Vector3.UnitY);

            float aspect = Math.Max(1, ClientSize.Width) / (float)Math.Max(1, ClientSize.Height);

            Matrix4x4 proj = CreatePerspectiveFovLH(
                BaseSceneFov,
                aspect,
                0.01f,
                200f);

            Matrix4x4 viewProj = view * proj;

            Vector3 lightDir = Vector3.Normalize(new Vector3(-0.40f, -1.0f, -0.55f));
            Vector3 lightPos = _focus - lightDir * 7.0f + new Vector3(0, 1.5f, 0);

            Matrix4x4 lightView = CreateLookAtLH(lightPos, target, Vector3.UnitY);
            Matrix4x4 lightProj = CreateOrthographicLH(10f, 10f, 0.1f, 25f);
            Matrix4x4 lightViewProj = lightView * lightProj;

            Matrix4x4 skyRotation = Matrix4x4.Identity;
            if (_skyRotate && _skyRotationSpeedDegreesPerSecond > 0.0001f)
            {
                Vector3 axis = _skyRotationAxis.LengthSquared() > 0.0001f
                    ? Vector3.Normalize(_skyRotationAxis)
                    : Vector3.UnitY;

                float angle = _animationTime * (_skyRotationSpeedDegreesPerSecond * MathF.PI / 180f);
                skyRotation = Matrix4x4.CreateFromAxisAngle(axis, angle);
            }

            FrameConstants frame = new()
            {
                ViewProj = viewProj,
                LightViewProj = lightViewProj,
                SkyRotation = skyRotation,
                LightDir = new Vector4(lightDir, 0),
                CameraPos = new Vector4(eye, 1),
                SkyTop = theme.SkyTop,
                SkyHorizon = theme.SkyHorizon,
                AmbientColor = theme.Ambient
            };

            D3D11Util.UpdateDynamicBuffer(_context, _frameCb!, frame);

            RenderShadowPass(pose);
            RenderMainPass(theme, frame, view, proj, pose);

            _swapChain.Present(0u, PresentFlags.None);
        }

        private PlayerPose EvaluatePlayerPose(float t)
        {
            if (_animationMode == PreviewAnimationMode.Fap)
                return EvaluateFapPose(t);

            const float px = 1f / 16f;
            float armW = _skinVariant == MinecraftSkinVariant.Slim ? 3f * px : 4f * px;

            float walkBlend = EvaluateWalkBlend(_animationMode, t);
            float idleBlend = 1f - walkBlend;

            float cycleSpeed = _animationMode switch
            {
                PreviewAnimationMode.Walk => 7.80f,
                PreviewAnimationMode.Auto => Lerp(1.75f, 4.75f, walkBlend),
                _ => 1.45f
            };

            float step = MathF.Sin(t * cycleSpeed);
            float breath = MathF.Sin(t * 1.55f);
            float sway = MathF.Sin(t * 1.05f);

            float bob = Lerp(0.017f * MathF.Max(0f, breath), 0.048f * MathF.Abs(step), walkBlend);
            float lateral = 0.011f * walkBlend * MathF.Sin(t * cycleSpeed * 0.55f);

            Vector3 rootOffset = new(lateral, bob, 0);

            float idleRightArm = -0.055f + 0.055f * MathF.Sin(t * 1.4f);
            float idleLeftArm = 0.055f - 0.055f * MathF.Sin(t * 1.4f);
            float idleRightLeg = 0.022f * MathF.Sin(t * 1.4f);
            float idleLeftLeg = -0.022f * MathF.Sin(t * 1.4f);

            float walkRightArm = -0.82f * step;
            float walkLeftArm = 0.82f * step;
            float walkRightLeg = 0.82f * step;
            float walkLeftLeg = -0.82f * step;

            float rightArmX = Lerp(idleRightArm, walkRightArm, walkBlend);
            float leftArmX = Lerp(idleLeftArm, walkLeftArm, walkBlend);
            float rightLegX = Lerp(idleRightLeg, walkRightLeg, walkBlend);
            float leftLegX = Lerp(idleLeftLeg, walkLeftLeg, walkBlend);

            float armTwist = 0.038f + 0.018f * idleBlend + 0.018f * walkBlend * MathF.Sin(t * cycleSpeed * 0.5f);
            float bodyRotZ = Lerp(0.020f * sway, 0.034f * MathF.Sin(t * cycleSpeed * 0.5f), walkBlend);
            float bodyRotX = 0.020f * idleBlend * MathF.Sin(t * 0.9f) + 0.040f * walkBlend * MathF.Abs(step);

            float headYaw = 0.055f * idleBlend * MathF.Sin(t * 0.45f) + 0.014f * walkBlend * MathF.Sin(t * cycleSpeed * 0.25f);
            float headPitch = 0.028f * MathF.Sin(t * 0.80f) - 0.030f * walkBlend * MathF.Abs(step);

            Vector3 bodyPos = rootOffset + new Vector3(0, 12 * px, 0);
            Vector3 headPos = rootOffset + new Vector3(0, 24 * px, 0);

            Vector3 rightShoulder = rootOffset + new Vector3(-(4 * px + armW * 0.5f), 24 * px, 0);
            Vector3 leftShoulder = rootOffset + new Vector3((4 * px + armW * 0.5f), 24 * px, 0);

            Vector3 rightHip = rootOffset + new Vector3(-2 * px, 12 * px, 0);
            Vector3 leftHip = rootOffset + new Vector3(2 * px, 12 * px, 0);

            Matrix4x4 body =
                Matrix4x4.CreateRotationZ(bodyRotZ) *
                Matrix4x4.CreateRotationX(bodyRotX) *
                Matrix4x4.CreateTranslation(bodyPos);

            Matrix4x4 head =
                Matrix4x4.CreateRotationY(headYaw) *
                Matrix4x4.CreateRotationX(headPitch) *
                Matrix4x4.CreateTranslation(headPos);

            Matrix4x4 rightArm =
                Matrix4x4.CreateRotationX(rightArmX) *
                Matrix4x4.CreateRotationZ(armTwist) *
                Matrix4x4.CreateTranslation(rightShoulder);

            Matrix4x4 leftArm =
                Matrix4x4.CreateRotationX(leftArmX) *
                Matrix4x4.CreateRotationZ(-armTwist) *
                Matrix4x4.CreateTranslation(leftShoulder);

            Matrix4x4 rightLeg =
                Matrix4x4.CreateRotationX(rightLegX) *
                Matrix4x4.CreateTranslation(rightHip);

            Matrix4x4 leftLeg =
                Matrix4x4.CreateRotationX(leftLegX) *
                Matrix4x4.CreateTranslation(leftHip);

            return new PlayerPose(head, body, rightArm, leftArm, rightLeg, leftLeg);
        }

        private PlayerPose EvaluateFapPose(float t)
        {
            const float px = 1f / 16f;
            float armW = _skinVariant == MinecraftSkinVariant.Slim ? 3f * px : 4f * px;

            float groove = MathF.Sin(t * 6.10f);
            float accent = MathF.Sin(t * 12.20f);
            float sway = MathF.Sin(t * 2.60f);

            Vector3 rootOffset = new(
                0.007f * groove,
                0.010f + 0.008f * (0.5f + 0.5f * MathF.Sin(t * 6.10f)),
                0);

            Vector3 bodyPos = rootOffset + new Vector3(0, 12 * px, 0);
            Vector3 headPos = rootOffset + new Vector3(0, 24 * px, 0);

            Vector3 rightShoulder = rootOffset + new Vector3(-(4 * px + armW * 0.5f), 24 * px, 0);
            Vector3 leftShoulder = rootOffset + new Vector3((4 * px + armW * 0.5f), 24 * px, 0);

            Vector3 rightHip = rootOffset + new Vector3(-2.05f * px, 12 * px, 0);
            Vector3 leftHip = rootOffset + new Vector3(2.05f * px, 12 * px, 0);

            float bodyRotZ = -0.018f + 0.028f * groove;
            float bodyRotX = 0.014f * MathF.Sin(t * 2.35f);

            float headYaw = 0.050f * MathF.Sin(t * 1.55f);
            float headPitch = -0.018f + 0.024f * MathF.Sin(t * 2.35f);

            float rightArmX = -0.10f + 0.09f * MathF.Sin(t * 2.20f);
            float rightArmZ = 0.11f + 0.03f * sway;

            float rightLegX = -0.020f - 0.018f * groove;
            float leftLegX = 0.020f + 0.018f * groove;

            Vector3 leftHandTarget = rootOffset + new Vector3(
                (0.15f + 1.85f * groove) * px,
                (13.00f + 0.25f * accent) * px,
                (3.05f + 0.95f * groove) * px);

            Vector3 leftArmDir = Vector3.Normalize(leftHandTarget - leftShoulder);

            Matrix4x4 body =
                Matrix4x4.CreateRotationZ(bodyRotZ) *
                Matrix4x4.CreateRotationX(bodyRotX) *
                Matrix4x4.CreateTranslation(bodyPos);

            Matrix4x4 head =
                Matrix4x4.CreateRotationY(headYaw) *
                Matrix4x4.CreateRotationX(headPitch) *
                Matrix4x4.CreateTranslation(headPos);

            Matrix4x4 rightArm =
                Matrix4x4.CreateRotationX(rightArmX) *
                Matrix4x4.CreateRotationZ(rightArmZ) *
                Matrix4x4.CreateTranslation(rightShoulder);

            Matrix4x4 leftArm = CreateAlignedLimb(leftArmDir, leftShoulder);

            Matrix4x4 rightLeg =
                Matrix4x4.CreateRotationX(rightLegX) *
                Matrix4x4.CreateTranslation(rightHip);

            Matrix4x4 leftLeg =
                Matrix4x4.CreateRotationX(leftLegX) *
                Matrix4x4.CreateTranslation(leftHip);

            return new PlayerPose(head, body, rightArm, leftArm, rightLeg, leftLeg);
        }

        private static float EvaluateWalkBlend(PreviewAnimationMode mode, float t)
        {
            return mode switch
            {
                PreviewAnimationMode.Idle => 0f,
                PreviewAnimationMode.Walk => 1f,
                PreviewAnimationMode.Fap => 0f,
                _ => SmoothStep(0.18f, 0.82f, 0.5f + 0.5f * MathF.Sin(t * 0.20f - 0.95f))
            };
        }

        private void RenderShadowPass(PlayerPose pose)
        {
            if (_context == null ||
                _shadowDsv == null ||
                _shadowVs == null ||
                _shadowPs == null)
            {
                return;
            }

            Viewport shadowVp = new(0, 0, 2048, 2048, 0, 1);
            _context.RSSetViewports(new[] { shadowVp });
            _context.RSSetState(_shadowRaster);
            _context.OMSetRenderTargets(Array.Empty<ID3D11RenderTargetView>(), _shadowDsv);
            _context.ClearDepthStencilView(_shadowDsv, DepthStencilClearFlags.Depth, 1.0f, 0);
            _context.OMSetDepthStencilState(_depthEnabled);

            _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _context.IASetInputLayout(_mainLayout);
            _context.VSSetShader(_shadowVs);
            _context.PSSetShader(_shadowPs);
            _context.VSSetConstantBuffers(0, new ID3D11Buffer?[] { _frameCb, _objectCb });
            _context.PSSetSamplers(0, new ID3D11SamplerState?[] { _pointClamp });
            _context.PSSetShaderResources(0, new ID3D11ShaderResourceView?[] { _skinSrv ?? _whiteSrv });

            DrawPlayerRig(pose);

            _context.PSSetShaderResources(0, new ID3D11ShaderResourceView?[] { null });
        }

        private void RenderMainPass(AmbienceTheme theme, FrameConstants frame, Matrix4x4 view, Matrix4x4 proj, PlayerPose pose)
        {
            if (_context == null || _rtv == null || _dsv == null)
                return;

            Viewport vp = new(0, 0, Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height), 0, 1);
            _context.RSSetViewports(new[] { vp });
            _context.RSSetState(_solidRaster);
            _context.OMSetRenderTargets(new[] { _rtv }, _dsv);
            _context.ClearRenderTargetView(_rtv, new Color4(theme.SkyHorizon.X, theme.SkyHorizon.Y, theme.SkyHorizon.Z, 1f));
            _context.ClearDepthStencilView(_dsv, DepthStencilClearFlags.Depth, 1.0f, 0);

            bool useSkybox = _backgroundMode == PreviewBackgroundMode.Panorama && _cubeSrv != null;

            if (useSkybox)
            {
                Matrix4x4 skyView = view;
                skyView.M41 = 0;
                skyView.M42 = 0;
                skyView.M43 = 0;

                float aspect = Math.Max(1, ClientSize.Width) / (float)Math.Max(1, ClientSize.Height);
                float skyFov = ComputePanoramaFov(BaseSceneFov, _distance);

                Matrix4x4 skyProj = CreatePerspectiveFovLH(
                    skyFov,
                    aspect,
                    0.01f,
                    200f);

                FrameConstants skyFrame = frame;
                skyFrame.ViewProj = skyView * skyProj;

                D3D11Util.UpdateDynamicBuffer(_context, _frameCb!, skyFrame);
                RenderSkybox();
                D3D11Util.UpdateDynamicBuffer(_context, _frameCb!, frame);
            }
            else
            {
                RenderGradient();
            }

            _context.OMSetDepthStencilState(_depthEnabled);
            _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _context.IASetInputLayout(_mainLayout);
            _context.VSSetShader(_mainVs);
            _context.PSSetShader(_mainPs);
            _context.VSSetConstantBuffers(0, new ID3D11Buffer?[] { _frameCb, _objectCb });
            _context.PSSetConstantBuffers(0, new ID3D11Buffer?[] { _frameCb, _objectCb });

            if (!useSkybox && _planeMesh != null)
            {
                _context.PSSetSamplers(0, new ID3D11SamplerState?[] { _linearWrap, _linearClamp });
                _context.PSSetShaderResources(0, new ID3D11ShaderResourceView?[] { _whiteSrv, _shadowSrv });
                DrawMesh(_planeMesh, Matrix4x4.Identity, theme.GroundTint, 1f, 1f);
            }

            _context.PSSetSamplers(0, new ID3D11SamplerState?[] { _pointClamp, _linearClamp });
            _context.PSSetShaderResources(0, new ID3D11ShaderResourceView?[] { _skinSrv ?? _whiteSrv, _shadowSrv });
            DrawPlayerRig(pose);

            _context.PSSetShaderResources(0, new ID3D11ShaderResourceView?[] { null, null });
        }

        private void DrawPlayerRig(PlayerPose pose)
        {
            DrawMesh(_headBaseMesh, pose.Head, Vector4.One, 0f, 1f);
            DrawMesh(_headLayerMesh, pose.Head, Vector4.One, 0f, 1f);

            DrawMesh(_bodyBaseMesh, pose.Body, Vector4.One, 0f, 1f);
            DrawMesh(_bodyLayerMesh, pose.Body, Vector4.One, 0f, 1f);

            DrawMesh(_rightArmBaseMesh, pose.RightArm, Vector4.One, 0f, 1f);
            DrawMesh(_rightArmLayerMesh, pose.RightArm, Vector4.One, 0f, 1f);

            DrawMesh(_leftArmBaseMesh, pose.LeftArm, Vector4.One, 0f, 1f);
            DrawMesh(_leftArmLayerMesh, pose.LeftArm, Vector4.One, 0f, 1f);

            DrawMesh(_rightLegBaseMesh, pose.RightLeg, Vector4.One, 0f, 1f);
            DrawMesh(_rightLegLayerMesh, pose.RightLeg, Vector4.One, 0f, 1f);

            DrawMesh(_leftLegBaseMesh, pose.LeftLeg, Vector4.One, 0f, 1f);
            DrawMesh(_leftLegLayerMesh, pose.LeftLeg, Vector4.One, 0f, 1f);
        }

        private void RenderGradient()
        {
            if (_context == null || _gradientVs == null || _gradientPs == null)
                return;

            _context.OMSetDepthStencilState(_depthDisabled);
            _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _context.IASetInputLayout(null);
            _context.VSSetShader(_gradientVs);
            _context.PSSetShader(_gradientPs);
            _context.VSSetConstantBuffers(0, new ID3D11Buffer?[] { _frameCb });
            _context.PSSetConstantBuffers(0, new ID3D11Buffer?[] { _frameCb });
            _context.Draw(3u, 0u);
        }

        private void RenderSkybox()
        {
            if (_context == null || _skyVs == null || _skyPs == null || _skyVb == null || _cubeSrv == null)
                return;

            _context.OMSetDepthStencilState(_depthDisabled);
            _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _context.IASetInputLayout(_skyLayout);
            BindVertexBuffer(_skyVb, (uint)Marshal.SizeOf<Vector3>());
            _context.VSSetShader(_skyVs);
            _context.PSSetShader(_skyPs);
            _context.VSSetConstantBuffers(0, new ID3D11Buffer?[] { _frameCb });
            _context.PSSetConstantBuffers(0, new ID3D11Buffer?[] { _frameCb });
            _context.PSSetSamplers(0, new ID3D11SamplerState?[] { _linearClamp });
            _context.PSSetShaderResources(0, new ID3D11ShaderResourceView?[] { _cubeSrv });
            _context.Draw((uint)_skyVertexCount, 0u);
            _context.PSSetShaderResources(0, new ID3D11ShaderResourceView?[] { null });
        }

        private void DrawMesh(
            MeshBuffer? mesh,
            Matrix4x4 world,
            Vector4 tint,
            float materialKind,
            float receiveShadow)
        {
            if (_context == null || mesh == null)
                return;

            ObjectConstants obj = new()
            {
                World = world,
                Tint = tint,
                Params = new Vector4(materialKind, receiveShadow, 0, 0)
            };

            D3D11Util.UpdateDynamicBuffer(_context, _objectCb!, obj);

            BindVertexBuffer(mesh.VertexBuffer, (uint)Marshal.SizeOf<PreviewVertex>());
            _context.IASetIndexBuffer(mesh.IndexBuffer, Format.R32_UInt, 0);
            _context.DrawIndexed((uint)mesh.IndexCount, 0u, 0);
        }

        private void ResizeSwapChain()
        {
            if (_swapChain == null || _context == null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            _context.ClearState();

            DisposeRef(ref _rtv);
            DisposeRef(ref _dsv);
            DisposeRef(ref _depthTex);

            _swapChain.ResizeBuffers(
                0,
                U(Math.Max(1, ClientSize.Width)),
                U(Math.Max(1, ClientSize.Height)),
                Format.Unknown,
                SwapChainFlags.None);

            CreateBackBuffer();
        }

        private void LoadSkinTexture(byte[]? png)
        {
            DisposeRef(ref _skinSrv);

            bool legacy = false;

            try
            {
                if (_device == null || png == null || png.Length == 0)
                {
                    _skinSrv = CreateSolidTexture(Color.LightGray);
                    legacy = false;
                }
                else
                {
                    using MemoryStream ms = new(png);
                    using Bitmap src = new(ms);
                    legacy = src.Width == 64 && src.Height == 32;

                    using Bitmap bmp = NormalizeSkinBitmap(src);
                    _skinSrv = CreateTextureFromBitmap(bmp) ?? CreateSolidTexture(Color.LightGray);
                }
            }
            catch
            {
                _skinSrv = CreateSolidTexture(Color.LightGray);
                legacy = false;
            }

            if (_skinIsLegacy != legacy)
            {
                _skinIsLegacy = legacy;

                if (_initialized)
                    RebuildPlayerMesh();
            }
        }

        private ID3D11ShaderResourceView? CreateSolidTexture(Color color)
        {
            using Bitmap bmp = new(1, 1, PixelFormat.Format32bppArgb);
            bmp.SetPixel(0, 0, color);
            return CreateTextureFromBitmap(bmp);
        }

        private ID3D11ShaderResourceView? CreateTextureFromBitmap(Bitmap bmp)
        {
            if (_device == null)
                return null;

            Rectangle rect = new(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                Texture2DDescription desc = new()
                {
                    Width = U(bmp.Width),
                    Height = U(bmp.Height),
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags = BindFlags.ShaderResource
                };

                SubresourceData sub = new(data.Scan0, (uint)data.Stride, 0u);

                using ID3D11Texture2D tex = _device.CreateTexture2D(desc, sub);
                return _device.CreateShaderResourceView(tex);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        private void LoadSkybox(string? source)
        {
            DisposeRef(ref _cubeSrv);

            _skyRotate = false;
            _skyRotationAxis = Vector3.UnitY;
            _skyRotationSpeedDegreesPerSecond = 0f;

            if (_device == null)
                return;

            string wanted = string.IsNullOrWhiteSpace(source)
                ? BuiltInPanoramaCatalog.DefaultKey
                : source!;

            Bitmap[] faces = Array.Empty<Bitmap>();

            try
            {
                if (!BuiltInPanoramaCatalog.TryLoad(wanted, out faces) &&
                    !string.Equals(wanted, BuiltInPanoramaCatalog.DefaultKey, StringComparison.OrdinalIgnoreCase))
                {
                    BuiltInPanoramaCatalog.TryLoad(BuiltInPanoramaCatalog.DefaultKey, out faces);
                }

                if (faces.Length != 6)
                    return;

                _cubeSrv = CreateSkyTextureArrayFromPanorama(faces);
            }
            finally
            {
                foreach (Bitmap face in faces)
                    face.Dispose();
            }
        }

        private ID3D11ShaderResourceView? CreateSkyTextureArrayFromPanorama(Bitmap[] logicalFaces)
        {
            if (_device == null || logicalFaces.Length != 6)
                return null;

            Bitmap[] bitmaps = new Bitmap[6];
            BitmapData?[] lockData = new BitmapData?[6];

            try
            {
                for (int i = 0; i < 6; i++)
                    bitmaps[i] = CopyToArgb32(logicalFaces[i]);

                int width = bitmaps[0].Width;
                int height = bitmaps[0].Height;

                if (width <= 0 || height <= 0 || width != height)
                    return null;

                for (int i = 1; i < 6; i++)
                {
                    if (bitmaps[i].Width != width || bitmaps[i].Height != height)
                        return null;
                }

                Rectangle rect = new(0, 0, width, height);
                SubresourceData[] subs = new SubresourceData[6];

                for (int i = 0; i < 6; i++)
                {
                    lockData[i] = bitmaps[i].LockBits(
                        rect,
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb);

                    subs[i] = new SubresourceData(
                        lockData[i]!.Scan0,
                        (uint)lockData[i]!.Stride,
                        0u);
                }

                Texture2DDescription desc = new()
                {
                    Width = U(width),
                    Height = U(height),
                    MipLevels = 1,
                    ArraySize = 6,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags = BindFlags.ShaderResource
                };

                using ID3D11Texture2D tex = _device.CreateTexture2D(desc, subs);

                ShaderResourceViewDescription srvDesc = new()
                {
                    Format = desc.Format,
                    ViewDimension = ShaderResourceViewDimension.Texture2DArray,
                    Texture2DArray = new Texture2DArrayShaderResourceView
                    {
                        MostDetailedMip = 0,
                        MipLevels = 1,
                        FirstArraySlice = 0,
                        ArraySize = 6
                    }
                };

                return _device.CreateShaderResourceView(tex, srvDesc);
            }
            catch
            {
                return null;
            }
            finally
            {
                for (int i = 0; i < 6; i++)
                {
                    if (bitmaps[i] != null)
                    {
                        if (lockData[i] != null)
                            bitmaps[i].UnlockBits(lockData[i]!);

                        bitmaps[i].Dispose();
                    }
                }
            }
        }

        private static float ComputePanoramaFov(float baseFov, float cameraDistance)
        {
            const float nearFov = 0.46f;
            const float farFov = 1.95f;

            if (cameraDistance <= DefaultDistance)
            {
                float t = Math.Clamp(
                    (cameraDistance - MinCameraDistance) / (DefaultDistance - MinCameraDistance),
                    0f,
                    1f);

                return Lerp(nearFov, baseFov, t);
            }

            float farT = Math.Clamp(
                (cameraDistance - DefaultDistance) / (MaxCameraDistance - DefaultDistance),
                0f,
                1f);

            farT = MathF.Pow(farT, 0.82f);

            return Lerp(baseFov, farFov, farT);
        }

        private static Bitmap NormalizeSkinBitmap(Bitmap src)
        {
            if (src.Width == 64 && src.Height == 64)
                return CopyToArgb32(src);

            if (src.Width == 64 && src.Height == 32)
            {
                Bitmap modern = new(64, 64, PixelFormat.Format32bppArgb);
                using Graphics g = Graphics.FromImage(modern);
                g.Clear(Color.Transparent);
                g.DrawImage(src, 0, 0, 64, 32);
                return modern;
            }

            Bitmap fallback = new(64, 64, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(fallback))
            {
                g.Clear(Color.LightGray);
            }
            return fallback;
        }

        private static Bitmap CopyToArgb32(Bitmap src)
        {
            Bitmap dst = new(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(dst);
            g.DrawImage(src, 0, 0, src.Width, src.Height);
            return dst;
        }

        private void OnMouseDownInternal(object? sender, WinForms.MouseEventArgs e)
        {
            Focus();
            Capture = true;
            _lastMouse = e.Location;

            if (e.Button == WinForms.MouseButtons.Left)
                _leftDown = true;

            if (e.Button == WinForms.MouseButtons.Right)
                _rightDown = true;
        }

        private void OnMouseUpInternal(object? sender, WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Left)
                _leftDown = false;

            if (e.Button == WinForms.MouseButtons.Right)
                _rightDown = false;

            if (!_leftDown && !_rightDown)
                Capture = false;
        }

        private void OnMouseMoveInternal(object? sender, WinForms.MouseEventArgs e)
        {
            int dx = e.X - _lastMouse.X;
            int dy = e.Y - _lastMouse.Y;
            _lastMouse = e.Location;

            const float orbitHorizontal = 0.0075f;
            const float orbitVertical = 0.0058f;
            const float panHorizontalBoost = 1.08f;

            if (_leftDown)
            {
                _yawTarget += dx * orbitHorizontal;
                _pitchTarget = Math.Clamp(_pitchTarget + dy * orbitVertical, -1.10f, 1.10f);
            }

            if (_rightDown)
            {
                Vector3 forward = Vector3.Normalize(new Vector3(
                    MathF.Cos(_pitchTarget) * MathF.Sin(_yawTarget),
                    MathF.Sin(_pitchTarget),
                    MathF.Cos(_pitchTarget) * MathF.Cos(_yawTarget)));

                Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
                Vector3 up = Vector3.UnitY;

                float panScale = _distanceTarget * 0.0032f;

                _focusTarget += (right * dx * panHorizontalBoost + up * dy) * panScale;
            }
        }

        private void OnMouseWheelInternal(object? sender, WinForms.MouseEventArgs e)
        {
            _distanceTarget = Math.Clamp(
                _distanceTarget - e.Delta * 0.0018f,
                MinCameraDistance,
                MaxCameraDistance);
        }

        private void ReleaseAll()
        {
            DisposeRef(ref _cubeSrv);
            DisposeRef(ref _skinSrv);
            DisposeRef(ref _whiteSrv);

            DisposeRef(ref _headBaseMesh);
            DisposeRef(ref _headLayerMesh);
            DisposeRef(ref _bodyBaseMesh);
            DisposeRef(ref _bodyLayerMesh);
            DisposeRef(ref _rightArmBaseMesh);
            DisposeRef(ref _rightArmLayerMesh);
            DisposeRef(ref _leftArmBaseMesh);
            DisposeRef(ref _leftArmLayerMesh);
            DisposeRef(ref _rightLegBaseMesh);
            DisposeRef(ref _rightLegLayerMesh);
            DisposeRef(ref _leftLegBaseMesh);
            DisposeRef(ref _leftLegLayerMesh);
            DisposeRef(ref _planeMesh);
            DisposeRef(ref _skyVb);

            DisposeRef(ref _shadowSrv);
            DisposeRef(ref _shadowDsv);
            DisposeRef(ref _shadowTex);

            DisposeRef(ref _frameCb);
            DisposeRef(ref _objectCb);

            DisposeRef(ref _mainLayout);
            DisposeRef(ref _skyLayout);

            DisposeRef(ref _mainVs);
            DisposeRef(ref _mainPs);
            DisposeRef(ref _shadowVs);
            DisposeRef(ref _shadowPs);
            DisposeRef(ref _gradientVs);
            DisposeRef(ref _gradientPs);
            DisposeRef(ref _skyVs);
            DisposeRef(ref _skyPs);

            DisposeRef(ref _pointClamp);
            DisposeRef(ref _linearWrap);
            DisposeRef(ref _linearClamp);
            DisposeRef(ref _solidRaster);
            DisposeRef(ref _shadowRaster);
            DisposeRef(ref _depthEnabled);
            DisposeRef(ref _depthDisabled);

            DisposeRef(ref _rtv);
            DisposeRef(ref _dsv);
            DisposeRef(ref _depthTex);

            DisposeRef(ref _swapChain);
            DisposeRef(ref _context);
            DisposeRef(ref _device);
            DisposeRef(ref _factory);
        }

        private static void DisposeRef<T>(ref T? obj) where T : class, IDisposable
        {
            obj?.Dispose();
            obj = null;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float SmoothStep(float a, float b, float x)
        {
            float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        private static Matrix4x4 CreateAlignedLimb(Vector3 direction, Vector3 translation)
        {
            if (direction.LengthSquared() < 0.000001f)
                direction = -Vector3.UnitY;

            direction = Vector3.Normalize(direction);

            Matrix4x4 align = CreateFromToRotation(-Vector3.UnitY, direction);
            return align * Matrix4x4.CreateTranslation(translation);
        }

        private static Matrix4x4 CreateFromToRotation(Vector3 from, Vector3 to)
        {
            from = Vector3.Normalize(from);
            to = Vector3.Normalize(to);

            float dot = Math.Clamp(Vector3.Dot(from, to), -1f, 1f);

            if (dot > 0.9999f)
                return Matrix4x4.Identity;

            if (dot < -0.9999f)
            {
                Vector3 axis = Vector3.Cross(from, Vector3.UnitX);

                if (axis.LengthSquared() < 0.000001f)
                    axis = Vector3.Cross(from, Vector3.UnitZ);

                axis = Vector3.Normalize(axis);
                return Matrix4x4.CreateFromAxisAngle(axis, MathF.PI);
            }

            Vector3 cross = Vector3.Cross(from, to);
            float s = MathF.Sqrt((1f + dot) * 2f);
            float invS = 1f / s;

            Quaternion q = new(
                cross.X * invS,
                cross.Y * invS,
                cross.Z * invS,
                s * 0.5f);

            q = Quaternion.Normalize(q);
            return Matrix4x4.CreateFromQuaternion(q);
        }

        private static Matrix4x4 CreateLookAtLH(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 z = Vector3.Normalize(target - eye);
            Vector3 x = Vector3.Normalize(Vector3.Cross(up, z));
            Vector3 y = Vector3.Cross(z, x);

            return new Matrix4x4(
                x.X, y.X, z.X, 0,
                x.Y, y.Y, z.Y, 0,
                x.Z, y.Z, z.Z, 0,
                -Vector3.Dot(x, eye), -Vector3.Dot(y, eye), -Vector3.Dot(z, eye), 1);
        }

        private static Matrix4x4 CreatePerspectiveFovLH(float fov, float aspect, float zNear, float zFar)
        {
            float yScale = 1f / MathF.Tan(fov * 0.5f);
            float xScale = yScale / aspect;

            return new Matrix4x4(
                xScale, 0, 0, 0,
                0, yScale, 0, 0,
                0, 0, zFar / (zFar - zNear), 1,
                0, 0, (-zNear * zFar) / (zFar - zNear), 0);
        }

        private static Matrix4x4 CreateOrthographicLH(float width, float height, float zNear, float zFar)
        {
            return new Matrix4x4(
                2f / width, 0, 0, 0,
                0, 2f / height, 0, 0,
                0, 0, 1f / (zFar - zNear), 0,
                0, 0, -zNear / (zFar - zNear), 1);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FrameConstants
        {
            public Matrix4x4 ViewProj;
            public Matrix4x4 LightViewProj;
            public Matrix4x4 SkyRotation;
            public Vector4 LightDir;
            public Vector4 CameraPos;
            public Vector4 SkyTop;
            public Vector4 SkyHorizon;
            public Vector4 AmbientColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ObjectConstants
        {
            public Matrix4x4 World;
            public Vector4 Tint;
            public Vector4 Params;
        }

        private readonly record struct PlayerPose(
            Matrix4x4 Head,
            Matrix4x4 Body,
            Matrix4x4 RightArm,
            Matrix4x4 LeftArm,
            Matrix4x4 RightLeg,
            Matrix4x4 LeftLeg);

        private readonly record struct AmbienceTheme(
            Vector4 SkyTop,
            Vector4 SkyHorizon,
            Vector4 Ambient,
            Vector4 GroundTint)
        {
            public static AmbienceTheme From(PreviewBackgroundMode mode)
            {
                return mode switch
                {
                    PreviewBackgroundMode.Moody => new(
                        new Vector4(0.15f, 0.20f, 0.31f, 1f),
                        new Vector4(0.32f, 0.36f, 0.42f, 1f),
                        new Vector4(0.14f, 0.15f, 0.17f, 1f),
                        new Vector4(0.26f, 0.27f, 0.30f, 1f)),

                    PreviewBackgroundMode.Dark => new(
                        new Vector4(0.05f, 0.06f, 0.08f, 1f),
                        new Vector4(0.10f, 0.10f, 0.12f, 1f),
                        new Vector4(0.05f, 0.05f, 0.06f, 1f),
                        new Vector4(0.08f, 0.08f, 0.09f, 1f)),

                    _ => new(
                        new Vector4(0.70f, 0.84f, 0.98f, 1f),
                        new Vector4(0.92f, 0.95f, 1.00f, 1f),
                        new Vector4(0.24f, 0.25f, 0.28f, 1f),
                        new Vector4(0.78f, 0.80f, 0.84f, 1f)),
                };
            }
        }

        private sealed class MeshBuffer : IDisposable
        {
            public ID3D11Buffer VertexBuffer { get; }
            public ID3D11Buffer IndexBuffer { get; }
            public int IndexCount { get; }

            public MeshBuffer(ID3D11Device device, PreviewMeshData mesh)
            {
                VertexBuffer = D3D11Util.CreateImmutableBuffer(device, BindFlags.VertexBuffer, mesh.Vertices.ToArray());
                IndexBuffer = D3D11Util.CreateImmutableBuffer(device, BindFlags.IndexBuffer, mesh.Indices.ToArray());
                IndexCount = mesh.Indices.Count;
            }

            public void Dispose()
            {
                VertexBuffer.Dispose();
                IndexBuffer.Dispose();
            }
        }

        private static class BuiltInPanoramaCatalog
        {
            private sealed record Preset(string Key, string DisplayName, params string[] Aliases);

            public const string DefaultKey = "old";

            private static readonly Preset[] Presets =
            {
                new("old", "Old", "legacy"),
                new("aquatic", "Aquatic"),
                new("village_and_pillage", "Village & Pillage", "village and pillage"),
                new("buzzy_bees", "Buzzy Bees", "buzzy bees"),
                new("nether", "Nether"),
                new("caves_and_cliffs_old", "Caves & Cliffs Old", "caves and cliffs old"),
                new("caves_and_cliffs_new", "Caves & Cliffs New", "caves and cliffs new"),
                new("the_wild", "The Wild", "wild"),
                new("trails_and_tales", "Trails & Tales", "trails and tales"),
                new("tricky_trials", "Tricky Trials", "tricky trials"),
                new("the_garden_awakens", "The Garden Awakens", "garden awakens"),
                new("spring_to_life", "Spring to Life", "spring to life"),
                new("chase_the_skies", "Chase the Skies", "chase the skies"),
                new("tiny_takeover", "Tiny Takeover", "tiny takeover"),
            };

            private static readonly System.Reflection.Assembly PreviewAssembly =
                typeof(PlayerPreviewRenderControl).Assembly;

            private static readonly string AssemblyShortName =
                PreviewAssembly.GetName().Name ?? "RefreshToAccess2";

            private static readonly ResourceManager WpfGeneratedResourceManager =
                new($"{AssemblyShortName}.g", PreviewAssembly);

            private static readonly string[] ManifestNames =
                PreviewAssembly.GetManifestResourceNames();

            private static readonly ResourceManager[] ResourceManagers =
            {
                new("RefreshToAccess2.Rendering.PlayerPreviewRenderControl", PreviewAssembly),
                new("RefreshToAccess2.Properties.Resources", PreviewAssembly),
            };

            private static readonly string[] SupportedImageExtensions =
            {
                ".png",
                ".jpg",
                ".jpeg"
            };

            private static readonly string[] NormalizedImageExtensionSuffixes =
            {
                "",
                "_png",
                "_jpg",
                "_jpeg"
            };

            public static bool TryLoad(string? input, out Bitmap[] faces)
            {
                faces = Array.Empty<Bitmap>();

                string? key = ResolveKey(input);
                if (string.IsNullOrWhiteSpace(key))
                    return false;

                return TryLoadFromWpfGeneratedResources(key, out faces)
                    || TryLoadFromPackUris(key, out faces)
                    || TryLoadFromManifestResources(key, out faces)
                    || TryLoadFromResx(key, out faces);
            }

            private static string? ResolveKey(string? input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return null;

                List<string> candidates = new() { input };

                string fileName = Path.GetFileNameWithoutExtension(input);
                if (!string.IsNullOrWhiteSpace(fileName))
                    candidates.Add(fileName);

                foreach (string part in input.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
                    candidates.Add(part);

                foreach (string candidate in candidates)
                {
                    string normalized = NormalizeKey(candidate);

                    foreach (Preset preset in Presets)
                    {
                        if (normalized == NormalizeKey(preset.Key) ||
                            normalized == NormalizeKey(preset.DisplayName) ||
                            preset.Aliases.Any(a => normalized == NormalizeKey(a)))
                        {
                            return preset.Key;
                        }
                    }

                    foreach (Preset preset in Presets)
                    {
                        if (normalized.IndexOf(NormalizeKey(preset.Key), StringComparison.OrdinalIgnoreCase) >= 0 ||
                            normalized.IndexOf(NormalizeKey(preset.DisplayName), StringComparison.OrdinalIgnoreCase) >= 0 ||
                            preset.Aliases.Any(a => normalized.IndexOf(NormalizeKey(a), StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            return preset.Key;
                        }
                    }
                }

                return null;
            }

            private static bool TryLoadFromWpfGeneratedResources(string key, out Bitmap[] faces)
            {
                faces = new Bitmap[6];

                try
                {
                    for (int i = 0; i < 6; i++)
                    {
                        using Stream s = OpenGeneratedResourceFaceStream(key, i);
                        using Bitmap raw = new(s);
                        faces[i] = CopyToArgb32(raw);
                    }

                    return true;
                }
                catch
                {
                    DisposeFaces(faces);
                    faces = Array.Empty<Bitmap>();
                    return false;
                }
            }

            private static Stream OpenGeneratedResourceFaceStream(string key, int faceIndex)
            {
                foreach (string ext in SupportedImageExtensions)
                {
                    string resourceKey = $"panoramas/{key}/panorama_{faceIndex}{ext}".ToLowerInvariant();

                    UnmanagedMemoryStream? stream = null;
                    try
                    {
                        stream = WpfGeneratedResourceManager.GetStream(resourceKey, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                    }

                    if (stream != null)
                        return stream;
                }

                throw new FileNotFoundException(
                    $"Could not find WPF generated resource face {faceIndex} for preset '{key}'.");
            }

            private static bool TryLoadFromPackUris(string key, out Bitmap[] faces)
            {
                faces = new Bitmap[6];

                try
                {
                    for (int i = 0; i < 6; i++)
                    {
                        using Stream s = OpenPackFaceStream(key, i);
                        using Bitmap raw = new(s);
                        faces[i] = CopyToArgb32(raw);
                    }

                    return true;
                }
                catch
                {
                    DisposeFaces(faces);
                    faces = Array.Empty<Bitmap>();
                    return false;
                }
            }

            private static Stream OpenPackFaceStream(string key, int faceIndex)
            {
                foreach (string ext in SupportedImageExtensions)
                {
                    string relativeUri = $"/panoramas/{key}/panorama_{faceIndex}{ext}";

                    try
                    {
                        StreamResourceInfo? relInfo = Application.GetResourceStream(new Uri(relativeUri, UriKind.Relative));
                        if (relInfo?.Stream != null)
                            return relInfo.Stream;
                    }
                    catch
                    {
                    }

                    string absoluteComponentUri =
                        $"pack://application:,,,/{AssemblyShortName};component/panoramas/{key}/panorama_{faceIndex}{ext}";

                    try
                    {
                        StreamResourceInfo? absInfo = Application.GetResourceStream(new Uri(absoluteComponentUri, UriKind.Absolute));
                        if (absInfo?.Stream != null)
                            return absInfo.Stream;
                    }
                    catch
                    {
                    }
                }

                throw new FileNotFoundException(
                    $"Could not find panorama face {faceIndex} for preset '{key}'.");
            }

            private static bool TryLoadFromManifestResources(string key, out Bitmap[] faces)
            {
                faces = new Bitmap[6];

                try
                {
                    for (int i = 0; i < 6; i++)
                    {
                        string? resourceName = ManifestNames.FirstOrDefault(name =>
                            MatchesNormalizedResourceName(NormalizeKey(name), key, i));

                        if (resourceName == null)
                            throw new MissingManifestResourceException($"{key}/panorama_{i}");

                        using Stream s = PreviewAssembly.GetManifestResourceStream(resourceName)
                            ?? throw new MissingManifestResourceException(resourceName);

                        using Bitmap raw = new(s);
                        faces[i] = CopyToArgb32(raw);
                    }

                    return true;
                }
                catch
                {
                    DisposeFaces(faces);
                    faces = Array.Empty<Bitmap>();
                    return false;
                }
            }

            private static bool TryLoadFromResx(string key, out Bitmap[] faces)
            {
                faces = Array.Empty<Bitmap>();

                foreach (ResourceManager manager in ResourceManagers)
                {
                    if (TryLoadFromResxManager(manager, key, out faces))
                        return true;
                }

                return false;
            }

            private static bool TryLoadFromResxManager(ResourceManager manager, string key, out Bitmap[] faces)
            {
                faces = new Bitmap[6];

                try
                {
                    ResourceSet? set = manager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
                    if (set == null)
                        throw new MissingManifestResourceException();

                    Dictionary<string, object> resources = new(StringComparer.OrdinalIgnoreCase);

                    foreach (DictionaryEntry entry in set)
                    {
                        if (entry.Key is string name && entry.Value != null)
                            resources[name] = entry.Value;
                    }

                    for (int i = 0; i < 6; i++)
                    {
                        object? obj = resources
                            .Where(kvp => MatchesNormalizedResourceName(NormalizeKey(kvp.Key), key, i))
                            .Select(kvp => kvp.Value)
                            .FirstOrDefault();

                        if (obj == null)
                            throw new MissingManifestResourceException($"{key}/panorama_{i}");

                        faces[i] = CreateBitmapFromResourceObject(obj);
                    }

                    return true;
                }
                catch
                {
                    DisposeFaces(faces);
                    faces = Array.Empty<Bitmap>();
                    return false;
                }
            }

            private static bool MatchesNormalizedResourceName(string normalizedName, string key, int faceIndex)
            {
                string token = $"{NormalizeKey(key)}_panorama_{faceIndex}";

                foreach (string suffix in NormalizedImageExtensionSuffixes)
                {
                    if (normalizedName == token + suffix ||
                        normalizedName.EndsWith(token + suffix, StringComparison.OrdinalIgnoreCase) ||
                        normalizedName.EndsWith("_" + token + suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return normalizedName.IndexOf("_" + token + "_", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static Bitmap CreateBitmapFromResourceObject(object obj)
            {
                return obj switch
                {
                    Bitmap bmp => CopyToArgb32(bmp),
                    byte[] bytes => LoadBitmapFromBytes(bytes),
                    UnmanagedMemoryStream ums => LoadBitmapFromStream(ums),
                    Stream stream => LoadBitmapFromStream(stream),
                    _ => throw new InvalidOperationException($"Unsupported panorama resource type: {obj.GetType().FullName}")
                };
            }

            private static Bitmap LoadBitmapFromBytes(byte[] bytes)
            {
                using MemoryStream ms = new(bytes);
                using Bitmap raw = new(ms);
                return CopyToArgb32(raw);
            }

            private static Bitmap LoadBitmapFromStream(Stream stream)
            {
                if (stream.CanSeek)
                    stream.Position = 0;

                using Bitmap raw = new(stream);
                return CopyToArgb32(raw);
            }

            private static void DisposeFaces(Bitmap[] faces)
            {
                foreach (Bitmap? face in faces)
                    face?.Dispose();
            }

            private static string NormalizeKey(string value)
            {
                value = value.Trim().ToLowerInvariant().Replace("&", " and ");

                StringBuilder sb = new(value.Length);
                bool pendingSeparator = false;

                foreach (char c in value)
                {
                    if (char.IsLetterOrDigit(c))
                    {
                        if (pendingSeparator && sb.Length > 0 && sb[sb.Length - 1] != '_')
                            sb.Append('_');

                        sb.Append(c);
                        pendingSeparator = false;
                    }
                    else
                    {
                        pendingSeparator = true;
                    }
                }

                return sb.ToString().Trim('_');
            }
        }
    }
}
