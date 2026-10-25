using System.Collections.Generic;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private readonly Dictionary<Camera,Rect> backgroundCameraRects=new Dictionary<Camera,Rect>();
        private readonly Dictionary<tk2dCamera,Rect> menuTk2dRects=new Dictionary<tk2dCamera,Rect>();
        private void EnforceBackgroundCameraFrame(Camera camera)
        {
            if(!visualsActive||camera==null||camera.targetTexture!=null) return;
            StoreCameraState(camera);
            if(!backgroundCameraRects.ContainsKey(camera)) backgroundCameraRects.Add(camera,camera.rect);
            // A partial camera viewport leaves pixels drawn by IMGUI outside it, even after F9 closes.
            camera.rect=new Rect(0,0,1,1);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=stripBackground;
        }
        private void EnforceMenuCameraFrame(Camera camera)
        {
            if(!visualsActive || camera==null || camera.targetTexture!=null || !IsMenuScene(GetActiveSceneName())) return;
            Rect rect=camera.rect;
            if(!MenuViewport.HasRoundingGap(rect.x,rect.y,rect.width,rect.height,Screen.width,Screen.height)) return;
            if(!backgroundCameraRects.ContainsKey(camera)) backgroundCameraRects.Add(camera,rect);
            camera.rect=new Rect(0,0,1,1);
            var tk2d=camera.GetComponent<tk2dCamera>();
            if(tk2d!=null && !tk2d.viewportClippingEnabled)
            {
                Rect settings=tk2d.CameraSettings.rect;
                if(MenuViewport.HasRoundingGap(settings.x,settings.y,settings.width,settings.height,Screen.width,Screen.height))
                {
                    if(!menuTk2dRects.ContainsKey(tk2d)) menuTk2dRects.Add(tk2d,settings);
                    // tk2dCamera.OnPreCull recalculates projection and copies this setting back to camera.rect.
                    tk2d.CameraSettings.rect=new Rect(0,0,1,1);
                }
            }
        }
        private void RestoreBackgroundCameraFrames()
        {
            foreach(var pair in backgroundCameraRects) if(pair.Key!=null) pair.Key.rect=pair.Value;
            backgroundCameraRects.Clear();
            foreach(var pair in menuTk2dRects) if(pair.Key!=null) pair.Key.CameraSettings.rect=pair.Value;
            menuTk2dRects.Clear();
        }
    }
}

