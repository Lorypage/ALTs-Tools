using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddMiscZh(Dictionary<string, string> d)
        {
            // ── MS login progress ──
            d["Login.MsToken"] = "正在获取 Microsoft 令牌…";
            d["Login.XblToken"] = "正在获取 Xbox Live 令牌…";
            d["Login.XstsToken"] = "正在获取 XSTS 令牌…";
            d["Login.AccessToken"] = "正在获取访问令牌…";
            d["Login.Profile"] = "正在获取玩家档案…";

            // ── Cookie → Token ──
            d["Cookie.Step.AuthCode"] = "正在获取授权码…";
            d["Cookie.Err.NoCookie"] = "未找到 Microsoft 登录 Cookie。请粘贴包含 __Host-MSAAUTHP 的 Cookie 导出内容。";
            d["Cookie.Err.Expired"] = "无法获取授权码——Cookie 可能已过期。";

            // ── IGN rename VM ──
            d["Rename.Msg.NoChange"] = "名称没有变化。";
            d["Rename.Msg.NoChangeTitle"] = "无变化";
            d["Rename.Msg.NoToken"] = "没有可用的访问令牌——请先转换一个令牌。";
            d["Rename.Msg.NotLoggedIn"] = "未登录";
            // {0}=new name
            d["Rename.Msg.Success"] = "已成功改名为：{0}";

            // ── IGN rename service exceptions ──
            d["RenameSvc.InvalidToken"] = "访问令牌无效或已过期。";
            d["RenameSvc.TooOften"] = "你改名过于频繁——请稍候再试。";
            d["RenameSvc.InvalidFormat"] = "名称格式无效。";
            d["RenameSvc.Wait30Days"] = "你必须等待 30 天才能再次改名。";
            d["RenameSvc.Taken"] = "该名称已被占用。";
            d["RenameSvc.NotAllowed"] = "该名称不被允许。";
            // {0}=code {1}=body
            d["RenameSvc.Unexpected"] = "意外的响应（{0}）：{1}";

            // ── Token injection service / helper ──
            d["Inject.Success"] = "令牌注入成功。";
            // {0}=message
            d["Inject.ReturnedFailure"] = "注入返回失败：\n{0}";
            d["Inject.Error"] = "注入错误";
            // {0}=error
            d["Inject.SendFailed"] = "发送令牌失败：\n{0}";
            d["Inject.DllError"] = "注入的 DLL 错误";
            d["Inject.HandshakeOk"] = "已找到已注入的 Minecraft 进程——可以交换令牌了。";
            // {0}=port {1}=message
            d["Inject.HandshakeFailed"] = "端口 {0} 握手失败：{1}";
            // {0}=port {1}=error
            d["Inject.HandshakeError"] = "端口 {0} 握手错误：\n{1}";
            d["Inject.Title"] = "令牌注入器";

            // ── Process selector (code-behind) ──
            // {0}=error
            d["ProcSel.Msg.EnumFailed"] = "枚举 Java 进程失败：\n{0}";
            d["ProcSel.Msg.NothingSelected"] = "请从列表中选择一个进程。";
            d["ProcSel.Msg.NothingSelectedTitle"] = "未选择";
            d["ProcSel.Msg.InjectFailed"] = "DLL 注入失败。\n请确保目标进程没有阻止远程线程创建的反作弊。";
            d["ProcSel.Msg.InjectFailedTitle"] = "注入失败";
            d["ProcSel.Msg.NotReady"] = "DLL 已注入但尚未回报。\n请稍候再试。";
            d["ProcSel.Msg.NotReadyTitle"] = "尚未就绪";

            // ── Injection token selector (code-behind) ──
            d["InjTok.Msg.LostContact"] = "与目标进程失去联系——该进程可能已退出。";
            d["InjTok.Msg.LostContactTitle"] = "未找到进程";
            d["InjTok.Msg.SelectAccount"] = "请从列表中选择一个账号。";
            d["InjTok.Msg.NothingSelectedTitle"] = "未选择";
            d["InjTok.Msg.PasteFirst"] = "请先粘贴一个访问令牌。";
            d["InjTok.Msg.EmptyFieldTitle"] = "字段为空";
            d["InjTok.Msg.Expired"] = "该令牌已过期。\n注入它将无法成功通过身份验证。";
            d["InjTok.Msg.ExpiredTitle"] = "令牌已过期";
            d["InjTok.Msg.Unverified"] = "无法验证该令牌的过期日期。\n它可能无效或格式异常。\n\n仍要注入吗？";
            d["InjTok.Msg.UnverifiedTitle"] = "未验证的令牌";

            // ── Custom client ID dialog (code-behind) ──
            d["CustomClient.Msg.Incomplete"] = "Client ID 和 Scope 都必须填写。";
            d["CustomClient.Msg.IncompleteTitle"] = "信息不完整";

            // ── MainWindow ──
            d["Main.Msg.LoginFirst"] = "使用此功能前，请先转换一个刷新令牌。";
            d["Main.Msg.NotLoggedIn"] = "未登录";
            d["Main.Msg.ListenerFailed"] = "初始化令牌注入监听器失败。\n请确保本程序只运行了一个实例。";
            d["Main.Msg.ListenerFailedTitle"] = "注入初始化失败";

            // ── App crash dialog ──
            // {0}=message {1}=path
            d["App.Crash"] = "发生了意外错误：\n\n{0}\n\n详细信息已写入：\n{1}";
            d["App.CrashTitle"] = "ALTs Tools — 错误";

            // ── Helper.PopException ──
            // {0}=message {1}=source {2}=stack {3}=inner {4}=target
            d["Helper.Exception"] = "发生异常：\n消息: {0}\n\n来源: {1}\n\n堆栈跟踪: {2}\n\n内部异常: {3}\n\n目标方法: {4}";

            // ── Auto-update ──
            d["Update.Title"] = "发现新版本";
            // {0}=current {1}=latest
            d["Update.VersionLine"] = "当前版本 {0}，最新版本 {1}。是否立即更新？";
            d["Update.NoNotes"] = "（本次更新没有提供更新说明）";
            d["Update.Now"] = "立即更新";
            d["Update.Later"] = "稍后";
            d["Update.Downloading"] = "正在下载更新…";
            // {0}=percent
            d["Update.DownloadingPct"] = "正在下载更新… {0}%";
            d["Update.Restarting"] = "下载完成，正在重启以应用更新…";
            // {0}=error
            d["Update.Failed"] = "更新失败：\n{0}";
            d["Update.CheckFailed"] = "无法检查更新。请检查网络连接后稍后再试。";
            d["Update.RateLimited"] = "GitHub 暂时限制了更新检查（来自你网络的请求过于频繁）。请稍后再试。";
            d["Update.NoRelease"] = "未找到可更新的已发布版本。";
            // {0}=current
            d["Update.UpToDate"] = "你已是最新版本（{0}）。";
        }
    }
}
