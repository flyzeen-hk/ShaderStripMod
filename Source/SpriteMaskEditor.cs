using System;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private Renderer maskEditorRenderer,maskEditorAttemptedRenderer;
        private MaskSource maskEditorSource;
        private Texture2D maskEditorPreview,maskEditorResult;
        private SpriteMaskPreview.Pixel[] maskEditorPixels;
        private readonly List<SpriteMaskGeometry.Point> maskEditorPoints=new List<SpriteMaskGeometry.Point>();
        private bool maskEditorDragging,maskEditorPanning,maskEditorShowResult,maskEditorResultDirty,maskLibraryMode,maskEditorOptions;
        private Vector2 maskEditorDragStart,maskEditorPan,maskEditorScroll,maskEditorStatusScroll;
        private int maskEditorControl;
        private float maskEditorZoom=1,maskEditorContentHeight=400;
        private GUIStyle maskEditorWrappedStyle,maskEditorButtonStyle;
        private void ReleaseMaskEditor()
        {
            CancelMaskEditorPointer();
            if(maskEditorPreview!=null) Object.Destroy(maskEditorPreview);
            if(maskEditorResult!=null) Object.Destroy(maskEditorResult);
            maskEditorPreview=null;maskEditorResult=null;maskEditorPixels=null;maskEditorRenderer=null;maskEditorAttemptedRenderer=null;
            maskEditorSource=null;maskEditorPoints.Clear();maskEditorZoom=1;maskEditorPan=Vector2.zero;maskEditorShowResult=false;maskEditorStatusScroll=Vector2.zero;
        }
        private void BeginMaskEditor(Renderer renderer)
        {
            ReleaseMaskEditor();maskEditorAttemptedRenderer=renderer;
            try
            {
                var source=ReadMaskSource(renderer);var preview=RenderMaskPreview(source);
                maskEditorRenderer=renderer;maskEditorSource=source;maskEditorPreview=preview;
                var rule=GetSpriteMask(renderer);
                if(rule!=null&&rule.Fingerprint==source.Fingerprint) maskEditorPoints.AddRange(SpriteMaskRectangle.FromBounds(rule.Points));
                maskEditorResultDirty=true;
                spriteMaskMessage=rule==null ? "Drag a rectangle around the part to KEEP. The shaded area will be hidden. Preview the result, then Apply and close." : (rule.Fingerprint==source.Fingerprint ? "Saved mask loaded as a rectangle. Changes are saved only when you press Apply and close." : "Saved mask no longer matches this sprite. Select a new rectangle, then Apply and close.");
            }
            catch(Exception ex) { spriteMaskMessage="Cannot open image: "+ex.Message; }
        }
        private Texture2D RenderMaskPreview(MaskSource source,bool rememberPixels=true)
        {
            Texture2D readable=null;
            try
            {
                string note;readable=CopyTextureToTexture2D(source.Texture,out note);
                var colors=readable.GetPixels32();var pixels=new SpriteMaskPreview.Pixel[colors.Length];
                for(int i=0;i<colors.Length;i++) pixels[i]=new SpriteMaskPreview.Pixel(colors[i].r,colors[i].g,colors[i].b,colors[i].a);
                if(source.AlphaTexture!=null)
                {
                    Texture2D alpha=null;
                    try
                    {
                        alpha=CopyTextureToTexture2D(source.AlphaTexture,out note);
                        if(alpha.width!=readable.width||alpha.height!=readable.height) throw new ArgumentException("Separate alpha texture dimensions do not match.");
                        var values=alpha.GetPixels32();for(int i=0;i<pixels.Length;i++) pixels[i].A=values[i].r;
                    }
                    finally { if(alpha!=null) Object.Destroy(alpha); }
                }
                float aspect=source.Bounds.width/source.Bounds.height;
                int width=Mathf.Clamp(Mathf.RoundToInt(1024*Mathf.Min(1,aspect)),32,1024),height=Mathf.Clamp(Mathf.RoundToInt(1024*Mathf.Min(1,1/aspect)),32,1024);
                var image=SpriteMaskPreview.Rasterize(source.Vertices,source.Triangles,pixels,readable.width,readable.height,width,height,
                    source.Bounds.x,source.Bounds.y,source.Bounds.width,source.Bounds.height);
                bool visible=false;foreach(var pixel in image) if(pixel.A>0) { visible=true;break; }
                if(!visible) throw new ArgumentException("This renderer has no visible image. Select the decoration's Nearby Sprite entry.");
                var texture=MakeMaskPreviewTexture(image,width,height);if(rememberPixels)maskEditorPixels=image;return texture;
            }
            finally { if(readable!=null) Object.Destroy(readable); }
        }
        private Texture2D MakeMaskPreviewTexture(SpriteMaskPreview.Pixel[] pixels,int width,int height)
        {
            var colors=new Color32[pixels.Length];for(int i=0;i<pixels.Length;i++) colors[i]=new Color32(pixels[i].R,pixels[i].G,pixels[i].B,pixels[i].A);
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
            try { texture.SetPixels32(colors);texture.Apply(false,false);return texture; }
            catch { Object.Destroy(texture);throw; }
        }
        private void UpdateMaskResult()
        {
            maskEditorResultDirty=false;
            if(maskEditorResult!=null) Object.Destroy(maskEditorResult);maskEditorResult=null;
            if(maskEditorPreview==null||!SpriteMaskRectangle.HasArea(maskEditorPoints)) return;
            try
            {
                var pixels=SpriteMaskPreview.ApplySelection(maskEditorPixels,maskEditorPreview.width,maskEditorPreview.height,maskEditorPoints);
                maskEditorResult=MakeMaskPreviewTexture(pixels,maskEditorPreview.width,maskEditorPreview.height);
            }
            catch(Exception ex) { Log("Mask preview rejected: "+ex.Message);spriteMaskMessage="Select a rectangle with a nonzero width and height."; }
        }
        private void DrawSpriteMaskTab(TextureBrowserEntry entry,Rect content)
        {
            if(maskEditorWrappedStyle==null) maskEditorWrappedStyle=new GUIStyle(GUI.skin.label) { wordWrap=true };
            if(maskEditorButtonStyle==null) maskEditorButtonStyle=new GUIStyle(GUI.skin.button) { wordWrap=true };
            Renderer renderer=entry!=null && !entry.MenuResource ? entry.Renderer : null;
            if(renderer==null || maskEditorAttemptedRenderer!=renderer) ReleaseMaskEditor();
            UiBox(content,"");var inner=new Rect(content.x+8,content.y+8,Mathf.Max(1,content.width-16),Mathf.Max(1,content.height-16));
            float y=inner.y,fifth=(inner.width-24)/5;
            if(renderer!=null&&maskEditorAttemptedRenderer==null&&Event.current.type==EventType.Repaint) BeginMaskEditor(renderer);
            bool oldEnabled=GUI.enabled;GUI.enabled=oldEnabled&&CanApplyMaskSelection;
            bool apply=UiButton(new Rect(inner.x,y,fifth,30),"Apply and close",maskEditorButtonStyle);
            GUI.enabled=oldEnabled&&CanUseMaskImageControls&&(maskEditorShowResult||CanApplyMaskSelection);
            bool result=UiButton(new Rect(inner.x+fifth+6,y,fifth,30),maskEditorShowResult ? "Show original" : "Preview result",maskEditorButtonStyle);GUI.enabled=oldEnabled;
            if(result) { maskEditorShowResult=!maskEditorShowResult;maskEditorResultDirty=true; }
            GUI.enabled=oldEnabled&&CanUseMaskImageControls;
            if(UiButton(new Rect(inner.x+2*(fifth+6),y,fifth,30),"Clear selection",maskEditorButtonStyle)) ResetMaskSelection();
            if(UiButton(new Rect(inner.x+3*(fifth+6),y,fifth,30),"Fit image",maskEditorButtonStyle)) {maskEditorZoom=1;maskEditorPan=Vector2.zero;}
            GUI.enabled=oldEnabled;
            if(UiButton(new Rect(inner.x+4*(fifth+6),y,fifth,30),maskEditorOptions ? "Hide options" : "More options",maskEditorButtonStyle)) {CancelMaskEditorPointer();maskEditorOptions=!maskEditorOptions;}y+=36;
            var selected=renderer!=null ? GetSpriteMask(renderer) : null;bool remove=false;
            if(maskEditorOptions)
            {
                float third=(inner.width-12)/3;
                if(UiButton(new Rect(inner.x,y,third,28),maskLibraryMode ? "Back to image" : "Saved masks",maskEditorButtonStyle)) {CancelMaskEditorPointer();maskLibraryMode=!maskLibraryMode;}
                GUI.enabled=oldEnabled&&CanUseMaskImageControls&&renderer!=null;
                if(UiButton(new Rect(inner.x+third+6,y,third,28),"Reload image",maskEditorButtonStyle)) BeginMaskEditor(renderer);
                GUI.enabled=oldEnabled&&CanUseMaskImageControls&&selected!=null;remove=UiButton(new Rect(inner.x+2*(third+6),y,third,28),"Remove saved mask",maskEditorButtonStyle);GUI.enabled=oldEnabled;y+=34;
                DrawMaskEditorLabel(ref y,inner,"Left drag: keep the framed region. Wheel: zoom. Right drag: pan. Esc: close without saving.");
                if(maskLibraryMode) {DrawMaskLibrary(new Rect(inner.x,y,inner.width,inner.yMax-y));return;}
            }
            if(maskEditorSource==null||maskEditorPreview==null)
            {
                string message=entry!=null&&entry.MenuResource ? "Menu resources support Info, Preview and Export. Visibility rules and masks currently apply to room objects." : (renderer==null ? "1. Open Nearby.\n2. Select the object's Nearby Sprite entry.\n3. Open Mask to see its image." : spriteMaskMessage);
                UiLabel(new Rect(inner.x,y,inner.width,inner.yMax-y),message,maskEditorWrappedStyle);return;
            }
            var region=MaskWorkspaceLayout.Canvas(inner.x,y,inner.width,inner.yMax);
            var canvas=new Rect(region.X,region.Y,region.Width,region.Height);
            DrawMaskCanvas(canvas);
            var status=new Rect(inner.x,canvas.yMax+4,inner.width,26);
            float statusHeight=maskEditorWrappedStyle.CalcHeight(new GUIContent(UiText(spriteMaskMessage)),Mathf.Max(1,status.width-20));
            maskEditorStatusScroll=GUI.BeginScrollView(status,maskEditorStatusScroll,new Rect(0,0,Mathf.Max(1,status.width-20),Mathf.Max(status.height,statusHeight)));
            UiLabel(new Rect(0,0,Mathf.Max(1,status.width-20),Mathf.Max(status.height,statusHeight)),spriteMaskMessage,maskEditorWrappedStyle);GUI.EndScrollView();
            try
            {
                if(remove) { RemoveSpriteMask(selected);ResetMaskSelection(false); }
                if(apply)
                {
                    var live=ReadMaskSource(renderer);
                    if(live.Fingerprint!=maskEditorSource.Fingerprint) throw new ArgumentException("The image changed. Reload image and select the rectangle again.");
                    SaveSpriteMask(renderer,live,maskEditorPoints);SetTextureBrowserVisible(false);
                }
            }
            catch(Exception ex) { spriteMaskMessage="Cannot apply: "+ex.Message; }
        }
        private void DrawMaskEditorLabel(ref float y,Rect inner,string text)
        {
            text=UiText(text);float height=Mathf.Max(20,maskEditorWrappedStyle.CalcHeight(new GUIContent(text),inner.width));
            UiLabel(new Rect(inner.x,y,inner.width,height),text,maskEditorWrappedStyle);y+=height+4;
        }
        private void DrawMaskLibrary(Rect viewport)
        {
            float width=Mathf.Max(1,viewport.width-20),y=0;SpriteMaskRecord toggle=null,remove=null;
            maskEditorScroll=GUI.BeginScrollView(viewport,maskEditorScroll,new Rect(0,0,width,Mathf.Max(viewport.height,maskEditorContentHeight)));
            if(spriteMasks.Count==0) MaskLabel(ref y,width,"No saved masks yet.");
            var sorted=new List<SpriteMaskRecord>(spriteMasks.Values);sorted.Sort((a,b)=>string.CompareOrdinal(a.Label,b.Label));
            foreach(var rule in sorted)
            {
                MaskLabel(ref y,width,(rule.Enabled ? "Enabled: " : "Disabled: ")+rule.Label);
                float half=(width-6)/2;
                if(UiButton(new Rect(0,y,half,28),rule.Enabled ? "Disable" : "Enable",maskEditorButtonStyle)) toggle=rule;
                if(UiButton(new Rect(half+6,y,half,28),"Remove",maskEditorButtonStyle)) remove=rule;y+=36;
            }
            MaskLabel(ref y,width,spriteMaskMessage);maskEditorContentHeight=y;GUI.EndScrollView();
            try { if(toggle!=null) SetSpriteMaskEnabled(toggle,!toggle.Enabled);if(remove!=null) RemoveSpriteMask(remove); }
            catch(Exception ex) { spriteMaskMessage="Cannot change mask: "+ex.Message; }
        }
        private void MaskLabel(ref float y,float width,string text)
        {
            text=UiText(text??"").Replace("/","/\u200b").Replace("\\","\\\u200b");
            float height=Mathf.Max(22,maskEditorWrappedStyle.CalcHeight(new GUIContent(text),width));UiLabel(new Rect(0,y,width,height),text,maskEditorWrappedStyle);y+=height+6;
        }
        private void DrawMaskCanvas(Rect canvas)
        {
            UiBox(canvas,"");GUI.BeginGroup(canvas);
            try
            {
                var local=new Rect(0,0,canvas.width,canvas.height);DrawMaskCheckerboard(local);
                float aspect=maskEditorSource.Bounds.width/maskEditorSource.Bounds.height;
                float fitWidth=Mathf.Min(Mathf.Max(1,canvas.width-8),Mathf.Max(1,canvas.height-8)*aspect),fitHeight=fitWidth/aspect;
                var image=new Rect((canvas.width-fitWidth*maskEditorZoom)/2+maskEditorPan.x,(canvas.height-fitHeight*maskEditorZoom)/2+maskEditorPan.y,fitWidth*maskEditorZoom,fitHeight*maskEditorZoom);
                if(maskEditorShowResult&&maskEditorResultDirty&&Event.current.type==EventType.Repaint) UpdateMaskResult();
                var texture=maskEditorShowResult ? maskEditorResult : maskEditorPreview;
                if(texture!=null) GUI.DrawTexture(image,texture,ScaleMode.StretchToFill,true);
                DrawMaskSelection(image);
                HandleMaskEditorMouse(local,image);
            } finally { GUI.EndGroup(); }
        }
        private void DrawMaskCheckerboard(Rect rect)
        {
            Color old=GUI.color;
            try
            {
            for(float x=rect.x;x<rect.xMax;x+=16) for(float y=rect.y;y<rect.yMax;y+=16)
            { GUI.color=(((int)((x-rect.x)/16)+(int)((y-rect.y)/16))%2==0) ? new Color(.2f,.2f,.2f):new Color(.3f,.3f,.3f);GUI.DrawTexture(new Rect(x,y,Mathf.Min(16,rect.xMax-x),Mathf.Min(16,rect.yMax-y)),Texture2D.whiteTexture); }
            }
            finally {GUI.color=old;}
        }
        private void DrawMaskSelection(Rect rect)
        {
            if(Event.current.type!=EventType.Repaint||!SpriteMaskRectangle.HasArea(maskEditorPoints)) return;
            var b=SpriteMaskRectangle.GetBounds(maskEditorPoints);
            var selection=new Rect(rect.x+(float)b.X*rect.width,rect.y+(1-(float)b.Y-(float)b.Height)*rect.height,(float)b.Width*rect.width,(float)b.Height*rect.height);
            Color previous=GUI.color;
            try
            {
                GUI.color=new Color(0,0,0,.55f);
                GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width,Mathf.Max(0,selection.y-rect.y)),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x,selection.yMax,rect.width,Mathf.Max(0,rect.yMax-selection.yMax)),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x,selection.y,Mathf.Max(0,selection.x-rect.x),selection.height),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(selection.xMax,selection.y,Mathf.Max(0,rect.xMax-selection.xMax),selection.height),Texture2D.whiteTexture);
                // Axis-aligned strips stay in the GUI group's coordinates: no rotating GUI.matrix.
                foreach(var edge in SpriteMaskRectangle.Frame(selection.x,selection.y,selection.width,selection.height,4))
                { GUI.color=Color.black;GUI.DrawTexture(new Rect((float)edge.X,(float)edge.Y,(float)edge.Width,(float)edge.Height),Texture2D.whiteTexture); }
                foreach(var edge in SpriteMaskRectangle.Frame(selection.x+1,selection.y+1,Mathf.Max(0,selection.width-2),Mathf.Max(0,selection.height-2),2))
                { GUI.color=Color.yellow;GUI.DrawTexture(new Rect((float)edge.X,(float)edge.Y,(float)edge.Width,(float)edge.Height),Texture2D.whiteTexture); }
            }
            finally { GUI.color=previous; }
        }
    }
}