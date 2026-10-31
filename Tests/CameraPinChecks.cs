using System;
using System.Reflection;
using UnityEngine;
using MonoMod.RuntimeDetour;
namespace GlobalEnums { public enum GameState { PLAYING,PAUSED,EXITING_LEVEL,CUTSCENE } }
namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        public static bool operator ==(Object a,Object b)
        {bool na=ReferenceEquals(a,null)||(!ReferenceEquals(a,null)&&a.destroyed),nb=ReferenceEquals(b,null)||(!ReferenceEquals(b,null)&&b.destroyed);return na||nb ? na==nb : ReferenceEquals(a,b);}
        public static bool operator !=(Object a,Object b){return !(a==b);}
        public override bool Equals(object other){return ReferenceEquals(this,other);}
        public override int GetHashCode(){return base.GetHashCode();}
    }
    public struct Vector3
    {
        public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
        public static Vector3 zero {get{return new Vector3();}}
        public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
        public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
    }
    public class Transform:Object
    {
        public Transform parent;public Vector3 localPosition;
        public Vector3 position {get{return parent==null ? localPosition : parent.position+localPosition;}set{localPosition=parent==null ? value : value-parent.position;}}
    }
    public class Camera:Object { public Transform transform;public bool isActiveAndEnabled=true;public object targetTexture; }
}
public class CameraController:UnityEngine.Object
{
    public enum CameraMode { FROZEN,FOLLOWING,LOCKED,PANNING,FADEOUT,FADEIN,PREVIOUS }
    public CameraMode mode=CameraMode.FOLLOWING;public bool isActiveAndEnabled=true;
    public Transform transform=new Transform();public Camera cam;
    public CameraController(){cam=new Camera{transform=transform};}
    private void LateUpdate(){}
}
public class HeroController:UnityEngine.Object
{public static HeroController SilentInstance;public State cState=new State();public class State{public bool dead,hazardDeath;}}
public class GameManager
{public static GameManager instance;public bool gameplay=true;public GlobalEnums.GameState gameState=GlobalEnums.GameState.PLAYING;public bool IsGameplayScene(){return gameplay;}}
public class GameCameras {public static GameCameras instance;public CameraController cameraController;}
namespace MonoMod.RuntimeDetour
{
    public class Hook:IDisposable
    {
        public static bool fail;public static int created,disposed;
        public Hook(MethodInfo method,Delegate callback){if(fail)throw new Exception("simulated hook failure");created++;}
        public void Dispose(){disposed++;}
    }
}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        bool initialized=true;string scene="RoomA";static int checks;
        string GetActiveSceneName(){return scene;}bool IsMenuScene(string value){return value=="Menu_Title";}void Log(string value){}
        static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
        static bool Same(Vector3 a,Vector3 b){return Math.Abs(a.x-b.x)<.001&&Math.Abs(a.y-b.y)<.001&&Math.Abs(a.z-b.z)<.001;}
        static CameraController Setup()
        {
            Hook.fail=false;HeroController.SilentInstance=new HeroController();GameManager.instance=new GameManager();
            var controller=new CameraController();GameCameras.instance=new GameCameras{cameraController=controller};
            controller.transform.position=new Vector3(10,20,-38);return controller;
        }
        public static void Main()
        {
            var controller=Setup();var mod=new ShaderStripMod();Vector3 anchor=controller.transform.position;
            mod.ToggleCameraPin();Check(mod.cameraPin.Active&&Same(mod.cameraPin.Position,anchor),"capture current position");
            Check(controller.mode==CameraController.CameraMode.FOLLOWING,"pin does not persistently change native mode");
            mod.OnPinnedCameraLateUpdate(c=>{Check(c.mode==CameraController.CameraMode.FROZEN,"freeze only inside native follow callback");c.transform.position=new Vector3(900,20,-38);},controller);
            Check(Same(controller.transform.position,anchor)&&controller.mode==CameraController.CameraMode.FOLLOWING,"cancel native position clamp and restore following");
            controller.mode=CameraController.CameraMode.LOCKED;
            mod.OnPinnedCameraLateUpdate(c=>{},controller);Check(controller.mode==CameraController.CameraMode.LOCKED&&mod.cameraPin.Active,"retain new native lock area while pinned");
            var other=new CameraController();bool otherRan=false;
            mod.OnPinnedCameraLateUpdate(c=>{otherRan=true;Check(c.mode==CameraController.CameraMode.FOLLOWING,"other controller not frozen");},other);
            Check(otherRan&&mod.cameraPin.Active,"unrelated native update still runs");
            var parent=new Transform();controller.transform.parent=parent;controller.transform.position=anchor;parent.position=new Vector3(30,-70,10);
            mod.OnPinnedCameraPreCull(other.cam);Check(!Same(controller.transform.position,anchor),"other camera cannot reassert pinned transform");
            mod.OnPinnedCameraPreCull(controller.cam);Check(Same(controller.transform.position,anchor),"reassert world coordinates after parent shake");
            bool threw=false;try{mod.OnPinnedCameraLateUpdate(c=>{c.transform.position=Vector3.zero;throw new InvalidOperationException();},controller);}catch(InvalidOperationException){threw=true;}
            Check(threw&&Same(controller.transform.position,anchor)&&controller.mode==CameraController.CameraMode.LOCKED,"restore native mode and position after native exception");
            int hooks=Hook.created;mod.ToggleCameraPin();Check(!mod.cameraPin.Active&&controller.mode==CameraController.CameraMode.LOCKED,"F8 release retains current lock mode");
            mod.OnPinnedCameraLateUpdate(c=>c.transform.position=Vector3.zero,controller);Check(Same(controller.transform.position,Vector3.zero),"follow update moves again after release");
            mod.ToggleCameraPin();Check(Hook.created==hooks&&Same(mod.cameraPin.Position,Vector3.zero),"reuse hook and capture new location");
            mod.DisposeCameraPin();Check(!mod.cameraPin.Active&&mod.cameraPinUpdateHook==null&&Hook.disposed==1,"unload releases and disposes hook");
            foreach(string invalid in new[]{"uninitialized","menu","nohero","dead","hazard","nostate","nogm","nongameplay","exiting","cutscene","nocameras","nocontroller","disabled","disabledcamera","nocamera","rtt","differenttransform","panning","fade","frozen"})
            {
                controller=Setup();mod=new ShaderStripMod();
                switch(invalid)
                {
                    case "uninitialized":mod.initialized=false;break;case "menu":mod.scene="Menu_Title";break;case "nohero":HeroController.SilentInstance=null;break;
                    case "dead":HeroController.SilentInstance.cState.dead=true;break;case "hazard":HeroController.SilentInstance.cState.hazardDeath=true;break;case "nostate":HeroController.SilentInstance.cState=null;break;
                    case "nogm":GameManager.instance=null;break;case "nongameplay":GameManager.instance.gameplay=false;break;case "exiting":GameManager.instance.gameState=GlobalEnums.GameState.EXITING_LEVEL;break;
                    case "cutscene":GameManager.instance.gameState=GlobalEnums.GameState.CUTSCENE;break;
                    case "nocameras":GameCameras.instance=null;break;case "nocontroller":GameCameras.instance.cameraController=null;break;
                    case "disabled":controller.isActiveAndEnabled=false;break;case "disabledcamera":controller.cam.isActiveAndEnabled=false;break;case "nocamera":controller.cam=null;break;
                    case "rtt":controller.cam.targetTexture=new object();break;case "differenttransform":controller.cam.transform=new Transform();break;
                    case "panning":controller.mode=CameraController.CameraMode.PANNING;break;case "fade":controller.mode=CameraController.CameraMode.FADEOUT;break;case "frozen":controller.mode=CameraController.CameraMode.FROZEN;break;
                }
                Check(mod.GetPinnableCamera()==null,"eligibility rejects "+invalid);mod.ToggleCameraPin();Check(!mod.cameraPin.Active,"pin disabled for "+invalid);
            }
            foreach(string invalid in new[]{"scene","death","hazard","replacement","destroyed","transition","cutscene"})
            {
                controller=Setup();mod=new ShaderStripMod();mod.ToggleCameraPin();
                switch(invalid)
                {case "scene":mod.scene="RoomB";break;case "death":HeroController.SilentInstance.cState.dead=true;break;case "hazard":HeroController.SilentInstance.cState.hazardDeath=true;break;case "replacement":GameCameras.instance.cameraController=new CameraController();break;case "destroyed":controller.destroyed=true;controller.transform.destroyed=true;break;case "transition":controller.mode=CameraController.CameraMode.PANNING;break;case "cutscene":GameManager.instance.gameState=GlobalEnums.GameState.CUTSCENE;break;}
                mod.UpdateCameraPin();Check(!mod.cameraPin.Active,"auto release after "+invalid);
            }
            controller=Setup();mod=new ShaderStripMod();mod.ToggleCameraPin();
            mod.OnPinnedCameraLateUpdate(c=>{c.mode=CameraController.CameraMode.PANNING;c.transform.position=Vector3.zero;},controller);
            Check(!mod.cameraPin.Active&&controller.mode==CameraController.CameraMode.PANNING&&Same(controller.transform.position,Vector3.zero),"native transition inside update must retain mode and position");
            controller=Setup();mod=new ShaderStripMod();mod.ToggleCameraPin();mod.OnPinnedCameraLateUpdate(c=>{mod.ReleaseCameraPin();c.transform.position=Vector3.zero;},controller);
            Check(!mod.cameraPin.Active&&Same(controller.transform.position,Vector3.zero)&&controller.mode==CameraController.CameraMode.FOLLOWING,"release inside callback does not teleport");
            controller=Setup();mod=new ShaderStripMod();Hook.fail=true;mod.ToggleCameraPin();
            Check(!mod.cameraPin.Active&&mod.cameraPinUpdateHook==null&&controller.mode==CameraController.CameraMode.FOLLOWING,"failed hook leaves native camera intact");
            Setup();mod=new ShaderStripMod();GameManager.instance.gameState=GlobalEnums.GameState.PAUSED;mod.ToggleCameraPin();Check(mod.cameraPin.Active,"pin available while game paused for F9");
            foreach(float width in new float[]{120,148,190,276,350,430,568,900})foreach(int count in new[]{3,4})
            {
                var cells=new RuleControlsLayout.Cell[count];
                for(int i=0;i<count;i++)
                {
                    cells[i]=RuleControlsLayout.Button(width,count,i);var cell=cells[i];
                    Check(cell.X>=0&&cell.X+cell.Width<=width+.001f&&cell.Width>0,"Rules button stays in panel");
                    Check(cell.Y>=0&&cell.Y+cell.Height<=RuleControlsLayout.Height(width,count)+.001f,"Rules content height includes every button");
                    for(int j=0;j<i;j++){var prev=cells[j];Check(cell.X>=prev.X+prev.Width||prev.X>=cell.X+cell.Width||cell.Y>=prev.Y+prev.Height||prev.Y>=cell.Y+cell.Height,"Rules controls do not overlap");}
                }
            }
            Console.WriteLine("PASS: "+checks+" camera pin / Rules layout assertions (production callbacks; Unity/game API substitutes).");
        }
    }
}
