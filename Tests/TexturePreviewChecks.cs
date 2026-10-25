using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero {get{return new Vector2();}}
    }
    public struct Rect
    {
        public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public float xMax {get{return x+width;}}public float yMax {get{return y+height;}}
    }
    public struct Color
    {
        public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}
    }
    public class Texture {public int width,height;}
    public class Texture2D:Texture {public static readonly Texture2D whiteTexture=new Texture2D {width=1,height=1};}
    public class Sprite
    {
        public Texture texture;public Rect rect,textureRect;
    }
    public enum ScaleMode {ScaleToFit}
    public static class Mathf
    {
        public static float Max(float a,float b){return Math.Max(a,b);}public static float Min(float a,float b){return Math.Min(a,b);}
        public static float Clamp(float a,float b,float c){return Math.Max(b,Math.Min(c,a));}
        public static int RoundToInt(float a){return (int)Math.Round(a);}public static int CeilToInt(float a){return (int)Math.Ceiling(a);}
    }
    public static class GUI
    {
        public struct Draw
        {
            public Rect Requested,Visible;public Texture Texture;public int GroupDepth;
        }
        public static Color color;public static readonly List<Draw> Draws=new List<Draw>();
        public static readonly Stack<Rect> Groups=new Stack<Rect>();public static bool ThrowDraw;
        public static string ClickButton;public static float? SliderValue;
        public static void BeginGroup(Rect rect){Groups.Push(rect);}
        public static void EndGroup(){Groups.Pop();}
        static void Record(Rect rect,Texture texture)
        {
            if(ThrowDraw)throw new InvalidOperationException("Simulated draw failure");
            Rect visible=rect;
            if(Groups.Count>0)
            {
                var clip=Groups.Peek();
                float x=Math.Max(0,rect.x),y=Math.Max(0,rect.y),endX=Math.Min(clip.width,rect.xMax),endY=Math.Min(clip.height,rect.yMax);
                visible=new Rect(clip.x+x,clip.y+y,Math.Max(0,endX-x),Math.Max(0,endY-y));
            }
            Draws.Add(new Draw {Requested=rect,Visible=visible,Texture=texture,GroupDepth=Groups.Count});
        }
        public static void DrawTexture(Rect rect,Texture texture){Record(rect,texture);}
        public static void DrawTexture(Rect rect,Texture texture,ScaleMode scale,bool alpha){Record(rect,texture);}
        public static void DrawTextureWithTexCoords(Rect rect,Texture texture,Rect coords,bool alpha){Record(rect,texture);}
        public static float HorizontalSlider(Rect rect,float value,float min,float max){return SliderValue ?? value;}
    }
}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        class TextureBrowserEntry {internal bool MenuResource;internal UnityEngine.Sprite Sprite;internal UnityEngine.Texture Texture;}
        bool menuShowAtlas,textureBrowserPreviewDragging,FailMenuPreview;float textureBrowserPreviewZoom=1;
        UnityEngine.Vector2 textureBrowserPreviewPan;UnityEngine.Texture2D Artwork;
        UnityEngine.Rect InputRect;int ReleasedPreviews;readonly List<string> Notes=new List<string>();
        void HandleTexturePreviewInput(UnityEngine.Rect rect){InputRect=rect;}
        void ReleaseMenuObjectPreview(){ReleasedPreviews++;}
        void CancelTexturePreviewPointer(){textureBrowserPreviewDragging=false;}
        UnityEngine.Texture2D GetMenuObjectPreview(TextureBrowserEntry entry)
        {if(FailMenuPreview)throw new InvalidOperationException("Simulated preview source failure");return Artwork;}
        bool UiButton(UnityEngine.Rect rect,string text)
        {if(UnityEngine.GUI.ClickButton==text){UnityEngine.GUI.ClickButton=null;return true;}return false;}
        void UiLabel(UnityEngine.Rect rect,string text){Notes.Add(text);}
        void UiBox(UnityEngine.Rect rect,string text){ }
        static int checks;
        static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
        static bool Near(float a,float b){return Math.Abs(a-b)<.001;}
        static void CheckClipped(UnityEngine.Rect viewport)
        {
            foreach(var draw in UnityEngine.GUI.Draws)
            {
                Check(draw.GroupDepth==1,"Every background and artwork draw is inside the clipping group");
                if(draw.Visible.width>0&&draw.Visible.height>0)
                    Check(draw.Visible.x>=viewport.x&&draw.Visible.y>=viewport.y&&draw.Visible.xMax<=viewport.xMax+.001f&&draw.Visible.yMax<=viewport.yMax+.001f,"Zoom or pan cannot cover controls outside the preview viewport");
                if(draw.Texture==UnityEngine.Texture2D.whiteTexture)
                    Check(draw.Requested.x>=0&&draw.Requested.y>=0&&draw.Requested.xMax<=viewport.width+.001f&&draw.Requested.yMax<=viewport.height+.001f,"Checkerboard's last row and column stay inside viewport");
            }
            Check(UnityEngine.GUI.Groups.Count==0,"Draw restores outer GUI group");
        }
        public static void Main()
        {
            var mod=new ShaderStripMod();var texture=new UnityEngine.Texture2D {width=1024,height=512};
            var outer=new UnityEngine.Rect(340,100,380,255);var viewport=new UnityEngine.Rect(350,110,360,235);
            foreach(float zoom in new[]{.25f,1,2,16})foreach(var pan in new[]{new UnityEngine.Vector2(),new UnityEngine.Vector2(150,-70),new UnityEngine.Vector2(-150,100),new UnityEngine.Vector2(5000,5000)})
            {
                mod.textureBrowserPreviewZoom=zoom;mod.textureBrowserPreviewPan=pan;UnityEngine.GUI.Draws.Clear();
                mod.DrawTexturePreview(new TextureBrowserEntry {Texture=texture},outer);
                Check(Near(mod.InputRect.x,viewport.x)&&Near(mod.InputRect.y,viewport.y),"Pointer coordinates are interpreted before entering the group");
                CheckClipped(viewport);
            }
            UnityEngine.GUI.Draws.Clear();mod.textureBrowserPreviewZoom=16;mod.textureBrowserPreviewPan=UnityEngine.Vector2.zero;
            mod.Artwork=new UnityEngine.Texture2D {width=100,height=100};
            mod.DrawTexturePreview(new TextureBrowserEntry {MenuResource=true,Texture=texture},outer);
            CheckClipped(viewport);Check(UnityEngine.GUI.Draws.Exists(d=>d.Texture==mod.Artwork),"Menu artwork uses the same clipped preview path");
            var sprite=new UnityEngine.Sprite {texture=texture,rect=new UnityEngine.Rect(0,0,80,40),textureRect=new UnityEngine.Rect(16,32,80,40)};
            UnityEngine.GUI.Draws.Clear();mod.DrawTexturePreview(new TextureBrowserEntry {Sprite=sprite},outer);CheckClipped(viewport);
            UnityEngine.GUI.Draws.Clear();mod.DrawTexturePreview(new TextureBrowserEntry(),outer);CheckClipped(viewport);Check(mod.Notes.Contains("No preview texture."),"Missing-source note remains inside clipped viewport");
            mod.FailMenuPreview=true;bool threw=false;
            try{mod.DrawTexturePreview(new TextureBrowserEntry {MenuResource=true},outer);}catch(InvalidOperationException){threw=true;}
            Check(threw&&UnityEngine.GUI.Groups.Count==0,"Preview exceptions still restore group through finally");mod.FailMenuPreview=false;
            UnityEngine.GUI.color=new UnityEngine.Color(.8f,.7f,.6f,.5f);
            UnityEngine.GUI.ThrowDraw=true;threw=false;
            try{mod.DrawTexturePreview(new TextureBrowserEntry {Texture=texture},outer);}catch(InvalidOperationException){threw=true;}
            Check(threw&&UnityEngine.GUI.Groups.Count==0,"Checkerboard exceptions cannot leak GUI clipping state");
            Check(UnityEngine.GUI.color.r==.8f&&UnityEngine.GUI.color.a==.5f,"Checkerboard exceptions restore original GUI color");UnityEngine.GUI.ThrowDraw=false;
            mod.menuShowAtlas=true;mod.textureBrowserPreviewZoom=8;mod.textureBrowserPreviewPan=new UnityEngine.Vector2(40,50);mod.textureBrowserPreviewDragging=true;
            UnityEngine.GUI.ClickButton="100%";mod.DrawTexturePreviewControls(new UnityEngine.Rect(0,0,400,24));
            Check(mod.menuShowAtlas&&mod.textureBrowserPreviewZoom==1&&mod.textureBrowserPreviewPan.x==0&&mod.textureBrowserPreviewPan.y==0&&!mod.textureBrowserPreviewDragging,"100 percent resets pan and zoom without unexpectedly changing atlas/object choice");
            int released=mod.ReleasedPreviews;mod.ResetTextureBrowserPreviewView();
            Check(!mod.menuShowAtlas&&mod.ReleasedPreviews==released+1,"Selecting a new resource releases old artwork and resets source choice");
            Console.WriteLine("PASS: "+checks+" actual production preview GUI assertions.");
        }
    }
}