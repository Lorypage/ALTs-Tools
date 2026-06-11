<div align="center">

<img src="img/sth/banner.png" alt="ALTs Tools" width="100%"/>

<br/>

# 🐖 ALTs Tools

#### A nicer multi-account (ALT) manager for Minecraft players

<p>All in one place — <b>Token Conversion</b> · <b>Account Management</b> · <b>Rename</b> · <b>Injection</b> · <b>Skin Changing</b></p>

<br/>

<a href="LICENSE"><img src="https://img.shields.io/badge/license-GPLv3-green.svg?style=flat-square" alt="License"/></a>
<img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6.svg?style=flat-square&logo=windows&logoColor=white" alt="platform"/>
<img src="https://img.shields.io/badge/.NET-8.0--windows-512BD4.svg?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8"/>
<img src="https://img.shields.io/badge/UI-WPF%20·%20MaterialDesign-2D7D9A.svg?style=flat-square" alt="UI"/>
<img src="https://img.shields.io/badge/version-2.3-blue.svg?style=flat-square" alt="version"/>

<br/>
<br/>

<a href="#-features">Features</a> ·
<a href="#️-screenshots">Screenshots</a> ·
<a href="#-build--run">Build</a> ·
<a href="#-getting-started">Getting Started</a> ·
<a href="#-tech-stack--dependencies">Dependencies</a>

<sub><b>English</b> · <a href="README.zh.md">简体中文</a></sub>
<br/>
<sub>Original by <a href="https://github.com/NoobCock/RefreshToAccess2">NoobCock</a></sub>

</div>

---

## ✨ What is this

**ALTs Tools** (named *Token Tools For Minecraft* in-app) is an all-in-one multi-account (ALT) manager built for Minecraft players.

Drop in a Refresh Token and it converts it into a usable Access Token, logs the account in, organizes profiles into neat cards, can "swap" that account into a running game, and even changes the skin while it's at it.

The entire UI is built on **WPF + .NET 8** with a **MaterialDesign** theme, a gently drifting **Minecraft panorama** background, and smooth navigation animations — good looking and easy to use.

> [!WARNING]
> For learning and personal account management only. Do not use it for anything that violates the Minecraft / Mojang / Microsoft Terms of Service. Use at your own risk.

---

## 🚀 Features

<table>
<tr>
<td width="50%" valign="top">

### 🔑 Converter
Turn a **Refresh Token** into an **Access Token** and log in with one click.

- Built-in launcher client identities:
  `Vanilla` · `HMCL` · `PCL` · `Essential` ·
  `ksyz Alt Manager` · `BakaXL` · `LabyMod`, and more
- Supports a **custom Client ID**
- Optional **auto-copy** of the converted token
- Live display of player name & UUID

</td>
<td width="50%" valign="top">

### 🗂️ Alt Manager
Bring all your ALT accounts together on a visual card wall.

- Switch freely between **card / list** views
- **Search + sort** (by date, by name)
- Cached player avatars for faster loading
- Batch **multi-select** actions
- Click to open the account detail panel

</td>
</tr>
<tr>
<td width="50%" valign="top">

### ✏️ Renamer
Change your in-game name (IGN) directly.

- Calls the official Minecraft rename API
- Requires logging in via Converter first
- Instant feedback on the result

</td>
<td width="50%" valign="top">

### 💉 Injector
"Inject" an account into a running Minecraft.

- Auto-detects the running game process
- Switch the logged-in account without restarting
- Delivers the token over a local channel

</td>
</tr>
<tr>
<td colspan="2" valign="top">

### 🎨 Skin Changer
Preview and change skins online — what you see is what you get.

- **Real-time 3D skin preview** (powered by Direct3D) · supports both `Classic` / `Slim` models
- Look up and apply skins by **player name** · switch between multiple Minecraft panorama backgrounds

</td>
</tr>
</table>

---

## 🖼️ Screenshots

<div align="center">

<table>
<tr>
<td align="center" width="50%">
<img src="img/screenshots/Token%20Converter.png" alt="Token Converter" width="100%"/>
<br/><b>🔑 Token Converter</b>
<br/><sub>Paste tokens in two columns, switch clients at the top, toggle auto-copy</sub>
</td>
<td align="center" width="50%">
<img src="img/screenshots/Alt%20manager3.png" alt="Alt Manager Detail" width="100%"/>
<br/><b>🗂️ Alt Manager · Account Detail</b>
<br/><sub>View UUID & tokens, refresh token / copy all</sub>
</td>
</tr>
<tr>
<td align="center" width="50%">
<img src="img/screenshots/Alt%20manager1.png" alt="Alt Manager Card View" width="100%"/>
<br/><b>🗂️ Alt Manager · Card View</b>
<br/><sub>Avatar wall at a glance, with search & import</sub>
</td>
<td align="center" width="50%">
<img src="img/screenshots/Alt%20manager2.png" alt="Alt Manager List View" width="100%"/>
<br/><b>🗂️ Alt Manager · List View</b>
<br/><sub>One-click switch, compact view for more accounts</sub>
</td>
</tr>
<tr>
<td align="center" width="50%">
<img src="img/screenshots/Rename.png" alt="Renamer" width="100%"/>
<br/><b>✏️ Renamer</b>
<br/><sub>Type a new name and rename in one click</sub>
</td>
<td align="center" width="50%">
<img src="img/screenshots/Token%20injector.png" alt="Token Injector" width="100%"/>
<br/><b>💉 Injector</b>
<br/><sub>Pick a running game process to inject the account</sub>
</td>
</tr>
</table>

<br/>

<img src="img/screenshots/skin%20changer.png" alt="Skin Changer" width="80%"/>
<br/><b>🎨 Skin Changer</b>
<br/><sub>Real-time 3D skin preview against a Minecraft panorama, with lookup by player name</sub>

</div>

---

## 📦 Requirements

| Purpose | Requirement |
| --- | --- |
| 🟢 Run the built release | Windows 10 / 11 + [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| 🔨 Build from source | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or Visual Studio 2022+ |

---

## 🔧 Build & Run

```powershell
# 1. Clone the repo
git clone https://github.com/NoobCock/RefreshToAccess2.git
cd RefreshToAccess2

# 2. Restore dependencies and build
dotnet restore
dotnet build -c Release

# 3. Run
dotnet run --project RefreshToAccess2
```

The build output is **`TokenTools.exe`**, located at:

```
RefreshToAccess2/bin/Release/net8.0-windows/
```

> [!TIP]
> You can also open `RefreshToAccess2.slnx` in **Visual Studio 2022** (or newer) and build/run with one click.

---

## 🧭 Getting Started

1. Open **Converter**, paste a Refresh Token, pick the matching client, and click convert to log in.
2. View, search, and organize all your accounts in **Alt Manager**.
3. To rename, go to **Renamer** (you must be logged in via Converter first).
4. After launching Minecraft, use **Injector** to inject the account into the game process and switch without restarting.
5. Preview and change skins in **Skin Changer**.

---

## 🧱 Tech Stack & Dependencies

| Dependency | Purpose |
| --- | --- |
| [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) | Material-style UI theme |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | JSON serialization / deserialization |
| [System.IdentityModel.Tokens.Jwt](https://learn.microsoft.com/dotnet/api/system.identitymodel.tokens.jwt) | JWT token parsing |
| [Vortice.Direct3D11 / D3DCompiler](https://github.com/amerkoleci/Vortice.Windows) | 3D skin preview rendering |
| [EasyCompressor.LZMA](https://github.com/mjebrahimi/EasyCompressor) | Embedded resource compression |
| System.Management | Process info queries (for injection) |

---

## 📄 License

This project is released under the [GNU General Public License v3.0](LICENSE.txt).

## 🙌 Credits

Original project created by [**NoobCock**](https://github.com/NoobCock/RefreshToAccess2); this repository builds on and improves it.

<div align="center">
<sub>If this project helps you, consider leaving a ⭐ Star</sub>
</div>
