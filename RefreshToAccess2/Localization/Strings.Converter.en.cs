using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddConverterEn(Dictionary<string, string> d)
        {
            // ── XAML ──
            d["Converter.Title"] = "Token Converter";
            d["Converter.ClientIdHint"] = "ClientID";
            d["Converter.RefreshSection"] = "REFRESH TOKEN";
            d["Converter.AccessSection"] = "ACCESS TOKEN";
            d["Converter.RefreshHint"] = "Refresh Token";
            d["Converter.AccessHint"] = "Access Token";
            d["Converter.Chars"] = "chars";
            d["Converter.Player"] = "PLAYER";
            d["Converter.Uuid"] = "UUID";
            d["Converter.PasteTip"] = "Paste from clipboard";
            d["Converter.ClearTip"] = "Clear";
            d["Converter.CopyTip"] = "Copy to clipboard";
            d["Converter.CopyNameTip"] = "Copy player name";
            d["Converter.CopyUuidTip"] = "Copy UUID";
            d["Converter.AutoCopy"] = "Auto-copy";
            d["Converter.AutoCopyTip"] = "Automatically copy the access token after conversion";
            d["Converter.ClientCustom"] = "Custom…";

            // ── ViewModel dynamic text ──
            d["Converter.WaitingLogin"] = "Waiting for login…";
            d["Converter.Ready"] = "Ready";
            d["Converter.Convert"] = "Convert";
            d["Converter.Cancel"] = "Cancel";

            // ── Token expiry check ──
            d["Converter.Expiry.Section"] = "TOKEN EXPIRY";
            d["Converter.Expiry.Check"] = "Check expiry";
            d["Converter.Expiry.Tip"] = "Detect the expiry of an access token (or a Microsoft login cookie dump).";
            d["Converter.Expiry.Idle"] = "Paste a token or cookie and check its expiry.";
            d["Converter.Expiry.InputHint"] = "Paste access token or cookie here";

            // ── Cookie → Token ──
            d["Cookie.Section"] = "COOKIE → TOKEN";
            d["Cookie.InputHint"] = "Paste Microsoft login cookie (incl. __Host-MSAAUTHP)";
            d["Cookie.Msg.MissingInput"] = "Please paste a Microsoft login cookie first.";
            d["Cookie.Status.Done"] = "Cookie converted successfully";
            d["Cookie.Status.Failed"] = "Cookie conversion failed";

            // ── Conversion mode switch ──
            d["Converter.ModeHint"] = "Mode";
            d["Converter.ModeRefresh"] = "Refresh → Access";
            d["Converter.ModeCookie"] = "Cookie → Token";
            d["Converter.Expiry.Unknown"] = "No JWT or recognizable cookie expiry found.";
            // {0}=value {1}=unit
            d["Converter.Expiry.Remaining"] = "Expires in {0} {1}";
            d["Converter.Expiry.Expired"] = "Expired {0} {1} ago";
            // {0}=absolute datetime
            d["Converter.Expiry.At"] = "(at {0})";
            d["Converter.Expiry.Day"] = "day(s)";
            d["Converter.Expiry.Hour"] = "hour(s)";
            d["Converter.Expiry.Minute"] = "minute(s)";

            // ── ViewModel messages ──
            d["Converter.Msg.MissingInput"] = "Please paste your refresh token first.";
            d["Converter.Msg.MissingInputTitle"] = "Missing input";
            d["Converter.Status.Cancelled"] = "Cancelled";
            d["Converter.Status.LoginSuccess"] = "Login successful";
            d["Converter.Status.LoginFailed"] = "Login failed";
            // {0}=player name, {1}=uuid
            d["Converter.Msg.Summary"] = "Successfully logged in\nPlayer name : {0}\nUUID        : {1}";
            d["Converter.Msg.SummaryCopied"] = "\n\nAccess token copied to clipboard.";
            d["Converter.Msg.Error400"] = "Wrong token format or expired – check with your alt seller.";
            d["Converter.Msg.Error429"] = "Too many requests – wait a moment or switch VPN node.";
            d["Converter.Msg.Error502"] = "Network error connecting to Microsoft services.";
            // {0}=friendly message
            d["Converter.Msg.SomethingWrong"] = "Something went wrong:\n\n{0}";
            d["Converter.Msg.ClearRefresh"] = "Clear the current refresh token?";
            d["Converter.Msg.ClearAccess"] = "Clear the current access token?";
            d["Converter.Msg.OverrideRefresh"] = "Override the current refresh token?";
            d["Converter.Msg.NotLikeToken"] = "The clipboard text doesn't look like a valid refresh token.\nPaste it anyway?";
            d["Converter.Msg.CustomNotConfigured"] = "Custom client ID is not configured. Click the ⚙ button next to the combo box.";
        }
    }
}
