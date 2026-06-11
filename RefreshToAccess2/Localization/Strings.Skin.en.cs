using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    public static partial class Strings
    {
        private static void AddSkinEn(Dictionary<string, string> d)
        {
            // ── XAML ──
            d["Skin.Title"] = "Player Profile";
            d["Skin.Subtitle"] = "Manage your account name and skin, and preview other players.";
            d["Skin.CurrentAccount"] = "Current account";
            d["Skin.Name"] = "Name";
            d["Skin.Rename"] = "Change name";
            d["Skin.NewNameHint"] = "New profile name";
            d["Skin.RenameButton"] = "Rename";
            d["Skin.Uuid"] = "UUID";
            d["Skin.Skin"] = "Skin";
            d["Skin.Refresh"] = "Refresh";
            d["Skin.FetchSkin"] = "Fetch skin";
            d["Skin.FindPlayer"] = "Find player skin";
            d["Skin.PlayerNameHint"] = "Player name";
            d["Skin.Preview"] = "Preview";
            d["Skin.SavePng"] = "Save PNG";
            d["Skin.ApplySkin"] = "Apply skin";
            d["Skin.ApplyDesc"] = "Upload to your account from a local file or URL.";
            d["Skin.VariantHint"] = "Skin variant";
            d["Skin.FromFile"] = "From file";
            d["Skin.PathHint"] = "Path to skin PNG";
            d["Skin.Browse"] = "Browse";
            d["Skin.Upload"] = "Upload";
            d["Skin.FromUrl"] = "From URL";
            d["Skin.UrlHint"] = "Direct PNG URL";
            d["Skin.ApplyUrl"] = "Apply URL";
            d["Skin.Scene"] = "Scene";
            d["Skin.AnimationHint"] = "Animation";
            d["Skin.BackgroundHint"] = "Background";
            d["Skin.PanoramaHint"] = "Panorama";
            d["Skin.ClearPanorama"] = "Clear panorama";
            d["Skin.ResetCamera"] = "Reset camera";
            d["Skin.OrbitHint"] = "Left-click orbit  ·  Right-click pan  ·  Scroll to zoom";

            // ── ViewModel status / messages ──
            d["Skin.Status.PreviewOrSignIn"] = "Preview other players freely, or sign in to manage your own account skin.";
            d["Skin.Status.NoToken"] = "No Minecraft access token is available. Convert a refresh token first.";
            d["Skin.Status.FetchingProfile"] = "Fetching current Minecraft profile...";
            d["Skin.Status.ProfileLoaded"] = "Profile loaded.";
            d["Skin.Status.FetchingSkin"] = "Fetching player skin...";
            d["Skin.Status.SkinLoaded"] = "Player skin loaded.";
            d["Skin.Status.NoActiveSkin"] = "No active player skin URL is available.";
            d["Skin.Status.LookingUp"] = "Looking up player skin...";
            // {0}=name
            d["Skin.Status.LoadedPreview"] = "Loaded {0}'s skin preview.";
            d["Skin.Status.Resolving"] = "Resolving player skin...";
            d["Skin.Status.SaveTitle"] = "Save player skin";
            d["Skin.Status.DownloadCancelled"] = "Download cancelled.";
            // {0}=name
            d["Skin.Status.Saved"] = "Saved {0}'s skin.";
            d["Skin.Status.Uploading"] = "Uploading skin file...";
            d["Skin.Status.Uploaded"] = "Skin uploaded successfully.";
            d["Skin.Status.ApplyingUrl"] = "Applying skin from URL...";
            d["Skin.Status.UrlApplied"] = "Skin URL applied successfully.";
            d["Skin.Status.SelectPng"] = "Select Minecraft Skin PNG";
            d["Skin.Status.EnterName"] = "Enter a Minecraft player name.";
            d["Skin.Status.InvalidToken"] = "The Minecraft access token is invalid or expired. Convert the refresh token again.";
            d["Skin.Status.RateLimited"] = "Minecraft Services is rate-limiting this account right now. Wait a moment and try again.";
            d["Skin.Status.Renaming"] = "Renaming profile...";
            d["Skin.Status.SignInFirst"] = "Sign in first to manage your own account skin. Previewing other players works without login.";

            // ── Enum options ──
            d["Skin.Variant.Classic"] = "Classic";
            d["Skin.Variant.Slim"] = "Slim";
            d["Skin.Anim.Auto"] = "Auto";
            d["Skin.Anim.Idle"] = "Idle";
            d["Skin.Anim.Walk"] = "Walk";
            d["Skin.Anim.Fap"] = "Fap";
            d["Skin.Bg.Bright"] = "Bright";
            d["Skin.Bg.Moody"] = "Moody";
            d["Skin.Bg.Dark"] = "Dark";
            d["Skin.Bg.Panorama"] = "Panorama";

            // ── Panorama presets ──
            d["Skin.Pano.old"] = "Old";
            d["Skin.Pano.aquatic"] = "Aquatic";
            d["Skin.Pano.village_and_pillage"] = "Village & Pillage";
            d["Skin.Pano.buzzy_bees"] = "Buzzy Bees";
            d["Skin.Pano.nether"] = "Nether";
            d["Skin.Pano.caves_and_cliffs_old"] = "Caves & Cliffs Old";
            d["Skin.Pano.caves_and_cliffs_new"] = "Caves & Cliffs New";
            d["Skin.Pano.the_wild"] = "The Wild";
            d["Skin.Pano.trails_and_tales"] = "Trails & Tales";
            d["Skin.Pano.tricky_trials"] = "Tricky Trials";
            d["Skin.Pano.the_garden_awakens"] = "The Garden Awakens";
            d["Skin.Pano.spring_to_life"] = "Spring to Life";
            d["Skin.Pano.chase_the_skies"] = "Chase the Skies";
            d["Skin.Pano.tiny_takeover"] = "Tiny Takeover";
        }
    }
}
