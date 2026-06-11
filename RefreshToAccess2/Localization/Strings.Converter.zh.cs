using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddConverterZh(Dictionary<string, string> d)
        {
            // ── XAML ──
            d["Converter.Title"] = "令牌转换器";
            d["Converter.ClientIdHint"] = "ClientID";
            d["Converter.RefreshSection"] = "刷新令牌";
            d["Converter.AccessSection"] = "访问令牌";
            d["Converter.RefreshHint"] = "刷新令牌";
            d["Converter.AccessHint"] = "访问令牌";
            d["Converter.Chars"] = "字符";
            d["Converter.Player"] = "玩家";
            d["Converter.Uuid"] = "UUID";
            d["Converter.PasteTip"] = "从剪贴板粘贴";
            d["Converter.ClearTip"] = "清空";
            d["Converter.CopyTip"] = "复制到剪贴板";
            d["Converter.CopyNameTip"] = "复制玩家名";
            d["Converter.CopyUuidTip"] = "复制 UUID";
            d["Converter.AutoCopy"] = "自动复制";
            d["Converter.AutoCopyTip"] = "转换后自动复制访问令牌";
            d["Converter.ClientCustom"] = "自定义…";

            // ── ViewModel dynamic text ──
            d["Converter.WaitingLogin"] = "等待登录…";
            d["Converter.Ready"] = "就绪";
            d["Converter.Convert"] = "转换";
            d["Converter.Cancel"] = "取消";

            // ── Token expiry check ──
            d["Converter.Expiry.Section"] = "令牌时效";
            d["Converter.Expiry.Check"] = "检查时效";
            d["Converter.Expiry.Tip"] = "检测访问令牌（或微软登录 Cookie）的过期时间。";
            d["Converter.Expiry.Idle"] = "粘贴令牌或 Cookie 并检查其过期时间。";
            d["Converter.Expiry.InputHint"] = "在此粘贴访问令牌或 Cookie";

            // ── Cookie → Token ──
            d["Cookie.Section"] = "COOKIE → 令牌";
            d["Cookie.InputHint"] = "粘贴 Microsoft 登录 Cookie（含 __Host-MSAAUTHP）";
            d["Cookie.Msg.MissingInput"] = "请先粘贴 Microsoft 登录 Cookie。";
            d["Cookie.Status.Done"] = "Cookie 转换成功";
            d["Cookie.Status.Failed"] = "Cookie 转换失败";

            // ── Conversion mode switch ──
            d["Converter.ModeHint"] = "模式";
            d["Converter.ModeRefresh"] = "刷新令牌 → 访问令牌";
            d["Converter.ModeCookie"] = "Cookie → 令牌";
            d["Converter.Expiry.Unknown"] = "未找到 JWT 或可识别的 Cookie 过期时间。";
            // {0}=value {1}=unit
            d["Converter.Expiry.Remaining"] = "{0} {1}后过期";
            d["Converter.Expiry.Expired"] = "已过期 {0} {1}";
            // {0}=absolute datetime
            d["Converter.Expiry.At"] = "（{0}）";
            d["Converter.Expiry.Day"] = "天";
            d["Converter.Expiry.Hour"] = "小时";
            d["Converter.Expiry.Minute"] = "分钟";

            // ── ViewModel messages ──
            d["Converter.Msg.MissingInput"] = "请先粘贴你的刷新令牌。";
            d["Converter.Msg.MissingInputTitle"] = "缺少输入";
            d["Converter.Status.Cancelled"] = "已取消";
            d["Converter.Status.LoginSuccess"] = "登录成功";
            d["Converter.Status.LoginFailed"] = "登录失败";
            // {0}=player name, {1}=uuid
            d["Converter.Msg.Summary"] = "登录成功\n玩家名 ：{0}\nUUID  ：{1}";
            d["Converter.Msg.SummaryCopied"] = "\n\n访问令牌已复制到剪贴板。";
            d["Converter.Msg.Error400"] = "令牌格式错误或已过期——请联系你的账号卖家核实。";
            d["Converter.Msg.Error429"] = "请求过于频繁——请稍候或切换 VPN 节点。";
            d["Converter.Msg.Error502"] = "连接微软服务时出现网络错误。";
            // {0}=friendly message
            d["Converter.Msg.SomethingWrong"] = "出现问题：\n\n{0}";
            d["Converter.Msg.ClearRefresh"] = "清空当前刷新令牌？";
            d["Converter.Msg.ClearAccess"] = "清空当前访问令牌？";
            d["Converter.Msg.OverrideRefresh"] = "覆盖当前刷新令牌？";
            d["Converter.Msg.NotLikeToken"] = "剪贴板内容看起来不像有效的刷新令牌。\n仍要粘贴吗？";
            d["Converter.Msg.CustomNotConfigured"] = "尚未配置自定义 Client ID。请点击下拉框旁边的 ⚙ 按钮。";
        }
    }
}
