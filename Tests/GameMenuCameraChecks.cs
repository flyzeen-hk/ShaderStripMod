using System.Collections.Generic;
namespace UnityEngine
{
    public enum RenderMode { ScreenSpaceOverlay,ScreenSpaceCamera,WorldSpace }
    public sealed class GameObject { public int layer; }
    public sealed class Canvas { public RenderMode renderMode;public Camera worldCamera;public GameObject gameObject=new GameObject(); }
}
public sealed class UIManager { public static UIManager instance;public UnityEngine.Canvas UICanvas; }
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        int heroLayer=-1,visibleMask;
        bool menuBackgroundMode,menuMaskCached;
        int cachedMenuMask;
        UnityEngine.Camera GetMenuBackgroundCamera(UnityEngine.Camera[] cameras)
        {
            UnityEngine.Camera result=null;
            foreach(var camera in cameras) if(camera!=null&&camera.targetTexture==null&&(result==null||camera.depth<result.depth)) result=camera;
            return result;
        }
        string testScene="Town";
        readonly Dictionary<UnityEngine.Camera,int> strippedMasks=new Dictionary<UnityEngine.Camera,int>();
        string GetActiveSceneName() { return testScene; }
        bool IsMenuScene(string scene) { return scene.StartsWith("Menu"); }
        void AddLayerToMask(string name,ref int mask) { if(name=="uGUI") mask|=1<<27; }
        static void RunGameMenuCameraChecks()
        {
            const int player=1<<9,attack=1<<17,gui=1<<27,defaultWorld=1;
            var world=new UnityEngine.Camera {depth=0,cullingMask=player|attack|defaultWorld,clearFlags=UnityEngine.CameraClearFlags.SolidColor};
            var hud=new UnityEngine.Camera {depth=1,cullingMask=gui|(1<<5),clearFlags=UnityEngine.CameraClearFlags.Depth};
            var fx=new UnityEngine.Camera {depth=2,cullingMask=defaultWorld};
            var canvas=new UnityEngine.Canvas {renderMode=UnityEngine.RenderMode.ScreenSpaceCamera,worldCamera=hud};canvas.gameObject.layer=27;
            UIManager.instance=new UIManager {UICanvas=canvas};
            UnityEngine.Camera.main=world;UnityEngine.Camera.allCameras=new[]{world,hud,fx};
            var mod=new ShaderStripMod {visualsActive=true,heroLayer=9,visibleMask=player|attack};
            Check(mod.GetGameplayWorldCamera(UnityEngine.Camera.allCameras)==world,"Screen background stays on gameplay camera");
            foreach(var camera in UnityEngine.Camera.allCameras) mod.ConfigureGameplayCamera(camera,world);
            Check(world.cullingMask==(player|attack),"World camera hides level scenery without drawing duplicate menu");
            Check(hud.cullingMask==gui,"HUD camera keeps actual pause menu uGUI without enabling hidden HUD layer");
            Check(hud.clearFlags==UnityEngine.CameraClearFlags.Depth,"Menu camera overlays background without clearing color");
            Check(fx.cullingMask==0,"Other scene cameras remain stripped");
            Check(mod.strippedMasks[hud]==(gui|(1<<5)),"Remember original camera mask for mod unload");
            // PauseGameToggle calls MoveMenuToHUDCamera: main ^= uGUI, HUD |= uGUI.
            // Then periodic ApplyStrip formerly zeroed the HUD mask. Exercise both orders repeatedly.
            for(int cycle=0;cycle<6;cycle++)
            {
                world.cullingMask^=gui;hud.cullingMask|=gui;
                foreach(var camera in UnityEngine.Camera.allCameras) mod.ReassertGameplayCameraBeforeCull(camera);
                Check(world.cullingMask==(player|attack)&&hud.cullingMask==gui,"Opening or closing Esc does not lose menu or duplicate it on world camera");
                hud.cullingMask=0;mod.ReassertGameplayCameraBeforeCull(hud);
                Check(hud.cullingMask==gui,"Repair wiped menu mask in same rendering frame, independent of scan interval or timeScale");
                foreach(var camera in UnityEngine.Camera.allCameras) mod.ConfigureGameplayCamera(camera,world);
                Check(hud.cullingMask==gui,"Periodic strip does not hide menu after it opens");
            }
            UnityEngine.Camera.main=hud;
            Check(mod.GetGameplayWorldCamera(UnityEngine.Camera.allCameras)==world,"Tagged menu camera cannot become screen background camera");
            var offscreen=new UnityEngine.Camera {targetTexture=new object(),depth=-100,cullingMask=-1,clearFlags=UnityEngine.CameraClearFlags.Skybox};
            mod.ConfigureGameplayCamera(offscreen,world);mod.ReassertGameplayCameraBeforeCull(offscreen);
            Check(offscreen.cullingMask==-1&&offscreen.clearFlags==UnityEngine.CameraClearFlags.Skybox&&!mod.strippedMasks.ContainsKey(offscreen),"Do not alter offscreen camera masks or clear flags");
            Check(mod.GetGameplayWorldCamera(new[]{hud,offscreen})==null,"Never clear sole UI camera to draw replacement background");
            UnityEngine.Camera.main=world;canvas.worldCamera=fx;mod.ReassertGameplayCameraBeforeCull(hud);mod.ReassertGameplayCameraBeforeCull(fx);
            Check(hud.cullingMask==0&&fx.cullingMask==gui,"Follow actual Canvas camera when game moves menu");
            canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceOverlay;
            Check(mod.GetGameMenuCamera()==null,"Screen-space overlay Canvas requires no camera routing");
            mod.ConfigureGameplayCamera(hud,world);Check(hud.cullingMask==gui,"Keep original UI bit as fallback when no Canvas camera is assigned");
            UIManager.instance=null;mod.ConfigureGameplayCamera(hud,world);Check(hud.cullingMask==gui,"UI initialization fallback preserves camera's original uGUI mask");
            mod.visualsActive=false;hud.cullingMask=123;mod.ReassertGameplayCameraBeforeCull(hud);Check(hud.cullingMask==123,"Disabled mod leaves camera alone");
            mod.visualsActive=true;mod.testScene="Menu_Title";mod.ReassertGameplayCameraBeforeCull(hud);Check(hud.cullingMask==123,"Title screen keeps existing separate menu camera path");
            mod.testScene="Town";mod.heroLayer=-1;mod.ReassertGameplayCameraBeforeCull(hud);Check(hud.cullingMask==123,"Loading scene without hero does not enter gameplay camera path");
            mod.heroLayer=9;mod.visibleMask=player|attack;mod.menuBackgroundMode=mod.menuMaskCached=true;mod.cachedMenuMask=gui;
            UnityEngine.Camera.allCameras=new[]{world,hud,offscreen};
            world.cullingMask=defaultWorld|gui;hud.cullingMask=gui;world.clearFlags=UnityEngine.CameraClearFlags.Skybox;
            mod.ConfigureMenuCamera(world,world,gui);mod.ConfigureMenuCamera(hud,world,gui);mod.ConfigureMenuCamera(offscreen,world,gui);
            Check(world.cullingMask==0&&world.clearFlags==UnityEngine.CameraClearFlags.SolidColor,"Title camera masks reuse original state instead of external current bits");
            Check(hud.cullingMask==gui&&hud.clearFlags==UnityEngine.CameraClearFlags.Depth,"Title UI camera composites over selected screen background");
            Check(offscreen.cullingMask==-1&&offscreen.clearFlags==UnityEngine.CameraClearFlags.Skybox&&!mod.strippedMasks.ContainsKey(offscreen),"Title menu never alters an offscreen camera");
            mod.testScene="Menu_Title";world.cullingMask=-1;world.clearFlags=UnityEngine.CameraClearFlags.Skybox;hud.cullingMask=0;
            mod.ReassertMenuCameraBeforeCull(world);mod.ReassertMenuCameraBeforeCull(hud);mod.ReassertMenuCameraBeforeCull(offscreen);
            Check(world.cullingMask==0&&hud.cullingMask==gui&&world.clearFlags==UnityEngine.CameraClearFlags.SolidColor,"Reassert title UI and backdrop after game changes camera masks");
            mod.visualsActive=false;world.cullingMask=123;mod.ReassertMenuCameraBeforeCull(world);Check(world.cullingMask==123,"Disabled mod does not reassert title masks");
            mod.visualsActive=true;mod.testScene="Town";mod.ReassertMenuCameraBeforeCull(world);Check(world.cullingMask==123,"Menu reassertion cannot override gameplay camera state");
            mod.testScene="Menu_Title";mod.menuBackgroundMode=false;mod.ReassertMenuCameraBeforeCull(world);Check(world.cullingMask==123,"Do not reassert title masks before title setup");
            UIManager.instance=null;UnityEngine.Camera.main=null;UnityEngine.Camera.allCameras=new UnityEngine.Camera[0];
        }
    }
}
