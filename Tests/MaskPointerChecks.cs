using System;
using System.Collections.Generic;
using System.IO;
namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y) {this.x=x;this.y=y;}
        public static Vector2 zero {get {return new Vector2();}}
        public static Vector2 operator +(Vector2 a,Vector2 b) {return new Vector2(a.x+b.x,a.y+b.y);}
        public static Vector2 operator -(Vector2 a,Vector2 b) {return new Vector2(a.x-b.x,a.y-b.y);}
        public static Vector2 operator *(Vector2 a,float b) {return new Vector2(a.x*b,a.y*b);}
    }
    public static class Mathf
    {
        public static float Clamp01(float v) {return Clamp(v,0,1);}
        public static float Clamp(float v,float min,float max) {return Math.Max(min,Math.Min(max,v));}
        public static float Pow(float a,float b) {return (float)Math.Pow(a,b);}
    }
    public enum EventType { Layout,Repaint,MouseDown,MouseDrag,MouseUp,ScrollWheel,Used,Ignore }
    public enum FocusType { Passive }
    public sealed class Event
    {
        public static Event current;public EventType type,rawType;public int button;public Vector2 mousePosition,delta;
        public void Use() {type=EventType.Used;}
    }
    public static class GUI {public static bool enabled=true;}
    public static class GUIUtility
    {
        public static int hotControl,NextControl;
        public static int GetControlID(int hint,FocusType focus,Rect rect) {return NextControl++;}
    }
    public static class Application { public static bool isFocused=true; }
}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        bool textureBrowserPreviewDragging,maskEditorOptions,maskLibraryMode;
        float textureBrowserPreviewZoom=1;
        UnityEngine.Vector2 textureBrowserPreviewPan;
        bool maskEditorDragging,maskEditorPanning,maskEditorShowResult,maskEditorResultDirty;
        int maskEditorControl,browserGuiReleaseHotControl;
        UnityEngine.Vector2 maskEditorDragStart,maskEditorPan;
        float maskEditorZoom=1;
        string spriteMaskMessage;
        readonly List<SpriteMaskGeometry.Point> maskEditorPoints=new List<SpriteMaskGeometry.Point>();
        static readonly UnityEngine.Rect TestMaskCanvas=new UnityEngine.Rect(0,0,400,240),TestMaskImage=new UnityEngine.Rect(20,20,200,100);
        static void MaskPointerEvent(ShaderStripMod mod,UnityEngine.EventType type,int button,float x,float y,int candidateControl,bool consumed=false,float dx=0,float dy=0)
        {
            UnityEngine.GUIUtility.NextControl=candidateControl;
            UnityEngine.Event.current=new UnityEngine.Event {rawType=type,type=consumed ? UnityEngine.EventType.Used:type,button=button,mousePosition=new UnityEngine.Vector2(x,y),delta=new UnityEngine.Vector2(dx,dy)};
            mod.HandleMaskEditorMouse(TestMaskCanvas,TestMaskImage);
        }
        static void PreviewPointerEvent(ShaderStripMod mod,UnityEngine.EventType type,int button,float x,float y,int control,bool consumed=false,float dx=0,float dy=0)
        {
            UnityEngine.GUIUtility.NextControl=control;
            UnityEngine.Event.current=new UnityEngine.Event {rawType=type,type=consumed ? UnityEngine.EventType.Used:type,button=button,mousePosition=new UnityEngine.Vector2(x,y),delta=new UnityEngine.Vector2(dx,dy)};
            mod.HandleTexturePreviewInput(TestMaskCanvas);
        }
        static void RunTexturePreviewPointerChecks()
        {
            var mod=new ShaderStripMod();UnityEngine.GUIUtility.hotControl=0;UnityEngine.GUI.enabled=true;UnityEngine.Input.Mouse[0]=true;
            PreviewPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,3000);
            Check(mod.textureBrowserPreviewDragging&&UnityEngine.GUIUtility.hotControl==3000,"Preview pan captures its own IMGUI control");
            PreviewPointerEvent(mod,UnityEngine.EventType.MouseDrag,0,190,90,3100,false,10,5);
            Check(mod.textureBrowserPreviewControl==3000&&mod.textureBrowserPreviewPan.x==10&&mod.textureBrowserPreviewPan.y==5,"Preview drag moves image and retains original capture ID");
            PreviewPointerEvent(mod,UnityEngine.EventType.Repaint,0,190,90,3200);
            Check(mod.textureBrowserPreviewDragging,"Preview repaint while held keeps pointer capture");
            float zoom=mod.textureBrowserPreviewZoom;PreviewPointerEvent(mod,UnityEngine.EventType.ScrollWheel,0,190,90,3300,false,0,-1);
            Check(mod.textureBrowserPreviewZoom==zoom,"Preview wheel cannot transform image during captured pan");
            PreviewPointerEvent(mod,UnityEngine.EventType.MouseUp,1,190,90,3400);Check(mod.textureBrowserPreviewDragging,"Right release does not cancel left preview pan");
            UnityEngine.Input.Mouse[0]=false;PreviewPointerEvent(mod,UnityEngine.EventType.MouseUp,0,900,500,3500,true);
            Check(!mod.textureBrowserPreviewDragging&&UnityEngine.GUIUtility.hotControl==0,"Consumed raw release outside preview stops panning");
            var pan=mod.textureBrowserPreviewPan;PreviewPointerEvent(mod,UnityEngine.EventType.MouseDrag,0,40,30,3600,false,10,5);
            Check(mod.textureBrowserPreviewPan.x==pan.x&&mod.textureBrowserPreviewPan.y==pan.y,"Unrelated later mouse drag cannot move preview after release");
            UnityEngine.Input.Mouse[0]=true;PreviewPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,3700);
            UnityEngine.Input.Mouse[0]=false;PreviewPointerEvent(mod,UnityEngine.EventType.Repaint,0,40,30,3800);
            Check(!mod.textureBrowserPreviewDragging&&UnityEngine.GUIUtility.hotControl==0,"Missed preview MouseUp recovers after physical release");
            UnityEngine.Input.Mouse[0]=true;PreviewPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,3900);
            UnityEngine.GUIUtility.hotControl=777;PreviewPointerEvent(mod,UnityEngine.EventType.Repaint,0,40,30,4000);
            Check(!mod.textureBrowserPreviewDragging&&UnityEngine.GUIUtility.hotControl==777,"Lost preview capture never steals another widget's control");
            mod.ReleasePendingMaskEditorPointer();UnityEngine.GUIUtility.hotControl=0;
            PreviewPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,4100);UnityEngine.Application.isFocused=false;mod.CancelMaskPointerOnFocusLoss();
            Check(!mod.textureBrowserPreviewDragging&&mod.browserGuiReleaseHotControl==4100,"Focus loss queues preview capture release");
            mod.ReleasePendingMaskEditorPointer();Check(UnityEngine.GUIUtility.hotControl==0,"Pending focus release frees preview hotControl");UnityEngine.Application.isFocused=true;
            PreviewPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,4200);mod.CancelTexturePreviewPointer();mod.ReleasePendingMaskEditorPointer();
            Check(!mod.textureBrowserPreviewDragging&&UnityEngine.GUIUtility.hotControl==0,"Tab switch and close cancellation free preview pointer");
            UnityEngine.GUI.enabled=false;PreviewPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,4300);
            Check(!mod.textureBrowserPreviewDragging,"Disabled preview controls cannot take the pointer");UnityEngine.GUI.enabled=true;
            PreviewPointerEvent(mod,UnityEngine.EventType.MouseDown,0,900,500,4400);Check(!mod.textureBrowserPreviewDragging,"Clicks outside preview cannot begin panning");
            PreviewPointerEvent(mod,UnityEngine.EventType.ScrollWheel,0,40,30,4500,false,0,-1);
            Check(mod.textureBrowserPreviewZoom>zoom,"Uncaptured preview wheel zoom remains functional");
        }
        static void RunMaskPointerChecks()
        {
            var mod=new ShaderStripMod();UnityEngine.GUIUtility.hotControl=0;UnityEngine.GUI.enabled=true;UnityEngine.Input.Mouse[0]=true;
            MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,100);
            Check(mod.maskEditorDragging&&UnityEngine.GUIUtility.hotControl==100&&!mod.CanApplyMaskSelection,"Start captures mouse and Apply waits until release");
            MaskPointerEvent(mod,UnityEngine.EventType.MouseDrag,0,180,90,201);
            Check(mod.maskEditorControl==100&&mod.maskEditorPoints.Count==4&&!mod.CanApplyMaskSelection,"A changed IMGUI control allocation cannot replace captured ID");
            MaskPointerEvent(mod,UnityEngine.EventType.Repaint,0,180,90,305);
            Check(mod.maskEditorDragging&&mod.maskEditorControl==100,"Repaint while held preserves selection capture");
            UnityEngine.Input.Mouse[0]=false;MaskPointerEvent(mod,UnityEngine.EventType.MouseUp,0,190,100,410);
            Check(mod.CanApplyMaskSelection&&!mod.HasMaskEditorPointer&&UnityEngine.GUIUtility.hotControl==0,"Apply enabled after actual production MouseUp handler");
            Check(mod.spriteMaskMessage=="Selection ready. Click Apply and close.","Display explicit ready state");
            var bounds=SpriteMaskRectangle.GetBounds(mod.maskEditorPoints);
            Check(Math.Abs(bounds.X-.1)<1e-6&&Math.Abs(bounds.Width-.75)<1e-6&&Math.Abs(bounds.Y-.2)<1e-6&&Math.Abs(bounds.Height-.7)<1e-6,"MouseUp position is committed with correct image coordinate conversion");
            string scratch=Path.Combine(Path.GetTempPath(),"ShaderStripModGestureChecks_"+Guid.NewGuid().ToString("N")),path=Path.Combine(scratch,"selected.mask");
            try
            {
                var record=new SpriteMaskRecord {Identity="Town\nRestBench[0]\nSpriteRenderer#0",Label="Town / RestBench",Fingerprint=new string('A',64),Enabled=true};
                record.Points.AddRange(mod.maskEditorPoints);SpriteMaskRecord.WriteAtomic(path,record.Serialize());
                var saved=SpriteMaskRecord.Parse(File.ReadAllText(path));
                Check(saved.Points.Count==4&&saved.Enabled,"Actual drag result can be validated, written and loaded as a saved mask");
                Check(Math.Abs(SpriteMaskRectangle.GetBounds(saved.Points).Width-bounds.Width)<1e-12,"Save retains dragged coordinates");
            }
            finally {if(File.Exists(path))File.Delete(path);if(Directory.Exists(scratch))Directory.Delete(scratch,false);}
            mod.maskEditorShowResult=true;mod.maskEditorResultDirty=false;mod.ResetMaskSelection();
            Check(!mod.maskEditorShowResult&&mod.maskEditorResultDirty&&mod.maskEditorPoints.Count==0,"Clear from result view restores editable original instead of trapping empty preview");
            Check(!mod.CanApplyMaskSelection,"Empty selection remains rejected");
            mod.spriteMaskMessage="Mask removed.";mod.ResetMaskSelection(false);Check(mod.spriteMaskMessage=="Mask removed.","Clearing after removal preserves confirmation message");
            Check(!SpriteMaskRectangle.HasArea(SpriteMaskRectangle.FromCorners(0,0,.00002,.00002)),"Apply rejects rectangle too small for geometry triangulation");
            var tiny=SpriteMaskRectangle.FromCorners(0,0,.001,.001);Check(SpriteMaskRectangle.HasArea(tiny)&&SpriteMaskGeometry.Triangulate(tiny).Count==6,"Accepted small selection also passes real triangulation");
            UnityEngine.Input.Mouse[0]=true;MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,500);MaskPointerEvent(mod,UnityEngine.EventType.MouseDrag,0,190,90,600);
            // Simulate clipping/another IMGUI handler consuming the release outside the canvas.
            UnityEngine.Input.Mouse[0]=false;MaskPointerEvent(mod,UnityEngine.EventType.MouseUp,0,900,500,700,true);
            Check(mod.CanApplyMaskSelection&&UnityEngine.GUIUtility.hotControl==0,"Consumed raw MouseUp outside canvas still releases selection");
            bounds=SpriteMaskRectangle.GetBounds(mod.maskEditorPoints);Check(bounds.X+bounds.Width==1&&bounds.Y==0,"Selection clamped to image when release occurs outside");
            UnityEngine.Input.Mouse[0]=true;MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,800);MaskPointerEvent(mod,UnityEngine.EventType.MouseDrag,0,190,90,900);
            MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,1,190,90,901);Check(mod.maskEditorDragging&&!mod.maskEditorPanning&&mod.maskEditorControl==800,"Right button cannot replace active left selection");
            MaskPointerEvent(mod,UnityEngine.EventType.MouseUp,1,190,90,902);Check(mod.maskEditorDragging,"Wrong button release cannot finish left selection");
            float zoom=mod.maskEditorZoom;MaskPointerEvent(mod,UnityEngine.EventType.ScrollWheel,0,190,90,903,false,0,-1);Check(mod.maskEditorZoom==zoom,"Wheel cannot transform image midway through selection");
            UnityEngine.Input.Mouse[0]=false;MaskPointerEvent(mod,UnityEngine.EventType.Repaint,0,190,90,904);
            Check(mod.CanApplyMaskSelection&&!mod.HasMaskEditorPointer&&UnityEngine.GUIUtility.hotControl==0,"Recover missed MouseUp after physical release");
            UnityEngine.Input.Mouse[1]=true;MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,1,80,70,1000);MaskPointerEvent(mod,UnityEngine.EventType.MouseDrag,1,90,75,1100,false,10,5);
            Check(mod.maskEditorPanning&&mod.maskEditorControl==1000&&mod.maskEditorPan.x==10&&!mod.CanApplyMaskSelection,"Right pan uses original capture ID and does not leave Apply enabled during pan");
            UnityEngine.Input.Mouse[1]=false;MaskPointerEvent(mod,UnityEngine.EventType.MouseUp,1,90,75,1200,true);
            Check(!mod.maskEditorPanning&&mod.CanApplyMaskSelection&&UnityEngine.GUIUtility.hotControl==0,"Right pan release leaves saved selection applicable");
            UnityEngine.Input.Mouse[0]=true;MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,1300);MaskPointerEvent(mod,UnityEngine.EventType.MouseDrag,0,190,90,1400);
            UnityEngine.GUIUtility.hotControl=9999;MaskPointerEvent(mod,UnityEngine.EventType.Repaint,0,190,90,1401);
            Check(mod.CanApplyMaskSelection&&UnityEngine.GUIUtility.hotControl==9999,"Lost capture clears stale drag without stealing another control");
            mod.ReleasePendingMaskEditorPointer();Check(UnityEngine.GUIUtility.hotControl==9999,"Pending release cannot clear an unrelated button's capture");
            UnityEngine.GUIUtility.hotControl=0;MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,1500);MaskPointerEvent(mod,UnityEngine.EventType.MouseDrag,0,190,90,1600);
            mod.textureBrowserPreviewDragging=true;UnityEngine.Application.isFocused=false;mod.CancelMaskPointerOnFocusLoss();
            Check(!mod.textureBrowserPreviewDragging&&!mod.HasMaskEditorPointer&&mod.browserGuiReleaseHotControl==1500&&mod.CanApplyMaskSelection,"Focus loss cancels stuck drag and retains completed geometry");
            mod.ReleasePendingMaskEditorPointer();Check(UnityEngine.GUIUtility.hotControl==0&&mod.browserGuiReleaseHotControl==0,"Release hotControl on next OnGUI after focus loss or window close");UnityEngine.Application.isFocused=true;
            MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,1700);mod.CancelMaskEditorPointer();mod.ReleasePendingMaskEditorPointer();
            Check(!mod.HasMaskEditorPointer&&UnityEngine.GUIUtility.hotControl==0,"Tab/window switch releases editor control");
            MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,1800);UnityEngine.Input.Mouse[0]=false;MaskPointerEvent(mod,UnityEngine.EventType.MouseUp,0,40,30,1900);
            Check(!mod.CanApplyMaskSelection&&!mod.HasMaskEditorPointer&&UnityEngine.GUIUtility.hotControl==0,"Click without rectangle area does not trap input or enable invalid save");
            UnityEngine.GUI.enabled=false;MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,2000);Check(!mod.HasMaskEditorPointer,"Disabled GUI cannot start a selection");UnityEngine.GUI.enabled=true;
            mod.maskEditorShowResult=true;MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,40,30,2100);Check(!mod.HasMaskEditorPointer,"Result preview does not overwrite original selection");mod.ResetMaskSelection();
            mod.ResetMaskSelection();UnityEngine.GUIUtility.hotControl=0;UnityEngine.Input.Mouse[0]=true;
            MaskPointerEvent(mod,UnityEngine.EventType.MouseDown,0,5,5,2200);
            Check(mod.maskEditorDragging,"Selection can begin in gray margin outside artwork");
            MaskPointerEvent(mod,UnityEngine.EventType.MouseDrag,0,190,90,2300);
            UnityEngine.Input.Mouse[0]=false;MaskPointerEvent(mod,UnityEngine.EventType.MouseUp,0,190,90,2400);
            bounds=SpriteMaskRectangle.GetBounds(mod.maskEditorPoints);
            Check(mod.CanApplyMaskSelection&&bounds.X==0&&bounds.Y+bounds.Height==1,"Gray margin start clamps to artwork edge and releases Apply");
            mod.maskEditorOptions=true;mod.maskLibraryMode=true;
            Check(!mod.CanUseMaskImageControls&&!mod.CanApplyMaskSelection,"Saved-mask library cannot advertise image Apply, clear, fit or reload actions that its early return would discard");
            mod.maskEditorOptions=false;
            Check(mod.CanUseMaskImageControls&&mod.CanApplyMaskSelection,"Hiding library options restores image controls without discarding the existing selection");
            mod.maskEditorOptions=true;mod.maskLibraryMode=false;
            Check(mod.CanUseMaskImageControls&&mod.CanApplyMaskSelection,"Returning from library restores applicable image controls");
            RunTexturePreviewPointerChecks();
            UnityEngine.Input.Mouse[0]=false;UnityEngine.Input.Mouse[1]=false;UnityEngine.Event.current=null;
        }
    }
}
