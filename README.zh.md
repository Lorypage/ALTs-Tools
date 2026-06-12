<div align="center">

<img src="img/sth/banner.png" alt="ALTs Tools" width="100%"/>

<br/>

# 🐖 ALTs Tools

#### 更优雅的 Minecraft 多账号（ALT）管理工具

<p>一键完成 <b>令牌转换</b> · <b>账号管理</b> · <b>注入</b> · <b>玩家档案（皮肤 &amp; 改名）</b>

<br/>

<a href="LICENSE"><img src="https://img.shields.io/badge/license-GPLv3-green.svg?style=flat-square" alt="License"/></a>
<img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6.svg?style=flat-square&logo=windows&logoColor=white" alt="platform"/>
<img src="https://img.shields.io/badge/.NET-8.0--windows-512BD4.svg?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8"/>
<img src="https://img.shields.io/badge/UI-WPF%20·%20MaterialDesign-2D7D9A.svg?style=flat-square" alt="UI"/>
<img src="https://img.shields.io/badge/version-1.0.1-blue.svg?style=flat-square" alt="version"/>

<br/>
<br/>

<a href="#-功能一览">功能</a> ·
<a href="#️-界面预览">预览</a> ·
<a href="#-构建与运行">构建</a> ·
<a href="#-快速上手">上手</a> ·
<a href="#-技术栈与依赖">依赖</a>

<sub><a href="README.md">English</a> · <b>简体中文</b></sub>

<sub>Original by <a href="https://github.com/NoobCock/RefreshToAccess2">NoobCock</a></sub>

</div>

---

## ✨ 这是什么

**ALTs Tools** 是一款为 Minecraft 玩家打造的多账号（ALT）一体化管理工具。

把一串 Refresh Token（或一段微软登录 Cookie）丢进来，它会帮你换成可用的 Access Token、登录账号、把档案整理成漂亮的卡片，还能直接给正在运行的游戏「换上」这个账号，并顺手管理玩家档案——皮肤和游戏内昵称。

整个界面基于 **WPF + .NET 8**，采用 **MaterialDesign** 主题，配上会缓缓流动的 **Minecraft 全景背景**、活泼的交互动画，以及可运行时切换的 **中英双语** 本地化 —— 既好看，又顺手。

> [!WARNING]
> 仅供学习交流与个人账号管理使用。请勿用于任何违反 Minecraft / Mojang / Microsoft 服务条款的行为，使用风险自负。

---

## 🚀 功能一览

<table>
<tr>
<td width="50%" valign="top">

### 🔑 Converter · 令牌转换
把 **Refresh Token** 一键换成 **Access Token** 并完成登录。

- 两种可切换模式：**刷新令牌 → 访问令牌** 与 **Cookie → 令牌**
  （转换微软登录 Cookie，不登录、不入库）
- 内置多种启动器客户端身份：
  `Vanilla` · `HMCL` · `PCL` · `Essential` ·
  `ksyz Alt Manager` · `BakaXL` · `LabyMod` 等
- 支持 **自定义 Client ID**
- 内置 **令牌时效检查**（解析 JWT 或 Cookie 过期时间）
- 可选 **自动复制** 转换后的令牌
- 实时显示玩家名与 UUID

</td>
<td width="50%" valign="top">

### 🗂️ Alt Manager · 账号管理
把所有 ALT 账号集中成可视化卡片墙。

- **卡片 / 列表** 两种视图自由切换
- **搜索 + 排序**（按时间、按名称）
- 玩家头像缓存，加载更快
- 批量 **多选** 操作
- 点击查看账号详情面板

</td>
</tr>
<tr>
<td width="50%" valign="top">

### ⚙️ Settings · 设置
让这个工具更合你心意。

- 在 **English / 简体中文** 之间切换界面语言——即时生效，无需重启
- 偏好设置跨启动记忆

</td>
<td width="50%" valign="top">

### 💉 Injector · 令牌注入
把账号「注入」到正在运行的 Minecraft。

- 自动识别运行中的游戏进程
- 免重启切换登录账号
- 通过本地通道安全下发令牌

</td>
</tr>
<tr>
<td colspan="2" valign="top">

### 🎨 Player Profile · 玩家档案
管理账号的昵称与皮肤，并预览其他玩家，所见即所得。

- **3D 皮肤实时预览**（基于 Direct3D 渲染） · 支持 `Classic` / `Slim` 两种模型
- 可按 **玩家名** 查询并套用他人皮肤 · 多套 Minecraft 版本全景背景任意切换
- 直接在此 **修改游戏内昵称（IGN）**，调用官方 Minecraft 接口（无需单独页面）

</td>
</tr>
</table>

---

## 🖼️ 界面预览

<div align="center">

<table>
<tr>
<td align="center" width="50%">
<img src="img/screenshots/Token%20Converter.png" alt="Token Converter" width="100%"/>
<br/><b>🔑 Token Converter · 令牌转换</b>
<br/><sub>双栏粘贴令牌，顶部切换客户端，可开启自动复制</sub>
</td>
<td align="center" width="50%">
<img src="img/screenshots/Alt%20manager3.png" alt="Alt Manager 详情" width="100%"/>
<br/><b>🗂️ Alt Manager · 账号详情</b>
<br/><sub>查看 UUID 与令牌，支持刷新令牌 / 一键复制</sub>
</td>
</tr>
<tr>
<td align="center" width="50%">
<img src="img/screenshots/Alt%20manager1.png" alt="Alt Manager 卡片视图" width="100%"/>
<br/><b>🗂️ Alt Manager · 卡片视图</b>
<br/><sub>账户一目了然，支持搜索与导入</sub>
</td>
<td align="center" width="50%">
<img src="img/screenshots/Alt%20manager2.png" alt="Alt Manager 列表视图" width="100%"/>
<br/><b>🗂️ Alt Manager · 列表视图</b>
<br/><sub>一键切换风格</sub>
</td>
</tr>
<tr>
<td align="center" width="50%">
<img src="img/screenshots/Player%20Profile.png" alt="Player Profile" width=100%"/>
<br/><b>🎨 Player Profile · 玩家档案</b>
<br/><sub>Minecraft 3D 实时皮肤预览，可按玩家名查询套用，并支持一键改名</sub>
</td>
<td align="center" width="50%">
<img src="img/screenshots/Token%20injector.png" alt="Token Injector" width="100%"/>
<br/><b>💉 Injector · 令牌注入</b>
<br/><sub>选择运行中的游戏进程注入账号</sub>
</td>
</tr>
</table>
<br/>
</div>

---

## 📦 环境要求

| 用途 | 需求 |
| --- | --- |
| 🟢 运行已编译版本 | Windows 10 / 11 + [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| 🔨 从源码编译 | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或 Visual Studio 2022+ |

---

## 🔧 构建与运行

```powershell
# 1. 克隆仓库
git clone https://github.com/NoobCock/RefreshToAccess2.git
cd RefreshToAccess2

# 2. 还原依赖并编译
dotnet restore
dotnet build -c Release

# 3. 运行
dotnet run --project RefreshToAccess2
```

编译产物为 **`TokenTools.exe`**，位于：

```
RefreshToAccess2/bin/Release/net8.0-windows/
```

> [!TIP]
> 也可以直接用 **Visual Studio 2022**（或更新版本）打开 `RefreshToAccess2.slnx` 一键编译运行。

---

## 🧭 快速上手

1. 打开 **Converter**，粘贴 Refresh Token（或切换到 *Cookie → 令牌* 模式），选择对应客户端，点击转换完成登录；也可在此检查令牌时效。
2. 在 **Alt Manager** 中查看、搜索、整理你的所有账号。
3. 启动 Minecraft 后，在 **Injector** 中把账号注入游戏进程，免重启切换。
4. 在 **Player Profile** 中预览并更换皮肤、查询其他玩家、修改游戏内昵称（改名需已在 Converter 登录）。
5. 打开 **Settings** 在中文与 English 之间切换界面语言。

---

## 🧱 技术栈与依赖

| 依赖 | 用途 |
| --- | --- |
| [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) | Material 风格 UI 主题 |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | JSON 序列化 / 反序列化 |
| [System.IdentityModel.Tokens.Jwt](https://learn.microsoft.com/dotnet/api/system.identitymodel.tokens.jwt) | JWT 令牌解析 |
| [Vortice.Direct3D11 / D3DCompiler](https://github.com/amerkoleci/Vortice.Windows) | 3D 皮肤预览渲染 |
| [EasyCompressor.LZMA](https://github.com/mjebrahimi/EasyCompressor) | 内嵌资源压缩 |
| System.Management | 进程信息查询（注入用） |

---

## 📄 许可证

本项目基于 [GNU General Public License v3.0](LICENSE.txt) 开源发布。

## 🙌 致谢

原始项目由 [**NoobCock**](https://github.com/NoobCock/RefreshToAccess2) 创建，本仓库在其基础上修改与完善。

<div align="center">
<sub>如果这个项目对你有帮助，欢迎点一个 ⭐ Star</sub>
</div>
