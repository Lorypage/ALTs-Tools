using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddMiscEn(Dictionary<string, string> d)
        {
            // ── MS login progress ──
            d["Login.MsToken"] = "Getting Microsoft token…";
            d["Login.XblToken"] = "Getting Xbox Live token…";
            d["Login.XstsToken"] = "Getting XSTS token…";
            d["Login.AccessToken"] = "Getting access token…";
            d["Login.Profile"] = "Getting player profile…";

            // ── Cookie → Token ──
            d["Cookie.Step.AuthCode"] = "Getting authorization code…";
            d["Cookie.Err.NoCookie"] = "No Microsoft login cookie found. Paste a cookie export containing __Host-MSAAUTHP.";
            d["Cookie.Err.Expired"] = "Could not obtain an authorization code — the cookie may be expired.";

            // ── IGN rename VM ──
            d["Rename.Msg.NoChange"] = "The name hasn't changed.";
            d["Rename.Msg.NoChangeTitle"] = "No change";
            d["Rename.Msg.NoToken"] = "No access token available – convert a token first.";
            d["Rename.Msg.NotLoggedIn"] = "Not logged in";
            // {0}=new name
            d["Rename.Msg.Success"] = "Successfully renamed to: {0}";

            // ── IGN rename service exceptions ──
            d["RenameSvc.InvalidToken"] = "Invalid or expired access token.";
            d["RenameSvc.TooOften"] = "You are changing your name too often – wait a moment and try again.";
            d["RenameSvc.InvalidFormat"] = "Invalid name format.";
            d["RenameSvc.Wait30Days"] = "You must wait 30 days before changing your name again.";
            d["RenameSvc.Taken"] = "That name is already taken.";
            d["RenameSvc.NotAllowed"] = "That name is not allowed.";
            // {0}=code {1}=body
            d["RenameSvc.Unexpected"] = "Unexpected response ({0}): {1}";

            // ── Token injection service / helper ──
            d["Inject.Success"] = "Token successfully injected.";
            // {0}=message
            d["Inject.ReturnedFailure"] = "Injection returned failure:\n{0}";
            d["Inject.Error"] = "Injection error";
            // {0}=error
            d["Inject.SendFailed"] = "Failed to send token:\n{0}";
            d["Inject.DllError"] = "Injected DLL error";
            d["Inject.HandshakeOk"] = "Found an injected Minecraft process – ready to swap tokens.";
            // {0}=port {1}=message
            d["Inject.HandshakeFailed"] = "Handshake failed on port {0}: {1}";
            // {0}=port {1}=error
            d["Inject.HandshakeError"] = "Handshake error on port {0}:\n{1}";
            d["Inject.Title"] = "Token Injector";

            // ── Process selector (code-behind) ──
            // {0}=error
            d["ProcSel.Msg.EnumFailed"] = "Failed to enumerate Java processes:\n{0}";
            d["ProcSel.Msg.NothingSelected"] = "Please select a process from the list.";
            d["ProcSel.Msg.NothingSelectedTitle"] = "Nothing selected";
            d["ProcSel.Msg.InjectFailed"] = "DLL injection failed.\nMake sure the target process has no anti-cheat that blocks remote thread creation.";
            d["ProcSel.Msg.InjectFailedTitle"] = "Injection failed";
            d["ProcSel.Msg.NotReady"] = "DLL was injected but has not reported back yet.\nTry again in a moment.";
            d["ProcSel.Msg.NotReadyTitle"] = "Not ready";

            // ── Injection token selector (code-behind) ──
            d["InjTok.Msg.LostContact"] = "Lost contact with the target process – the process may have exited.";
            d["InjTok.Msg.LostContactTitle"] = "Process not found";
            d["InjTok.Msg.SelectAccount"] = "Please select an account from the list.";
            d["InjTok.Msg.NothingSelectedTitle"] = "Nothing selected";
            d["InjTok.Msg.PasteFirst"] = "Please paste an access token first.";
            d["InjTok.Msg.EmptyFieldTitle"] = "Empty field";
            d["InjTok.Msg.Expired"] = "This token has already expired.\nInjecting it will not result in a successful authentication.";
            d["InjTok.Msg.ExpiredTitle"] = "Expired token";
            d["InjTok.Msg.Unverified"] = "Could not verify the token's expiry date.\nIt may be invalid or in an unexpected format.\n\nInject anyway?";
            d["InjTok.Msg.UnverifiedTitle"] = "Unverified token";

            // ── Custom client ID dialog (code-behind) ──
            d["CustomClient.Msg.Incomplete"] = "Both Client ID and Scope must be filled in.";
            d["CustomClient.Msg.IncompleteTitle"] = "Incomplete";

            // ── MainWindow ──
            d["Main.Msg.LoginFirst"] = "Please convert a refresh token first before using this feature.";
            d["Main.Msg.NotLoggedIn"] = "Not logged in";
            d["Main.Msg.ListenerFailed"] = "Failed to initialise the token injection listener.\nMake sure only one instance of this program is running.";
            d["Main.Msg.ListenerFailedTitle"] = "Injection init failed";

            // ── App crash dialog ──
            // {0}=message {1}=path
            d["App.Crash"] = "An unexpected error occurred:\n\n{0}\n\nDetails have been written to:\n{1}";
            d["App.CrashTitle"] = "ALTs Tools — Error";

            // ── Helper.PopException ──
            // {0}=message {1}=source {2}=stack {3}=inner {4}=target
            d["Helper.Exception"] = "An exception occurred:\nMessage: {0}\n\nSource: {1}\n\nStackTrace: {2}\n\nInnerException: {3}\n\nTargetSite: {4}";
        }
    }
}
