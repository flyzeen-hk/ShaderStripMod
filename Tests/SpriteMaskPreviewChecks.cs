using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
    public struct Rect { public float x,y,width,height;public bool Contains(Vector2 p) { return p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height; } public Rect(float x,float y,float w,float h) { this.x=x;this.y=y;width=w;height=h; } }
    public struct Color { public float r,g,b,a;public Color(float r,float g,float b,float a=1) { this.r=r;this.g=g;this.b=b;this.a=a; } }
    public enum CameraClearFlags { Skybox,SolidColor,Depth }
    public sealed class Camera { public tk2dCamera testTk2d;public T GetComponent<T>() where T:class {return testTk2d as T;} public static Camera main;public static Camera[] allCameras=new Camera[0];public float depth;public int cullingMask;public Rect rect;public object targetTexture;public CameraClearFlags clearFlags;public Color backgroundColor; }
}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        void StoreCameraState(UnityEngine.Camera camera) { if(!strippedMasks.ContainsKey(camera)) strippedMasks.Add(camera,camera.cullingMask); }
        bool visualsActive;UnityEngine.Color stripBackground=new UnityEngine.Color(0,0,1);
        sealed class FakeRenderer { internal bool Hidden,Alive=true; }
        static void RunMaskPreviewChecks()
        {
            int size=16;var atlas=new SpriteMaskPreview.Pixel[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++) atlas[y*size+x]=new SpriteMaskPreview.Pixel((byte)(x*16),(byte)(y*16),80,(byte)(x<size/2 ? 255:128));
            var vertices=new[]{new SpriteMaskGeometry.Vertex(-2,-3,100,0,0),new SpriteMaskGeometry.Vertex(2,-3,100,1,0),new SpriteMaskGeometry.Vertex(2,3,100,1,1),new SpriteMaskGeometry.Vertex(-2,3,100,0,1)};
            var triangles=new[]{0,1,2,0,2,3};
            var result=SpriteMaskPreview.Rasterize(vertices,triangles,atlas,size,size,size,size,-2,-3,4,6);
            for(int i=0;i<result.Length;i++) Check(result[i].R==atlas[i].R&&result[i].G==atlas[i].G&&result[i].A==atlas[i].A,"Preview reads exact artwork independent of shader, GL, camera, or z offset");
            var reverse=SpriteMaskPreview.Rasterize(vertices,new[]{2,1,0,3,2,0},atlas,size,size,size,size,-2,-3,4,6);
            for(int i=0;i<reverse.Length;i++) Check(reverse[i].R==result[i].R&&reverse[i].A==result[i].A,"Preview accepts reversed sprite winding");
            var rotated=vertices.Select(v=>new SpriteMaskGeometry.Vertex(v.X,v.Y,v.Z,v.V,1-v.U)).ToArray();
            var rotation=SpriteMaskPreview.Rasterize(rotated,triangles,atlas,size,size,size,size,-2,-3,4,6);
            for(int y=0;y<size;y++) for(int x=0;x<size;x++) Check(rotation[y*size+x].R==atlas[(size-1-x)*size+y].R&&rotation[y*size+x].G==atlas[(size-1-x)*size+y].G,"Rotated atlas UV maps to correct source pixels");
            var selected=SpriteMaskPreview.ApplySelection(result,size,size,Rectangle(.25,.25,.75,.75));
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                Check(selected[y*size+x].A==(x>=4&&x<12&&y>=4&&y<12 ? result[y*size+x].A:0),"Result preview shows exactly the kept rectangle");
            var concave=new List<SpriteMaskGeometry.Point>{P(0,0),P(1,0),P(1,.5),P(.5,.5),P(.5,1),P(0,1)};
            var cut=SpriteMaskPreview.ApplySelection(result,size,size,concave);
            for(int y=0;y<size;y++) for(int x=0;x<size;x++) Check(cut[y*size+x].A==(x>=8&&y>=8 ? 0:result[y*size+x].A),"Concave result preview removes unwanted artwork");
            Check(RejectMask(()=>SpriteMaskPreview.Rasterize(vertices,new[]{0,1,99},atlas,size,size,size,size,-2,-3,4,6)),"Preview rejects invalid mesh index");
            Check(RejectMask(()=>SpriteMaskPreview.ApplySelection(result,size,size,new[]{P(0,0),P(1,1),P(0,1),P(1,0)})),"Preview does not show a misleading crossed contour");
            var ownership=new RendererVisibilityOverride<FakeRenderer>();var native=new FakeRenderer();var alreadyHidden=new FakeRenderer { Hidden=true };
            Check(ownership.Hold(native,r=>r.Hidden,(r,state)=>r.Hidden=state)&&native.Hidden,"Suppress full native sprite while showing mask geometry");
            Check(ownership.Hold(native,r=>r.Hidden,(r,state)=>r.Hidden=state),"Repeated suppression remembers original state");
            Check(!ownership.Hold(alreadyHidden,r=>r.Hidden,(r,state)=>r.Hidden=state),"Respect a sprite hidden by the game or another mod");
            native.Hidden=false;ownership.Reassert(r=>r.Alive,(r,state)=>r.Hidden=state);
            Check(native.Hidden,"Prevent game from showing full sprite over mask between frames");
            ownership.ReleaseAll(r=>r.Alive,(r,state)=>r.Hidden=state);
            Check(!native.Hidden&&alreadyHidden.Hidden,"Restore each renderer's original state when mask is disabled");
            ownership.ReleaseAll(r=>r.Alive,(r,state)=>r.Hidden=state);Check(!native.Hidden,"Repeated restoration is safe");
            var dead=new FakeRenderer();ownership.Hold(dead,r=>r.Hidden,(r,state)=>r.Hidden=state);dead.Alive=false;
            ownership.ReleaseAll(r=>r.Alive,(r,state)=> { if(!r.Alive) throw new Exception("Destroyed renderer accessed");r.Hidden=state; });Check(true,"Destroyed renderers are not accessed during cleanup");
            var mod=new ShaderStripMod();var camera=new UnityEngine.Camera { rect=new UnityEngine.Rect(0,.002f,1,.996f),clearFlags=UnityEngine.CameraClearFlags.Depth };
            mod.EnforceBackgroundCameraFrame(camera);Check(camera.rect.y==.002f,"Disabled mod does not alter camera rectangle");
            mod.visualsActive=true;mod.EnforceBackgroundCameraFrame(camera);
            Check(camera.rect.x==0&&camera.rect.y==0&&camera.rect.width==1&&camera.rect.height==1,"Camera clears full screen including bottom F9 border pixels");
            Check(camera.clearFlags==UnityEngine.CameraClearFlags.SolidColor&&camera.backgroundColor.b==1,"Both solid and image modes have an opaque full-screen clear");
            camera.rect=new UnityEngine.Rect(0,.01f,1,.98f);mod.EnforceBackgroundCameraFrame(camera);
            Check(camera.rect.y==0&&camera.rect.height==1,"Reassert viewport after game updates letterboxing");
            var offscreen=new UnityEngine.Camera { targetTexture=new object(),rect=new UnityEngine.Rect(.1f,.2f,.4f,.5f) };
            mod.EnforceBackgroundCameraFrame(offscreen);Check(offscreen.rect.y==.2f,"Do not resize offscreen rendering targets");
            mod.RestoreBackgroundCameraFrames();Check(camera.rect.y==.002f&&camera.rect.height==.996f,"Restore original camera viewport when mod is disabled");
        }
    }
}

