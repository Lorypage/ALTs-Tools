using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddAltMgrEn(Dictionary<string, string> d)
        {
            // ── XAML ──
            d["AltMgr.SearchHint"] = "Search accounts…";
            // {0}=count
            d["AltMgr.AccountCount"] = "{0} account(s)";
            d["AltMgr.ImportTip"] = "Import profiles (.tapf)";
            d["AltMgr.Import"] = "Import";
            d["AltMgr.ToggleViewTip"] = "Toggle card / list";
            d["AltMgr.ToggleSelectTip"] = "Toggle selection mode";
            d["AltMgr.SettingsTip"] = "Settings";
            d["AltMgr.NoAccounts"] = "No accounts found";
            d["AltMgr.NoAccountsHint"] = "Import profiles or convert tokens to get started";
            d["AltMgr.SelectAll"] = "Select All";
            d["AltMgr.Deselect"] = "Deselect";
            d["AltMgr.ExportSelected"] = "Export Selected";
            d["AltMgr.ExportAll"] = "Export All";
            d["AltMgr.DeleteAll"] = "Delete All";
            d["AltMgr.SortBy"] = "SORT BY";
            d["AltMgr.SortOrderHint"] = "Sort order";
            d["AltMgr.SortDateNewest"] = "Date (Newest first)";
            d["AltMgr.SortDateOldest"] = "Date (Oldest first)";
            d["AltMgr.SortNameAZ"] = "Name (A → Z)";
            d["AltMgr.SortNameZA"] = "Name (Z → A)";
            d["AltMgr.CardSize"] = "CARD SIZE";
            d["AltMgr.RefreshAllHeads"] = "Refresh All Head Skins";
            d["AltMgr.CloseEsc"] = "Close (Esc)";
            d["AltMgr.LoggedIn"] = "Logged in:";
            d["AltMgr.Client"] = "Client:";
            d["AltMgr.Uuid"] = "UUID";
            d["AltMgr.CopyUuidTip"] = "Copy UUID";
            d["AltMgr.RefreshToken"] = "Refresh Token";
            d["AltMgr.CopyRefreshTip"] = "Copy Refresh Token";
            d["AltMgr.AccessToken"] = "Access Token";
            d["AltMgr.CopyAccessTip"] = "Copy Access Token";
            d["AltMgr.CopyAll"] = "Copy All";
            d["AltMgr.RefreshTokenTip"] = "Refresh token";
            d["AltMgr.CopyAllTip"] = "Copy all";

            // ── Dynamic / code-behind ──
            d["AltMgr.Delete"] = "Delete";
            // {0}=count
            d["AltMgr.DeleteCount"] = "Delete ({0})";
            d["AltMgr.FieldEmpty"] = "Field is empty";
            d["AltMgr.NoRefreshToken"] = "No refresh token";
            d["AltMgr.RefreshingHeads"] = "Refreshing all head skins…";
            // {0}=count
            d["AltMgr.RefreshedHeads"] = "✓ Refreshed {0} head(s)";
            d["AltMgr.NothingSelected"] = "Nothing selected";
            d["AltMgr.NoAccountsToExport"] = "No accounts to export";
            d["AltMgr.ExportTitle"] = "Export alt profiles";
            // {0}=count
            d["AltMgr.Exported"] = "✓ Exported {0} profile(s)";
            // {0}=error
            d["AltMgr.ExportFailed"] = "Export failed:\n{0}";
            d["AltMgr.ImportTitle"] = "Import alt profiles";
            d["AltMgr.NoValidProfiles"] = "File had no valid profiles";
            // {0}=count
            d["AltMgr.ImportMode"] = "Found {0} profile(s).\n\nYES → Merge\nNO → Replace";
            d["AltMgr.ImportModeTitle"] = "Import mode";
            // {0}=count
            d["AltMgr.Imported"] = "✓ Imported {0} profile(s)";
            // {0}=error
            d["AltMgr.ImportFailed"] = "Import failed:\n{0}";
            // {0}=label
            d["AltMgr.Copied"] = "✓ Copied {0}";
            d["AltMgr.ClipboardError"] = "Clipboard error";
            // {0}=count
            d["AltMgr.ConfirmDeleteSelected"] = "Permanently delete {0} selected account(s)?";
            d["AltMgr.ConfirmDeleteAll"] = "Permanently delete ALL stored accounts?";
            // copy-all label body: {0}=IGN {1}=UUID {2}=ClientId {3}=RefToken {4}=AccToken {5}=LoginDate
            d["AltMgr.CopyAllBody"] = "IGN: {0}\nUUID: {1}\nClient ID: {2}\nRefresh Token: {3}\nAccess Token: {4}\nLogin Date: {5}";
        }
    }
}
