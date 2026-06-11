using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddSkinZh(Dictionary<string, string> d)
        {
            // ── XAML ──
            d["Skin.Title"] = "玩家档案";
            d["Skin.Subtitle"] = "管理你的账号名称和皮肤，并预览其他玩家。";
            d["Skin.CurrentAccount"] = "当前账号";
            d["Skin.Name"] = "名称";
            d["Skin.Rename"] = "修改名称";
            d["Skin.NewNameHint"] = "新游戏名";
            d["Skin.RenameButton"] = "改名";
            d["Skin.Uuid"] = "UUID";
            d["Skin.Skin"] = "皮肤";
            d["Skin.Refresh"] = "刷新";
            d["Skin.FetchSkin"] = "获取皮肤";
            d["Skin.FindPlayer"] = "查找玩家皮肤";
            d["Skin.PlayerNameHint"] = "玩家名";
            d["Skin.Preview"] = "预览";
            d["Skin.SavePng"] = "保存 PNG";
            d["Skin.ApplySkin"] = "应用皮肤";
            d["Skin.ApplyDesc"] = "从本地文件或 URL 上传到你的账号。";
            d["Skin.VariantHint"] = "皮肤模型";
            d["Skin.FromFile"] = "来自文件";
            d["Skin.PathHint"] = "皮肤 PNG 路径";
            d["Skin.Browse"] = "浏览";
            d["Skin.Upload"] = "上传";
            d["Skin.FromUrl"] = "来自 URL";
            d["Skin.UrlHint"] = "PNG 直链 URL";
            d["Skin.ApplyUrl"] = "应用 URL";
            d["Skin.Scene"] = "场景";
            d["Skin.AnimationHint"] = "动画";
            d["Skin.BackgroundHint"] = "背景";
            d["Skin.PanoramaHint"] = "全景图";
            d["Skin.ClearPanorama"] = "清除全景图";
            d["Skin.ResetCamera"] = "重置相机";
            d["Skin.OrbitHint"] = "左键旋转  ·  右键平移  ·  滚轮缩放";

            // ── ViewModel status / messages ──
            d["Skin.Status.PreviewOrSignIn"] = "可自由预览其他玩家，或登录以管理你自己账号的皮肤。";
            d["Skin.Status.NoToken"] = "没有可用的 Minecraft 访问令牌。请先转换一个刷新令牌。";
            d["Skin.Status.FetchingProfile"] = "正在获取当前 Minecraft 档案…";
            d["Skin.Status.ProfileLoaded"] = "档案已加载。";
            d["Skin.Status.FetchingSkin"] = "正在获取玩家皮肤…";
            d["Skin.Status.SkinLoaded"] = "玩家皮肤已加载。";
            d["Skin.Status.NoActiveSkin"] = "没有可用的当前玩家皮肤 URL。";
            d["Skin.Status.LookingUp"] = "正在查找玩家皮肤…";
            // {0}=name
            d["Skin.Status.LoadedPreview"] = "已加载 {0} 的皮肤预览。";
            d["Skin.Status.Resolving"] = "正在解析玩家皮肤…";
            d["Skin.Status.SaveTitle"] = "保存玩家皮肤";
            d["Skin.Status.DownloadCancelled"] = "下载已取消。";
            // {0}=name
            d["Skin.Status.Saved"] = "已保存 {0} 的皮肤。";
            d["Skin.Status.Uploading"] = "正在上传皮肤文件…";
            d["Skin.Status.Uploaded"] = "皮肤上传成功。";
            d["Skin.Status.ApplyingUrl"] = "正在从 URL 应用皮肤…";
            d["Skin.Status.UrlApplied"] = "皮肤 URL 应用成功。";
            d["Skin.Status.SelectPng"] = "选择 Minecraft 皮肤 PNG";
            d["Skin.Status.EnterName"] = "请输入 Minecraft 玩家名。";
            d["Skin.Status.InvalidToken"] = "Minecraft 访问令牌无效或已过期。请重新转换刷新令牌。";
            d["Skin.Status.RateLimited"] = "Minecraft 服务正在对此账号限流。请稍候再试。";
            d["Skin.Status.Renaming"] = "正在修改游戏名…";
            d["Skin.Status.SignInFirst"] = "请先登录以管理你自己账号的皮肤。预览其他玩家无需登录。";

            // ── Enum options ──
            d["Skin.Variant.Classic"] = "经典 (Steve)";
            d["Skin.Variant.Slim"] = "纤细 (Alex)";
            d["Skin.Anim.Auto"] = "自动";
            d["Skin.Anim.Idle"] = "待机";
            d["Skin.Anim.Walk"] = "行走";
            d["Skin.Anim.Fap"] = "摆动";
            d["Skin.Bg.Bright"] = "明亮";
            d["Skin.Bg.Moody"] = "柔和";
            d["Skin.Bg.Dark"] = "深色";
            d["Skin.Bg.Panorama"] = "全景图";

            // ── Panorama presets ──
            d["Skin.Pano.old"] = "经典";
            d["Skin.Pano.aquatic"] = "水域更新";
            d["Skin.Pano.village_and_pillage"] = "村庄与掠夺";
            d["Skin.Pano.buzzy_bees"] = "嗡嗡蜜蜂";
            d["Skin.Pano.nether"] = "下界更新";
            d["Skin.Pano.caves_and_cliffs_old"] = "洞穴与山崖（旧）";
            d["Skin.Pano.caves_and_cliffs_new"] = "洞穴与山崖（新）";
            d["Skin.Pano.the_wild"] = "荒野更新";
            d["Skin.Pano.trails_and_tales"] = "足迹与故事";
            d["Skin.Pano.tricky_trials"] = "诡异的试炼";
            d["Skin.Pano.the_garden_awakens"] = "花园觉醒";
            d["Skin.Pano.spring_to_life"] = "万物生机";
            d["Skin.Pano.chase_the_skies"] = "追逐天空";
            d["Skin.Pano.tiny_takeover"] = "微缩入侵";
        }
    }
}
