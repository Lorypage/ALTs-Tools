using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddCommonEn(Dictionary<string, string> d)
        {
            // Window
            d["Window.Title"] = "ALTs Tools";
            d["Window.Minimize"] = "Minimize";
            d["Window.Maximize"] = "Maximize";
            d["Window.Close"] = "Close";

            // Navigation
            d["Nav.Converter"] = "Converter";
            d["Nav.Converter.Tip"] = "Open Token Converter";
            d["Nav.AltManager"] = "AltManager";
            d["Nav.AltManager.Tip"] = "Open Alt Manager";
            d["Nav.Injector"] = "Injector";
            d["Nav.Injector.Tip"] = "Open Token Injector";
            d["Nav.SkinChanger"] = "Player Profile";
            d["Nav.SkinChanger.Tip"] = "Open Player Profile";
            d["Nav.Settings"] = "Settings";
            d["Nav.Settings.Tip"] = "Open Settings";
            d["Nav.ToggleLabels"] = "Toggle navigation labels";

            // Common buttons / words
            d["Common.Confirm"] = "Confirm";
            d["Common.Cancel"] = "Cancel";
            d["Common.Refresh"] = "Refresh";
            d["Common.Close"] = "Close";
            d["Common.Browse"] = "Browse";
            d["Common.Error"] = "Error";
            d["Common.Success"] = "Success";
            d["Common.Warning"] = "Warning";
            d["Common.Ready"] = "Ready.";
            d["Common.OK"] = "OK";
            d["Common.Yes"] = "Yes";
            d["Common.No"] = "No";
            d["Common.Information"] = "Information";
            d["Common.Confirm.Title"] = "Confirm";

            // Settings page
            d["Settings.Title"] = "Settings";
            d["Settings.Subtitle"] = "Configure application preferences.";
            d["Settings.Language"] = "Language";
            d["Settings.LanguageHint"] = "Display language";
            d["Settings.LanguageDesc"] = "Changes apply immediately across the app.";
            d["Settings.Appearance"] = "Appearance";
            d["Settings.AppearanceDesc"] = "Pick a theme color and light or dark mode — applies instantly.";
            d["Settings.DarkMode"] = "Dark mode";
            d["Settings.AccentColor"] = "Theme color";
            d["Settings.Dynamic"] = "Dynamic (from wallpaper)";
            d["Settings.DynamicTip"] = "Generate the theme color from your desktop wallpaper";
            d["Settings.Updates"] = "Updates";
            d["Settings.UpdatesDesc"] = "Keep ALTs Tools up to date with the latest release.";
            d["Settings.AutoCheckUpdates"] = "Check for updates on startup";
            d["Settings.CheckNow"] = "Check for updates now";

            // Token Injector view
            d["Injector.Title"] = "Token Injector";
            d["Injector.Desc"] = "Select a running Minecraft process and inject the current access token into it. Make sure you have converted a valid token before injecting.";
            d["Injector.OpenSelector"] = "Open Process Selector";

            // Minecraft process selector
            d["ProcSel.Title"] = "Select Minecraft Process";
            d["ProcSel.Hint"] = "Running Minecraft instances";

            // Injection token selector
            d["InjTok.Title"] = "Select Token for Injection";
            d["InjTok.StoredAccount"] = "Stored Account";
            d["InjTok.OnlyNonExpired"] = "Only accounts with a non-expired access token are listed.";
            d["InjTok.SelectAccount"] = "Select account";
            d["InjTok.CustomToken"] = "Custom Token";
            d["InjTok.PasteHere"] = "Paste access token here";
            d["InjTok.InjectToken"] = "Inject Token";

            // Custom client ID dialog
            d["CustomClient.Title"] = "Custom Client ID";
            d["CustomClient.ClientId"] = "Client ID";
            d["CustomClient.Scope"] = "Scope";
        }
    }
}
