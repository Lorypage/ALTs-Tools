using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddAltMgrZh(Dictionary<string, string> d)
        {
            // ── XAML ──
            d["AltMgr.SearchHint"] = "搜索账号…";
            // {0}=count
            d["AltMgr.AccountCount"] = "{0} 个账号";
            d["AltMgr.ImportTip"] = "导入配置文件 (.tapf)";
            d["AltMgr.Import"] = "导入";
            d["AltMgr.MsLogin"] = "微软登录";
            d["AltMgr.MsLoginTip"] = "登录微软账号并添加到管理器";
            d["AltMgr.MsLoginHint"] = "登录你的微软账号即可添加到管理器";
            d["AltMgr.MsLoginProgress"] = "正在登录…";
            // {0}=IGN
            d["AltMgr.MsLoginSuccess"] = "✓ 已添加 {0}";
            d["AltMgr.MsLoginCancelled"] = "已取消登录";
            // {0}=error
            d["AltMgr.MsLoginFailed"] = "登录失败：\n{0}";
            d["AltMgr.Login"] = "登录";
            d["AltMgr.LoginTip"] = "刷新该账号的令牌，使其可用于注入或修改档案";
            d["AltMgr.LoggingIn"] = "正在登录…";
            // {0}=IGN
            d["AltMgr.LoginSuccess"] = "✓ 已登录 {0}——可注入或修改档案";
            // {0}=error
            d["AltMgr.LoginFailed"] = "登录失败：\n{0}";
            d["AltMgr.ToggleViewTip"] = "切换卡片 / 列表";
            d["AltMgr.ToggleSelectTip"] = "切换选择模式";
            d["AltMgr.SettingsTip"] = "设置";
            d["AltMgr.NoAccounts"] = "未找到账号";
            d["AltMgr.NoAccountsHint"] = "导入配置文件或转换令牌即可开始";
            d["AltMgr.SelectAll"] = "全选";
            d["AltMgr.Deselect"] = "取消选择";
            d["AltMgr.ExportSelected"] = "导出所选";
            d["AltMgr.ExportAll"] = "导出全部";
            d["AltMgr.DeleteAll"] = "删除全部";
            d["AltMgr.SortBy"] = "排序方式";
            d["AltMgr.SortOrderHint"] = "排序顺序";
            d["AltMgr.SortDateNewest"] = "日期（最新优先）";
            d["AltMgr.SortDateOldest"] = "日期（最早优先）";
            d["AltMgr.SortNameAZ"] = "名称（A → Z）";
            d["AltMgr.SortNameZA"] = "名称（Z → A）";
            d["AltMgr.CardSize"] = "卡片大小";
            d["AltMgr.RefreshAllHeads"] = "刷新所有头像皮肤";
            d["AltMgr.CloseEsc"] = "关闭（Esc）";
            d["AltMgr.LoggedIn"] = "登录于：";
            d["AltMgr.Client"] = "客户端：";
            d["AltMgr.Uuid"] = "UUID";
            d["AltMgr.CopyUuidTip"] = "复制 UUID";
            d["AltMgr.RefreshToken"] = "刷新令牌";
            d["AltMgr.CopyRefreshTip"] = "复制刷新令牌";
            d["AltMgr.AccessToken"] = "访问令牌";
            d["AltMgr.CopyAccessTip"] = "复制访问令牌";
            d["AltMgr.CopyAll"] = "复制全部";
            d["AltMgr.RefreshTokenTip"] = "刷新令牌";
            d["AltMgr.CopyAllTip"] = "复制全部";

            // ── Dynamic / code-behind ──
            d["AltMgr.Delete"] = "删除";
            // {0}=count
            d["AltMgr.DeleteCount"] = "删除（{0}）";
            d["AltMgr.FieldEmpty"] = "字段为空";
            d["AltMgr.NoRefreshToken"] = "无刷新令牌";
            d["AltMgr.RefreshingHeads"] = "正在刷新所有头像皮肤…";
            // {0}=count
            d["AltMgr.RefreshedHeads"] = "✓ 已刷新 {0} 个头像";
            d["AltMgr.NothingSelected"] = "未选择任何项";
            d["AltMgr.NoAccountsToExport"] = "没有可导出的账号";
            d["AltMgr.ExportTitle"] = "导出账号配置文件";
            // {0}=count
            d["AltMgr.Exported"] = "✓ 已导出 {0} 个配置文件";
            // {0}=error
            d["AltMgr.ExportFailed"] = "导出失败：\n{0}";
            d["AltMgr.ImportTitle"] = "导入账号配置文件";
            d["AltMgr.NoValidProfiles"] = "文件中没有有效的配置文件";
            // {0}=count
            d["AltMgr.ImportMode"] = "找到 {0} 个配置文件。\n\n是 → 合并\n否 → 替换";
            d["AltMgr.ImportModeTitle"] = "导入模式";
            // {0}=count
            d["AltMgr.Imported"] = "✓ 已导入 {0} 个配置文件";
            // {0}=error
            d["AltMgr.ImportFailed"] = "导入失败：\n{0}";
            // {0}=label
            d["AltMgr.Copied"] = "✓ 已复制 {0}";
            d["AltMgr.ClipboardError"] = "剪贴板错误";
            // {0}=count
            d["AltMgr.ConfirmDeleteSelected"] = "永久删除选中的 {0} 个账号？";
            d["AltMgr.ConfirmDeleteAll"] = "永久删除所有已存账号？";
            // copy-all label body
            d["AltMgr.CopyAllBody"] = "游戏名: {0}\nUUID: {1}\nClient ID: {2}\n刷新令牌: {3}\n访问令牌: {4}\n登录日期: {5}";
        }
    }
}
