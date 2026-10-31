using System;
using System.Reflection;
using GlobalEnums;
using MonoMod.RuntimeDetour;
using UnityEngine;
namespace ShaderStripMod
{
    // Stores a world-space anchor. No saved camera mode or scene coordinates survive release.
    internal sealed class CameraPinAnchor
    {
        internal Transform Target { get; private set; }
        internal Vector3 Position { get; private set; }
        internal string Scene { get; private set; }
        internal bool Active { get { return Target!=null; } }
        internal void Capture(Transform target,string scene)
        {
            if(target==null)throw new ArgumentNullException("target");
            Target=target;Position=target.position;Scene=scene;
        }
        internal void Enforce(){if(Target!=null)Target.position=Position;}
        internal void Release(){Target=null;Scene=null;Position=Vector3.zero;}
    }
    public sealed partial class ShaderStripMod
    {
        private readonly CameraPinAnchor cameraPin=new CameraPinAnchor();
        private CameraController pinnedCameraController;
        private Hook cameraPinUpdateHook;
        private string cameraPinMessage="";
        private static bool IsCameraFollowing(CameraController controller)
        {return controller!=null&&(controller.mode==CameraController.CameraMode.FOLLOWING||controller.mode==CameraController.CameraMode.LOCKED);}
        private CameraController GetPinnableCamera()
        {
            var hero=HeroController.SilentInstance;var gm=GameManager.instance;
            if(!initialized||hero==null||hero.cState==null||hero.cState.dead||hero.cState.hazardDeath||gm==null||
               !gm.IsGameplayScene()||gm.gameState==GameState.EXITING_LEVEL||gm.gameState==GameState.CUTSCENE||IsMenuScene(GetActiveSceneName()))return null;
            var cameras=GameCameras.instance;var controller=cameras!=null?cameras.cameraController:null;
            var camera=controller!=null?controller.cam:null;
            return IsCameraFollowing(controller)&&controller.isActiveAndEnabled&&camera!=null&&camera.isActiveAndEnabled&&
                camera.targetTexture==null&&camera.transform==controller.transform ? controller : null;
        }
        private void ToggleCameraPin()
        {
            if(cameraPin.Active){ReleaseCameraPin("Camera follows the hero.");return;}
            var controller=GetPinnableCamera();
            if(controller==null){cameraPinMessage="Enter a room to pin the camera. Transitions and cutscenes must finish first.";return;}
            try
            {
                if(cameraPinUpdateHook==null)
                {
                    var update=typeof(CameraController).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
                    if(update==null)throw new MissingMethodException(typeof(CameraController).FullName,"LateUpdate");
                    cameraPinUpdateHook=new Hook(update,new Action<Action<CameraController>,CameraController>(OnPinnedCameraLateUpdate));
                }
                cameraPin.Capture(controller.transform,GetActiveSceneName());pinnedCameraController=controller;
                cameraPinMessage="Camera pinned here. Press F8 again to release.";
            }
            catch(Exception ex){ReleaseCameraPin("Camera could not be pinned.");Log("Camera pin failed: "+ex.Message);}
        }
        private bool ValidateCameraPin()
        {
            if(!cameraPin.Active)return false;
            if(cameraPin.Scene!=GetActiveSceneName()||pinnedCameraController==null||GetPinnableCamera()!=pinnedCameraController)
            {ReleaseCameraPin("Camera released for a scene change, respawn or transition.");return false;}
            return true;
        }
        private void OnPinnedCameraLateUpdate(Action<CameraController> orig,CameraController self)
        {
            if(self!=pinnedCameraController||!ValidateCameraPin()){orig(self);return;}
            // The native target and lock-zone logic keep running. Freeze only this LateUpdate's
            // follow calculation, preserving the mode established by the game between frames.
            var mode=self.mode;self.mode=CameraController.CameraMode.FROZEN;
            try{orig(self);}
            finally
            {
                if(self!=null&&self.mode==CameraController.CameraMode.FROZEN)self.mode=mode;
                if(ValidateCameraPin())cameraPin.Enforce();
            }
        }
        private void UpdateCameraPin(){if(ValidateCameraPin())cameraPin.Enforce();}
        private void OnPinnedCameraPreCull(Camera camera)
        {if(cameraPin.Active&&pinnedCameraController!=null&&camera==pinnedCameraController.cam&&ValidateCameraPin())cameraPin.Enforce();}
        private void ReleaseCameraPin(string message="")
        {cameraPin.Release();pinnedCameraController=null;cameraPinMessage=message;}
        private void DisposeCameraPin()
        {
            ReleaseCameraPin();
            if(cameraPinUpdateHook!=null){cameraPinUpdateHook.Dispose();cameraPinUpdateHook=null;}
        }
    }
}
