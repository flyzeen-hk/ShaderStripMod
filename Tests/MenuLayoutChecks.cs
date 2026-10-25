using System;
namespace UnityEngine { public static class Screen { public static int width=1360,height=768; } }
public sealed class tk2dCamera { public bool viewportClippingEnabled;public readonly tk2dCameraSettings CameraSettings=new tk2dCameraSettings(); }
public sealed class tk2dCameraSettings {public UnityEngine.Rect rect;}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        static void RunMenuLayoutChecks()
        {
            Check(MenuResourcePolicy.Includes("Menu_Title",true,false,false,false),"Discover ordinary menu sprites");
            Check(MenuResourcePolicy.Includes("DontDestroyOnLoad",true,true,false,false),"Find persistent inactive menu pages via menu Canvas ancestry");
            Check(MenuResourcePolicy.Includes("DontDestroyOnLoad",true,false,true,true),"Find other menu UI canvases");
            Check(!MenuResourcePolicy.Includes("",false,true,true,true),"Exclude font/image prefab asset components without a valid scene");
            Check(!MenuResourcePolicy.Includes("Town",true,false,false,false),"Exclude unrelated room renderers from Menu");
            Check(!MenuResourcePolicy.Includes("DontDestroyOnLoad",true,false,false,true),"Do not mix persistent gameplay sprites into Menu solely by UI layer");
            Check(MenuResourcePolicy.Matches("НАЧАТЬ", "Title", "Menu Text", "font=Trajan\ntext=Начать игру"),"Search menu labels by their visible localized text");
            Check(MenuResourcePolicy.Matches("trajan", "Title", "Menu Text", "font=Trajan\ntext=Начать игру"),"Search by font name case-insensitively");
            Check(MenuResourcePolicy.Matches("TitleLogo", "Logo", "Menu Image", "path=UI/Main/TitleLogo"),"Search by UI object path");
            Check(MenuResourcePolicy.Matches(" ",null,null,null)&&!MenuResourcePolicy.Matches("other",null,null,null),"Search handles empty and missing metadata");
            foreach(var screen in new[]{new[]{1360,768},new[]{1920,1080},new[]{1280,720},new[]{2560,1440}})
            {
                float height=Math.Min(1,(float)screen[0]/screen[1]/(16f/9f));
                Check(MenuViewport.HasRoundingGap(0,(1-height)/2,1,height,screen[0],screen[1]),"Snap 16:9 rounding gap at common game resolution");
            }
            Check(!MenuViewport.HasRoundingGap(0,.125f,1,.75f,1024,768),"Keep intentional 4:3 letterboxing");
            Check(!MenuViewport.HasRoundingGap(.1f,0,.9f,1,1920,1080),"Keep deliberate partial camera viewport");
            Check(!MenuViewport.HasRoundingGap(0,0,1,1,0,768),"Do not normalize invalid screen dimensions");
            var mod=new ShaderStripMod {visualsActive=true,testScene="Menu_Title"};
            var rect=new UnityEngine.Rect(0,.001953125f,1,.99609375f);
            var overlay=new UnityEngine.Camera {rect=rect,clearFlags=UnityEngine.CameraClearFlags.Depth,cullingMask=1<<27};
            overlay.testTk2d=new tk2dCamera();overlay.testTk2d.CameraSettings.rect=rect;
            mod.EnforceMenuCameraFrame(overlay);
            Check(overlay.rect.y==0&&overlay.rect.height==1,"Overlay camera covers top and bottom pixel rounding gaps");
            Check(overlay.testTk2d.CameraSettings.rect.height==1,"tk2d PreCull cannot restore the small letterbox after viewport correction");
            Check(overlay.clearFlags==UnityEngine.CameraClearFlags.Depth&&overlay.cullingMask==1<<27,"Viewport correction preserves menu compositing and mask");
            overlay.rect=rect;mod.EnforceMenuCameraFrame(overlay);mod.RestoreBackgroundCameraFrames();
            Check(overlay.testTk2d.CameraSettings.rect.height==rect.height,"Restore tk2d settings with the original camera rectangle");
            Check(overlay.rect.y==rect.y&&overlay.rect.height==rect.height,"Restore original overlay viewport on disable/scene change");
            mod.testScene="Town";mod.EnforceMenuCameraFrame(overlay);Check(overlay.rect.y==rect.y,"Do not resize gameplay UI cameras");
            mod.testScene="Menu_Title";overlay.targetTexture=new object();mod.EnforceMenuCameraFrame(overlay);Check(overlay.rect.y==rect.y,"Do not resize render-to-texture cameras");
            overlay.targetTexture=null;overlay.testTk2d.viewportClippingEnabled=true;mod.EnforceMenuCameraFrame(overlay);
            Check(overlay.testTk2d.CameraSettings.rect.height==rect.height,"Keep tk2d explicit clipping settings");
            mod.RestoreBackgroundCameraFrames();
            foreach(var screen in new[]{new[]{1360,768},new[]{1920,1080},new[]{1280,720},new[]{800,600},new[]{640,480}})
            {
                float scale=Math.Min(1,Math.Min((screen[0]-20f)/940,(screen[1]-20f)/620));
                var window=MaskWorkspaceLayout.Window(screen[0],screen[1],scale);
                var full=MaskWorkspaceLayout.Content(window.Width,window.Height);
                var canvas=MaskWorkspaceLayout.Canvas(full.X+8,full.Y+8+36,full.Width-16,full.Y+full.Height-8);
                Check(canvas.Width*scale>=screen[0]-50,"Gray selection area uses almost the entire physical screen width");
                Check(canvas.Height*scale>=screen[1]*.7f,"Default gray selection area uses at least 70 percent of screen height");
                Check(canvas.Y+canvas.Height+30<=full.Y+full.Height-8,"Status and gray canvas remain inside editor bounds");
                if(screen[0]==1360)Check(canvas.Width==1312&&canvas.Height==598,"Screenshot resolution provides a 1312 by 598 gray selection area");
            }
            Check(UiLocalization.Text("Menu","Russian")=="Меню","Menu tab respects configured UI language");
            Check(UiLocalization.Text("Back to browser","Russian")=="Вернуться к списку","Large editor has localized return action");
        }
    }
}