using System;
namespace UnityEngine
{
    public static class Time { public static float timeScale = 1; public static int frameCount; }
    public static class AudioListener { public static bool pause; }
}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private bool textureBrowserVisible, pauseGameWhileBrowserOpen, textureBrowserPauseActive;
        private float textureBrowserPreviousTimeScale;
        static int checks;
        static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
        static bool Near(float a, float b) { return Math.Abs(a-b) < 0.0001f; }
        public static void Main()
        {
            Check(BrowserInputPolicy.ShouldBlock(true, false, 10, 0), "Block hero input when browser opens");
            Check(BrowserInputPolicy.ShouldBlock(false, true, 12, 10), "Block held close click");
            Check(BrowserInputPolicy.ShouldBlock(false, false, 11, 11), "Block close frame");
            Check(!BrowserInputPolicy.ShouldBlock(false, false, 12, 11), "Restore input after release");
            Check(!BrowserInputPolicy.ShouldToggleCameraPin(true, true), "F8 camera hotkey ignored while F9 is open");
            Check(BrowserInputPolicy.ShouldToggleCameraPin(false, true), "F8 camera hotkey works after F9 closes");
            Check(!BrowserInputPolicy.ShouldToggleCameraPin(false, false), "F8 camera hotkey requires a press");
            Check(RenderStatePolicy.Classify(true, true) == RenderSceneState.Menu, "Menu scene wins even with a live hero");
            Check(RenderStatePolicy.Classify(true, false) == RenderSceneState.Menu, "Title menu classified as menu");
            Check(RenderStatePolicy.Classify(false, true) == RenderSceneState.GameplayWithHero, "Gameplay scene with hero classified as gameplay");
            Check(RenderStatePolicy.Classify(false, false) == RenderSceneState.GameplayWithoutHero, "Transition scene without hero is not a menu");
            var preserveStats = new PreserveRunStats();
            preserveStats.ResetFrame();
            preserveStats.RecordStale(PreserveSkipKind.Disabled);
            preserveStats.RecordStale(PreserveSkipKind.Disabled);
            preserveStats.RecordStale(PreserveSkipKind.Inactive);
            preserveStats.RecordStale(PreserveSkipKind.AlreadyVisible);
            preserveStats.RecordStale(PreserveSkipKind.ExpensiveParticle);
            preserveStats.RecordStale(PreserveSkipKind.OtherScene);
            preserveStats.RecordStale(PreserveSkipKind.NoRule);
            preserveStats.RecordStale(PreserveSkipKind.NoMaterials);
            preserveStats.RecordStale(PreserveSkipKind.ProxyRenderer);
            preserveStats.RecordStale(PreserveSkipKind.MissingRenderer);
            Check(preserveStats.Stale == 10, "Preserve stats count every blocked renderer");
            Check(preserveStats.SkippedDisabled == 2, "Preserve stats count repeated block reasons");
            Check(preserveStats.SkippedInactive == 1 && preserveStats.SkippedAlreadyVisible == 1, "Preserve stats split inactive and visible blocks");
            Check(preserveStats.SkippedExpensiveParticle == 1 && preserveStats.SkippedOtherScene == 1, "Preserve stats split particle and scene blocks");
            Check(preserveStats.SkippedNoRule == 1 && preserveStats.SkippedNoMaterials == 1, "Preserve stats split rule and material blocks");
            Check(preserveStats.SkippedProxy == 1 && preserveStats.SkippedMissing == 1, "Preserve stats split proxy and missing blocks");
            preserveStats.ResetFrame();
            Check(preserveStats.Stale == 0 && preserveStats.SkippedDisabled == 0, "Preserve frame stats reset");
            preserveStats.CandidateMatched = 7;
            preserveStats.Sources = 5;
            preserveStats.SkippedByDrawLimit = 1;
            preserveStats.SkippedByParticleLimit = 1;
            preserveStats.ResetScan();
            Check(preserveStats.CandidateMatched == 0 && preserveStats.Sources == 0 && preserveStats.SkippedByDrawLimit == 0 && preserveStats.SkippedByParticleLimit == 0, "Preserve scan stats reset");
            Check(PreserveRunStats.Describe(PreserveSkipKind.Drawable) == "none", "Drawable renderer has no skip reason");
            Check(PreserveRunStats.Describe(PreserveSkipKind.Disabled) == "renderer disabled", "Disabled renderer reason");
            Check(PreserveRunStats.Describe(PreserveSkipKind.AlreadyVisible) == "renderer layer is already visible to the main camera", "Already visible renderer reason");
            Check(PreserveRunStats.Describe(PreserveSkipKind.OtherScene) == "scene mismatch or non-gameplay scene", "Other scene renderer reason");
            const string deleteConfirm = "Click Delete permanently again to confirm. This cannot be undone.";
            Check(UiLocalization.Text(deleteConfirm, "Russian") == "Нажмите «Удалить навсегда» ещё раз для подтверждения. Отменить это действие будет невозможно.", "Single-file delete confirmation is localized");
            Check(UiLocalization.Text(deleteConfirm, "English") == deleteConfirm, "Single-file delete confirmation round-trips to English");
            Check(MediaTrashPolicy.GetOriginalName("0123456789abcdef0123456789abcdef_my image.png") == "my image.png", "Trash GUID prefix is stripped");
            Check(MediaTrashPolicy.GetOriginalName("plain.png") == "plain.png", "Trash name without GUID prefix is unchanged");
            Check(MediaTrashPolicy.GetOriginalName("not-a-guid-prefix-0123456789abcdef_x.png") == "not-a-guid-prefix-0123456789abcdef_x.png", "Trash name with non-hex prefix is unchanged");
            Check(MediaTrashPolicy.FormatSize(512) == "512 B", "Trash size in bytes");
            Check(MediaTrashPolicy.FormatSize(2048) == "2 KB", "Trash size in kilobytes");
            Check(MediaTrashPolicy.FormatSize(5L * 1024 * 1024) == "5 MB", "Trash size in megabytes");
            Check(MediaTrashPolicy.IsPathInside(System.IO.Path.Combine("C:", "media", "removed", "a.png"), System.IO.Path.Combine("C:", "media", "removed")), "Trash path inside directory");
            Check(!MediaTrashPolicy.IsPathInside(System.IO.Path.Combine("C:", "media", "a.png"), System.IO.Path.Combine("C:", "media", "removed")), "Media path outside trash rejected");
            Check(!MediaTrashPolicy.IsPathInside(System.IO.Path.Combine("C:", "media", "removed2", "a.png"), System.IO.Path.Combine("C:", "media", "removed")), "Sibling trash directory rejected");
            Check(!MediaTrashPolicy.IsPathInside("", System.IO.Path.Combine("C:", "media", "removed")), "Empty trash path rejected");
            string trashDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "trashcheck");
            string firstRestore = MediaTrashPolicy.GetUniqueFilePath(trashDir, "a.png", p => false);
            Check(firstRestore.EndsWith("a.png", StringComparison.OrdinalIgnoreCase), "Restore keeps original name when free");
            string secondRestore = MediaTrashPolicy.GetUniqueFilePath(trashDir, "a.png", p => p.EndsWith("a.png", StringComparison.OrdinalIgnoreCase));
            Check(secondRestore.EndsWith("a (2).png", StringComparison.OrdinalIgnoreCase), "Restore adds a numeric suffix on collision");
            string thirdRestore = MediaTrashPolicy.GetUniqueFilePath(trashDir, "a.png", p => p.EndsWith("a.png", StringComparison.OrdinalIgnoreCase) || p.EndsWith("a (2).png", StringComparison.OrdinalIgnoreCase));
            Check(thirdRestore.EndsWith("a (3).png", StringComparison.OrdinalIgnoreCase), "Restore increments the collision suffix");
            var m = new ShaderStripMod();
            m.textureBrowserVisible = true; m.pauseGameWhileBrowserOpen = true;
            UnityEngine.Time.timeScale = .25f;
            m.UpdateTextureBrowserPause();
            Check(UnityEngine.Time.timeScale == 0 && UnityEngine.AudioListener.pause, "Pause time AND sound");
            UnityEngine.Time.timeScale = 1;
            m.UpdateTextureBrowserPause();
            Check(UnityEngine.Time.timeScale == 0, "Game cannot undo browser pause");
            m.textureBrowserVisible = false; m.UpdateTextureBrowserPause();
            Check(UnityEngine.Time.timeScale == .25f && !UnityEngine.AudioListener.pause, "Restore initial time and audio state");
            UnityEngine.Time.timeScale = 0; UnityEngine.AudioListener.pause = true;
            m.textureBrowserVisible = true; m.UpdateTextureBrowserPause(); m.ReleaseTextureBrowserPause();
            Check(UnityEngine.Time.timeScale == 0 && UnityEngine.AudioListener.pause, "Preserve external pause");
            m.textureBrowserVisible = false; UnityEngine.Time.timeScale = 1; UnityEngine.AudioListener.pause = false;
            m.ReleaseTextureBrowserPause(); Check(UnityEngine.Time.timeScale == 1 && !UnityEngine.AudioListener.pause, "Release is idempotent");
            foreach (float imageWidth in new float[]{200,1920,4000})
            foreach (float imageHeight in new float[]{100,1080,3000})
            foreach (float screenWidth in new float[]{640,1920})
            foreach (float screenHeight in new float[]{480,1080})
            {
                var fill = BackgroundImageGeometry.Calculate(imageWidth,imageHeight,screenWidth,screenHeight,"Cover");
                Check(fill.Width >= .9999f && fill.Height >= .9999f, "Fill covers viewport");
                Check(Near(fill.Width,1) || Near(fill.Height,1), "Fill has one matching edge");
                var fit = BackgroundImageGeometry.Calculate(imageWidth,imageHeight,screenWidth,screenHeight,"Contain");
                Check(fit.Width <= 1.0001f && fit.Height <= 1.0001f, "Fit keeps entire image");
                Check(Near(fit.Width,1) || Near(fit.Height,1), "Fit touches an edge");
                var stretch = BackgroundImageGeometry.Calculate(imageWidth,imageHeight,screenWidth,screenHeight,"Stretch");
                Check(stretch.Width == 1 && stretch.Height == 1, "Stretch matches viewport");
                Check(Near(fill.Width/fill.Height * screenWidth/screenHeight,imageWidth/imageHeight), "Fill preserves aspect ratio");
                Check(Near(fit.Width/fit.Height * screenWidth/screenHeight,imageWidth/imageHeight), "Fit preserves aspect ratio");
            }
            bool invalid = false;
            try { BackgroundImageGeometry.Calculate(0,10,100,100,"Cover"); } catch(ArgumentOutOfRangeException){invalid=true;}
            Check(invalid,"Reject invalid dimensions");
            RunEditorInputLanguageChecks();
            RunGameMenuCameraChecks();
            RunMaskPointerChecks();
            RunMenuLayoutChecks();
            RunMaskChecks();
            RunMaskPreviewChecks();
            Console.WriteLine("PASS: " + checks + " regression assertions (production preview pixels, mask clipping/persistence, native visibility, camera viewport, layout, pause and input).");
        }
    }
}




