using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddCommonZh(Dictionary<string, string> d)
        {
            // Window
            d["Window.Title"] = "ALTs Tools 2.3";

            // Navigation
            d["Nav.Converter"] = "转换器";
            d["Nav.Converter.Tip"] = "打开令牌转换器";
            d["Nav.AltManager"] = "账号管理";
            d["Nav.AltManager.Tip"] = "打开账号管理器";
            d["Nav.Injector"] = "注入器";
            d["Nav.Injector.Tip"] = "打开令牌注入器";
            d["Nav.SkinChanger"] = "玩家档案";
            d["Nav.SkinChanger.Tip"] = "打开玩家档案";
            d["Nav.Settings"] = "设置";
            d["Nav.Settings.Tip"] = "打开设置";
            d["Nav.ToggleLabels"] = "切换导航标签";

            // Common buttons / words
            d["Common.Confirm"] = "确认";
            d["Common.Cancel"] = "取消";
            d["Common.Refresh"] = "刷新";
            d["Common.Close"] = "关闭";
            d["Common.Browse"] = "浏览";
            d["Common.Error"] = "错误";
            d["Common.Success"] = "成功";
            d["Common.Warning"] = "警告";
            d["Common.Ready"] = "就绪。";

            // Settings page
            d["Settings.Title"] = "设置";
            d["Settings.Subtitle"] = "配置应用程序首选项。";
            d["Settings.Language"] = "语言";
            d["Settings.LanguageHint"] = "显示语言";
            d["Settings.LanguageDesc"] = "更改后立即在整个应用中生效。";
            d["Settings.Appearance"] = "外观";
            d["Settings.AppearanceDesc"] = "选择主题色和浅色 / 深色模式——即时生效。";
            d["Settings.DarkMode"] = "深色模式";
            d["Settings.AccentColor"] = "主题色";
            d["Settings.Dynamic"] = "动态取色（来自壁纸）";
            d["Settings.DynamicTip"] = "从桌面壁纸自动生成主题色";

            // Token Injector view
            d["Injector.Title"] = "令牌注入器";
            d["Injector.Desc"] = "选择一个正在运行的 Minecraft 进程，并将当前访问令牌注入其中。注入前请确保已转换出有效的令牌。";
            d["Injector.OpenSelector"] = "打开进程选择器";

            // Minecraft process selector
            d["ProcSel.Title"] = "选择 Minecraft 进程";
            d["ProcSel.Hint"] = "正在运行的 Minecraft 实例";

            // Injection token selector
            d["InjTok.Title"] = "选择要注入的令牌";
            d["InjTok.StoredAccount"] = "已存账号";
            d["InjTok.OnlyNonExpired"] = "仅列出访问令牌未过期的账号。";
            d["InjTok.SelectAccount"] = "选择账号";
            d["InjTok.CustomToken"] = "自定义令牌";
            d["InjTok.PasteHere"] = "在此粘贴访问令牌";
            d["InjTok.InjectToken"] = "注入令牌";

            // Custom client ID dialog
            d["CustomClient.Title"] = "自定义 Client ID";
            d["CustomClient.ClientId"] = "Client ID";
            d["CustomClient.Scope"] = "Scope";
        }
    }
}
