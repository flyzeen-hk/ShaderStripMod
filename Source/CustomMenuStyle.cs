using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private void UpdateCustomMenuLogo()
        {
            if(string.IsNullOrEmpty(customMenuLogoFile)){ReleaseCustomMenuLogo();return;}
            if(loadedLogoName!=customMenuLogoFile)
            {
                ReleaseCustomMenuLogo();loadedLogoName=customMenuLogoFile;
                try
                {
                    string path=MenuMediaPolicy.Resolve(menuMediaDirectory,customMenuLogoFile);var info=new FileInfo(path);
                    if(!MenuMediaPolicy.IsImage(Path.GetExtension(path))||!info.Exists||info.Length==0||info.Length>64L*1024*1024)throw new IOException("Choose a PNG/JPG logo smaller than 64 MiB.");
                    var encoded=File.ReadAllBytes(path);MenuMediaPolicy.ValidateImageDimensions(encoded,Path.GetExtension(path));
                    customLogoTexture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    if(!customLogoTexture.LoadImage(encoded,true))throw new IOException("Cannot decode logo image.");
                    MenuMediaPolicy.ValidateDecodedImageDimensions(customLogoTexture.width,customLogoTexture.height);
                    customLogoTexture.wrapMode=TextureWrapMode.Clamp;customLogoTexture.filterMode=FilterMode.Bilinear;customLogoTexture.hideFlags=HideFlags.HideAndDontSave;
                }catch(Exception ex){customLogoMessage="Logo error: "+ex.Message;if(customLogoTexture!=null)UnityEngine.Object.Destroy(customLogoTexture);customLogoTexture=null;}
            }
            if(customLogoTexture==null)return;
            var ui=UIManager.instance;var renderer=ui!=null ? ui.gameTitle : null;
            if(renderer==null&&MenuStyles.Instance!=null&&MenuStyles.Instance.title!=null)renderer=MenuStyles.Instance.title.Title;
            if(renderer==null)return;
            if(customLogoRenderer!=renderer)
            {
                RestoreCustomLogoRenderer();customLogoRenderer=renderer;originalLogoSprite=renderer.sprite;
                if(customLogoSprite!=null)UnityEngine.Object.Destroy(customLogoSprite);customLogoSprite=null;
            }
            else if(renderer.sprite!=customLogoSprite&&renderer.sprite!=null)
            {
                originalLogoSprite=renderer.sprite;if(customLogoSprite!=null)UnityEngine.Object.Destroy(customLogoSprite);customLogoSprite=null;
            }
            if(originalLogoSprite==null)return;
            if(customLogoSprite==null)
            {
                var originalBounds=originalLogoSprite.bounds;var bounds=originalBounds.size;
                float ppu=Mathf.Max(customLogoTexture.width/Mathf.Max(.01f,bounds.x),customLogoTexture.height/Mathf.Max(.01f,bounds.y));
                var pivot=new Vector2(.5f-originalBounds.center.x*ppu/customLogoTexture.width,.5f-originalBounds.center.y*ppu/customLogoTexture.height);
                customLogoSprite=Sprite.Create(customLogoTexture,new Rect(0,0,customLogoTexture.width,customLogoTexture.height),pivot,ppu,0,SpriteMeshType.FullRect);
                customLogoSprite.name="ShaderStripMod Custom Logo";customLogoSprite.hideFlags=HideFlags.HideAndDontSave;
            }
            renderer.sprite=customLogoSprite;
        }
        private void RestoreCustomLogoRenderer()
        {if(customLogoRenderer!=null&&customLogoRenderer.sprite==customLogoSprite)customLogoRenderer.sprite=originalLogoSprite;customLogoRenderer=null;originalLogoSprite=null;}
        private void ReleaseCustomMenuLogo()
        {
            RestoreCustomLogoRenderer();if(customLogoSprite!=null)UnityEngine.Object.Destroy(customLogoSprite);if(customLogoTexture!=null)UnityEngine.Object.Destroy(customLogoTexture);
            customLogoSprite=null;customLogoTexture=null;loadedLogoName=null;customLogoMessage="";
        }
        private static readonly string[] MenuTextColorNames={"Original","White","Gold","Cyan","Red","Green","Pink"};
        private static string NormalizeMenuTextColor(string color)
        {foreach(var name in MenuTextColorNames)if(string.Equals(name,color,StringComparison.OrdinalIgnoreCase))return name;return "Original";}
        private Color MenuTextTint()
        {
            switch(customMenuTextColor){case "Gold":return new Color(1,.8f,.35f);case "Cyan":return new Color(.3f,.9f,1);case "Red":return new Color(1,.35f,.35f);case "Green":return new Color(.35f,1,.5f);case "Pink":return new Color(1,.5f,.85f);default:return Color.white;}
        }
        private bool IsMenuButtonText(Component component)
        {
            if(component==null)return false;var ui=UIManager.instance;if(ui==null||ui.mainMenuScreen==null)return false;
            var root=ui.mainMenuScreen.transform;bool inRoot=false,hasButton=false;
            for(var p=component.transform;p!=null;p=p.parent)
            {
                if(p.GetComponent<Selectable>()!=null||p.GetComponent("tk2dUIItem")!=null)hasButton=true;
                if(p==root){inRoot=true;break;}
            }
            return inRoot&&hasButton;
        }
        private void UpdateCustomMenuTextStyle()
        {
            if(customMenuTextColor=="Original"&&Mathf.Abs(customMenuTextScale-1)<.001f){ReleaseCustomMenuTextStyle();return;}
            if(Time.unscaledTime>=nextMenuStyleScan)
            {
                nextMenuStyleScan=Time.unscaledTime+.5f;menuStyledTexts.Clear();var ui=UIManager.instance;
                if(ui!=null&&ui.mainMenuScreen!=null)
                {
                    foreach(var component in ui.mainMenuScreen.GetComponentsInChildren<Component>(true))
                        if(component!=null&&(component is Text||component is tk2dTextMesh||IsTmpText(component.GetType()))&&IsMenuButtonText(component))menuStyledTexts.Add(component);
                }
            }
            Color tint=MenuTextTint();bool resize=Mathf.Abs(customMenuTextScale-1)>.001f;
            menuTextColors.ReleaseWhere(c=>!menuStyledTexts.Contains(c),c=>c!=null,ReadMenuTextColor,WriteMenuTextColor,ReportMenuStyleError);
            menuTextSizes.ReleaseWhere(c=>!menuStyledTexts.Contains(c)||!resize,c=>c!=null,ReadMenuTextSize,WriteMenuTextSize,ReportMenuStyleError);
            menuTextScales.ReleaseWhere(c=>!menuStyledTexts.Contains(c)||!resize,c=>c!=null,c=>c.scale,(c,v)=>{c.scale=v;c.Commit();},ReportMenuStyleError);
            menuTextGradientColors.ReleaseWhere(c=>c==null||!menuStyledTexts.Contains(c)||!c.useGradient,c=>c!=null,c=>c.color2,(c,v)=>{c.color2=v;c.Commit();},ReportMenuStyleError);
            foreach(var component in menuStyledTexts)
            {
                if(component==null)continue;
                try
                {
                    menuTextColors.Apply(component,ReadMenuTextColor,WriteMenuTextColor,color=>new Color(color.r*tint.r,color.g*tint.g,color.b*tint.b,color.a),ObserveMenuTextColor);
                    var tk=component as tk2dTextMesh;
                    if(tk!=null)
                    {
                        if(tk.useGradient)menuTextGradientColors.Apply(tk,c=>c.color2,(c,v)=>{c.color2=v;c.Commit();},color=>new Color(color.r*tint.r,color.g*tint.g,color.b*tint.b,color.a),ObserveMenuTextColor);
                        if(resize)menuTextScales.Apply(tk,c=>c.scale,(c,v)=>{c.scale=v;c.Commit();},v=>new Vector3(v.x*customMenuTextScale,v.y*customMenuTextScale,v.z),ObserveMenuTextScale);
                    }
                    else if(resize)menuTextSizes.Apply(component,ReadMenuTextSize,WriteMenuTextSize,size=>Mathf.Max(1,size*customMenuTextScale));
                }catch(Exception ex){customMenuMessage="Button style error: "+ex.Message;}
            }
        }
        private static Color ObserveMenuTextColor(Color original,Color current,Color applied)
        {
            // Alpha animation changes only transparency; the already tinted RGB still belongs to this mod.
            return current.r==applied.r&&current.g==applied.g&&current.b==applied.b ? new Color(original.r,original.g,original.b,current.a) : current;
        }
        private static Vector3 ObserveMenuTextScale(Vector3 original,Vector3 current,Vector3 applied)
        {return current.x==applied.x&&current.y==applied.y ? new Vector3(original.x,original.y,current.z) : current;}
        private void ReportMenuStyleError(Exception ex){customMenuMessage="Button style error: "+ex.Message;}
        private Color ReadMenuTextColor(Component component)
        {
            var text=component as Text;if(text!=null)return text.color;var tk=component as tk2dTextMesh;if(tk!=null)return tk.color;
            var value=ReadMenuProperty(component,"color");return value is Color ? (Color)value : Color.white;
        }
        private void WriteMenuTextColor(Component component,Color color)
        {
            var text=component as Text;if(text!=null){text.color=color;return;}var tk=component as tk2dTextMesh;if(tk!=null){tk.color=color;tk.Commit();return;}
            WriteMenuProperty(component,"color",color);
        }
        private float ReadMenuTextSize(Component component)
        {var text=component as Text;if(text!=null)return text.fontSize;var value=ReadMenuProperty(component,"fontSize");return value!=null ? Convert.ToSingle(value) : 24;}
        private void WriteMenuTextSize(Component component,float size)
        {var text=component as Text;if(text!=null){text.fontSize=Mathf.RoundToInt(size);return;}WriteMenuProperty(component,"fontSize",size);}
        private static void WriteMenuProperty(object source,string name,object value)
        {var property=source.GetType().GetProperty(name,BindingFlags.Instance|BindingFlags.Public);if(property!=null&&property.CanWrite)property.SetValue(source,property.PropertyType.IsInstanceOfType(value) ? value : Convert.ChangeType(value,property.PropertyType),null);}
        private void ReleaseCustomMenuTextStyle()
        {
            menuTextColors.Release(c=>c!=null,ReadMenuTextColor,WriteMenuTextColor,ReportMenuStyleError);menuTextSizes.Release(c=>c!=null,ReadMenuTextSize,WriteMenuTextSize,ReportMenuStyleError);
            menuTextScales.Release(c=>c!=null,c=>c.scale,(c,v)=>{c.scale=v;c.Commit();},ReportMenuStyleError);
            menuTextGradientColors.Release(c=>c!=null,c=>c.color2,(c,v)=>{c.color2=v;c.Commit();},ReportMenuStyleError);
            menuStyledTexts.Clear();nextMenuStyleScan=0;
        }
    }
}
