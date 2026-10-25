using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private Canvas GetGameMenuCanvas()
        {
            var ui=UIManager.instance;return ui!=null ? ui.UICanvas : null;
        }
        private Camera GetGameMenuCamera()
        {
            var canvas=GetGameMenuCanvas();
            return canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        }
        private int GetGameMenuLayerMask()
        {
            int mask=0;
            // GameCameras.MoveMenuToHUDCamera routes the central UI Canvas on uGUI (27).
            AddLayerToMask("uGUI",ref mask);AddLayerToMask("Menu",ref mask);
            var canvas=GetGameMenuCanvas();
            if(canvas!=null && canvas.gameObject.layer>0 && canvas.gameObject.layer<=31) mask|=1<<canvas.gameObject.layer;
            return mask;
        }
        private Camera GetGameplayWorldCamera(Camera[] cameras)
        {
            var menuCamera=GetGameMenuCamera();var main=Camera.main;
            if(main!=null && main.targetTexture==null && main!=menuCamera) return main;
            Camera world=null;
            foreach(var camera in cameras)
                if(camera!=null && camera.targetTexture==null && camera!=menuCamera && (world==null || camera.depth<world.depth)) world=camera;
            return world;
        }
        private void ConfigureGameplayCamera(Camera camera,Camera world)
        {
            if(camera==null || camera.targetTexture!=null) return;
            StoreCameraState(camera);
            int mask=camera==world ? visibleMask : 0;
            var menuCamera=GetGameMenuCamera();
            if(camera==menuCamera) mask|=GetGameMenuLayerMask();
            else if(menuCamera==null) mask|=strippedMasks[camera]&GetGameMenuLayerMask();
            camera.cullingMask=mask;
            // The UI camera overlays the already rendered background and never clears its color.
            camera.clearFlags=camera==world ? CameraClearFlags.SolidColor : CameraClearFlags.Depth;
            camera.backgroundColor=stripBackground;
        }
        private void ConfigureMenuCamera(Camera camera,Camera backgroundCamera,int menuMask)
        {
            if(camera==null || camera.targetTexture!=null) return;
            StoreCameraState(camera);
            if(menuMask!=0) camera.cullingMask=strippedMasks[camera]&menuMask;
            camera.backgroundColor=stripBackground;
            if(camera==backgroundCamera) camera.clearFlags=CameraClearFlags.SolidColor;
            else if(menuMask!=0) camera.clearFlags=CameraClearFlags.Depth;
        }
        private void ReassertMenuCameraBeforeCull(Camera camera)
        {
            if(!visualsActive || !menuBackgroundMode || !menuMaskCached || !IsMenuScene(GetActiveSceneName())) return;
            ConfigureMenuCamera(camera,GetMenuBackgroundCamera(Camera.allCameras),cachedMenuMask);
        }
        private void ReassertGameplayCameraBeforeCull(Camera camera)
        {
            if(!visualsActive || camera==null || camera.targetTexture!=null || heroLayer<0 || visibleMask==0 || IsMenuScene(GetActiveSceneName())) return;
            ConfigureGameplayCamera(camera,GetGameplayWorldCamera(Camera.allCameras));
        }
    }
}
