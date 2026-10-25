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




