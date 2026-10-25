using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name;public bool valid;public bool IsValid(){return valid;} }
}
namespace UnityEngine
{
    public class Object { static int next;int id=++next;public string name="";public int GetInstanceID(){return id;} }
    public struct Vector2 { public float x,y;public static Vector2 zero=>new Vector2(); }
    public struct Vector3 { public float x,y,z; }
    public class Texture:Object { }
    public class Sprite:Object { public Texture texture; }
    public class Material:Object { public Texture mainTexture; }
    public class Font:Object { public Material material; }
    public class GameObject:Object
    {
        public int layer;public bool activeInHierarchy=true;
        public SceneManagement.Scene scene=new SceneManagement.Scene {name="DontDestroyOnLoad",valid=true};
        public Transform transform;
        public readonly List<Component> components=new List<Component>();
        public GameObject(string name) { this.name=name;transform=new Transform {gameObject=this}; }
        public T GetComponent<T>() where T:Component { return components.OfType<T>().FirstOrDefault(); }
    }
    public class Component:Object
    {
        public GameObject gameObject;
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:Component {return gameObject.GetComponent<T>();}
        public T GetComponentInParent<T>() where T:Component
        {
            for(var p=transform;p!=null;p=p.parent)
                if(p.gameObject.activeInHierarchy){var c=p.GetComponent<T>();if(c!=null)return c;}
            return null;
        }
    }
    public class Transform:Component { public Transform parent;public Vector3 position; }
    public class MonoBehaviour:Component { }
    public class Canvas:Component { }
    public class Renderer:Component { public Material[] sharedMaterials=new Material[0];public Material sharedMaterial=>sharedMaterials.FirstOrDefault(); }
    public class SpriteRenderer:Renderer { public Sprite sprite; }
    public class TextMesh:Component { public string text;public Font font; }
    public static class LayerMask { public static string LayerToName(int layer){return layer==27?"uGUI":layer==5?"UI":"Default";} }
    public static class Resources
    {
        public static readonly List<Object> Loaded=new List<Object>();
        public static T[] FindObjectsOfTypeAll<T>() where T:Object {return Loaded.OfType<T>().ToArray();}
    }
}
namespace UnityEngine.UI
{
    public class Graphic:UnityEngine.MonoBehaviour
    {
        public UnityEngine.Texture mainTexture;
        public UnityEngine.Material materialForRendering;
    }
    public class Image:Graphic { public UnityEngine.Sprite sprite,overrideSprite; }
    public class Text:Graphic { public string text;public UnityEngine.Font font; }
    public class RawImage:Graphic { }
}
namespace TMPro
{
    public class TMP_FontAsset:UnityEngine.Object { public UnityEngine.Texture[] atlasTextures {get;set;} }
    public class TMP_Text:UnityEngine.UI.Graphic
    {
        public string text {get;set;}
        public TMP_FontAsset font {get;set;}
        public UnityEngine.Material fontSharedMaterial {get;set;}
    }
    public class TextMeshProUGUI:TMP_Text { }
    public class TextMeshPro:TMP_Text { }
}
public sealed class UIManager { public static UIManager instance;public UnityEngine.Canvas UICanvas;public UnityEngine.Transform transform;public UnityEngine.SpriteRenderer gameTitle; }
public sealed class tk2dFontData:UnityEngine.Object {public UnityEngine.Material material;}
public sealed class tk2dTextMesh:UnityEngine.MonoBehaviour {public tk2dFontData font;public string text;}
public sealed class tk2dSpriteDefinition {public string name;public UnityEngine.Material material,materialInst;}
public sealed class tk2dBaseSprite:UnityEngine.MonoBehaviour {public tk2dSpriteDefinition definition;public tk2dSpriteDefinition GetCurrentSpriteDef(){return definition;}}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private bool textureBrowserEntityMode,textureBrowserNearbyMode,textureBrowserAtlasMode,textureBrowserPreserveMode,textureBrowserProjectileMode,textureBrowserMenuMode;
        private bool textureBrowserNeedsRefresh=true;
        private UnityEngine.Vector2 textureBrowserListScroll,textureBrowserDetailScroll;
        private int selectedTextureBrowserIndex=-1,releaseCount,resetCount,previewReleaseCount;
        private string textureBrowserSearch="",activeScene="Menu_Title";
        private string GetActiveSceneName(){return activeScene;}
        private bool IsMenuScene(string s){return s.StartsWith("Menu",StringComparison.OrdinalIgnoreCase);}
        private void ReleaseMenuObjectPreview(){previewReleaseCount++;}
        private readonly List<TextureBrowserEntry> textureBrowserEntries=new List<TextureBrowserEntry>();
        private sealed class TextureBrowserEntry
        {
            internal string Kind,Name,Details,SceneName,ObjectPath,Layer,RendererType;
            internal UnityEngine.Texture Texture;internal UnityEngine.Sprite Sprite;internal UnityEngine.Material Material;
            internal UnityEngine.Renderer Renderer;internal bool MenuResource;internal UnityEngine.Vector3 Position;
        }
        private void ReleaseMaskEditor(){releaseCount++;}
        private void ResetTextureBrowserPreviewView(){resetCount++;}
        private void Log(string text){ }
        private bool IsProxyRenderer(UnityEngine.Renderer renderer){return renderer.gameObject.name=="Proxy";}
        private UnityEngine.Texture GetMaterialMainTexture(UnityEngine.Material material){return material?.mainTexture;}
        private string GetObjectName(UnityEngine.Object obj){return obj?.name??"";}
        private string GetObjectPath(UnityEngine.GameObject obj){return obj.transform.parent==null?obj.name:GetObjectPath(obj.transform.parent.gameObject)+"/"+obj.name;}
        private string GetLayerLabel(UnityEngine.GameObject obj){return UnityEngine.LayerMask.LayerToName(obj.layer);}
        private int CompareTextureBrowserEntries(TextureBrowserEntry a,TextureBrowserEntry b){int c=string.CompareOrdinal(a.Kind,b.Kind);return c!=0?c:string.CompareOrdinal(a.Name,b.Name);}
        static int checks;
        static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;}
        static T Attach<T>(string name,UnityEngine.Transform parent=null,string scene="DontDestroyOnLoad",bool valid=true,bool active=true) where T:UnityEngine.Component,new()
        {
            var obj=new UnityEngine.GameObject(name){activeInHierarchy=active,scene=new UnityEngine.SceneManagement.Scene {name=scene,valid=valid}};
            obj.transform.parent=parent;
            var c=new T {gameObject=obj};obj.components.Add(c);UnityEngine.Resources.Loaded.Add(c);return c;
        }
        static void Main()
        {
            var root=Attach<UnityEngine.Canvas>("UI",null,"DontDestroyOnLoad",true,false);UIManager.instance=new UIManager {UICanvas=root};
            var logoTexture=new UnityEngine.Texture {name="TitleAtlas"};var logoSprite=new UnityEngine.Sprite {name="HollowKnightLogo",texture=logoTexture};
            var logo=Attach<UnityEngine.UI.Image>("Logo",root.transform);logo.sprite=logoSprite;logo.mainTexture=logoTexture;
            var fontTexture=new UnityEngine.Texture {name="TrajanGlyphAtlas"};var fontMaterial=new UnityEngine.Material {name="TrajanMaterial",mainTexture=fontTexture};
            var legacy=Attach<UnityEngine.UI.Text>("StartButton",root.transform,active:false);legacy.text="Начать игру";legacy.font=new UnityEngine.Font {name="Trajan",material=fontMaterial};legacy.mainTexture=fontTexture;legacy.materialForRendering=fontMaterial;
            var raw=Attach<UnityEngine.UI.RawImage>("CreditsLogo",scene:"Menu_Title");raw.mainTexture=new UnityEngine.Texture {name="TeamCherry"};
            var mesh=Attach<UnityEngine.TextMesh>("LegacyTitle",root.transform);mesh.text="Options";mesh.font=legacy.font;
            var meshRenderer=new UnityEngine.Renderer {gameObject=mesh.gameObject,sharedMaterials=new[]{fontMaterial}};mesh.gameObject.components.Add(meshRenderer);UnityEngine.Resources.Loaded.Add(meshRenderer);
            var fontPage2=new UnityEngine.Texture {name="TMPPage2"};var tmpFont=new TMPro.TMP_FontAsset {name="Perpetua",atlasTextures=new[]{fontTexture,fontPage2}};
            var tmp=Attach<TMPro.TextMeshProUGUI>("QuitButton",root.transform,active:false);tmp.text="Выход";tmp.font=tmpFont;tmp.fontSharedMaterial=fontMaterial;tmp.mainTexture=fontTexture;
            var meshTmp=Attach<TMPro.TextMeshPro>("Achievements",root.transform);meshTmp.text="Достижения";meshTmp.font=tmpFont;meshTmp.fontSharedMaterial=fontMaterial;
            var sr=Attach<UnityEngine.SpriteRenderer>("MenuDecoration",scene:"Menu_Title",active:false);sr.sprite=logoSprite;
            Attach<UnityEngine.SpriteRenderer>("RoomBench",scene:"Town").sprite=logoSprite;
            Attach<UnityEngine.UI.Text>("PrefabText",root.transform,scene:"",valid:false).text="Prefab";
            Attach<UnityEngine.SpriteRenderer>("Proxy",scene:"Menu_Title").sprite=logoSprite;
            var title=Attach<UnityEngine.SpriteRenderer>("LogoTitle",scene:"DontDestroyOnLoad");title.sprite=logoSprite;UIManager.instance.gameTitle=title;
            var uiRoot=Attach<UnityEngine.MonoBehaviour>("UIManagerRoot");UIManager.instance.transform=uiRoot.transform;
            var persistent=Attach<UnityEngine.SpriteRenderer>("MenuPersistentNoCanvas",scene:"DontDestroyOnLoad");persistent.gameObject.layer=27;persistent.sprite=logoSprite;
            var sibling=Attach<UnityEngine.SpriteRenderer>("TitleSibling",uiRoot.transform);sibling.sprite=logoSprite;
            var tkText=Attach<tk2dTextMesh>("TkStart",root.transform,active:false);tkText.text="НАЧАТЬ";tkText.font=new tk2dFontData {name="PerpetuaTk",material=fontMaterial};
            var tkRenderer=new UnityEngine.Renderer {gameObject=tkText.gameObject,sharedMaterials=new[]{fontMaterial}};tkText.gameObject.components.Add(tkRenderer);UnityEngine.Resources.Loaded.Add(tkRenderer);
            var tkSprite=Attach<tk2dBaseSprite>("TkLogo",scene:"Menu_Title");tkSprite.definition=new tk2dSpriteDefinition {name="MenuLogoSprite",materialInst=fontMaterial};
            var tkSpriteRenderer=new UnityEngine.Renderer {gameObject=tkSprite.gameObject,sharedMaterials=new[]{fontMaterial}};tkSprite.gameObject.components.Add(tkSpriteRenderer);UnityEngine.Resources.Loaded.Add(tkSpriteRenderer);
            var mod=new ShaderStripMod {textureBrowserNearbyMode=true,textureBrowserSearch="RestBench"};mod.RefreshMenuTextureBrowserEntries();
            var entries=mod.textureBrowserEntries;
            Check(mod.textureBrowserMenuMode&&!mod.textureBrowserNearbyMode&&!mod.textureBrowserEntityMode&&!mod.textureBrowserAtlasMode&&!mod.textureBrowserPreserveMode&&!mod.textureBrowserProjectileMode,"Menu mode clears every other mode");
            Check(!mod.textureBrowserNeedsRefresh&&mod.selectedTextureBrowserIndex==0&&mod.releaseCount==1&&mod.resetCount==1,"Refresh resets stale selection, mask and preview state");
            Check(mod.textureBrowserListScroll.x==0&&mod.textureBrowserDetailScroll.y==0,"Reset scroll positions");
            Check(entries.All(e=>e.MenuResource)&&entries.Count>0,"All entries are marked as menu resources");
            Check(entries.All(e=>!e.Name.Contains("RoomBench")&&!e.Name.Contains("PrefabText")&&!e.Name.StartsWith("Proxy")),"Reject room, invalid prefab and mod proxy objects");
            var label=entries.Single(e=>e.Kind=="Menu Text");
            Check(label.Texture==fontTexture&&label.Material==fontMaterial&&label.Details.Contains("active=False"),"Inactive persistent Text exposes actual font atlas and material");
            Check(MenuResourcePolicy.Matches("начать игру",label.Name,label.Kind,label.Details),"Find label by displayed Russian text");
            Check(MenuResourcePolicy.Matches("trajan",label.Name,label.Kind,label.Details),"Find font by name");
            Check(entries.Single(e=>e.Kind=="Menu Image").Sprite==logoSprite,"Image exports cropped logo sprite and retains full atlas reference");
            Check(entries.Single(e=>e.Kind=="Menu RawImage").Texture==raw.mainTexture,"Find raw UI logo textures");
            Check(entries.Count(e=>e.Name.StartsWith("QuitButton")&&e.Kind=="Menu TMP Text")==1&&!entries.Any(e=>e.Kind=="Menu TextMeshProUGUI"),"Do not duplicate TMP through Graphic and MonoBehaviour scans");
            Check(entries.Count(e=>e.Texture==fontPage2)==2,"Discover extra TMP atlas pages for both menu text components");
            Check(entries.Single(e=>e.Kind=="Menu TextMesh").Renderer==meshRenderer&&!entries.Any(e=>e.Kind.StartsWith("Menu Material")&&e.Renderer==meshRenderer),"Legacy TextMesh resource is not duplicated as material renderer");
            Check(entries.Any(e=>e.Kind=="Menu Sprite"&&e.Renderer==sr),"Include inactive menu decorations with real renderer identity");
            Check(label.ObjectPath=="UI/StartButton"&&label.Renderer==null,"UI component metadata has object path and is not mistaken for a world renderer");
            Check(mod.textureBrowserSearch==""&&mod.previewReleaseCount==1,"Opening Menu clears stale Nearby search and cached preview");
            Check(entries.Any(e=>e.Kind=="Menu Sprite"&&e.Renderer==title),"Discover real persistent LogoTitle outside Canvas and UI ancestry");
            Check(entries.Any(e=>e.Renderer==sibling),"Discover persistent renderer under UIManager root outside Canvas");
            Check(entries.Any(e=>e.Renderer==persistent),"Discover persistent uGUI sprite outside Canvas while title scene is active");
            Check(entries.Single(e=>e.Kind=="Menu tk2d Text").Details.Contains("text=НАЧАТЬ"),"tk2d text exposes rendered label and font");
            Check(entries.Single(e=>e.Kind=="Menu tk2d Sprite").Details.Contains("MenuLogoSprite"),"tk2d sprite resolves selected definition and instantiated material");
            Check(entries.Count(e=>e.Renderer==tkRenderer)==1&&entries.Count(e=>e.Renderer==tkSpriteRenderer)==1,"Do not duplicate tk2d entries as raw renderers");
            mod.activeScene="Town";Check(!mod.IsMenuResource(persistent),"UI layer alone cannot classify persistent gameplay renderer outside title scene");mod.activeScene="Menu_Title";
            int count=entries.Count;mod.RefreshMenuTextureBrowserEntries();Check(entries.Count==count,"Refreshing menu scan does not accumulate duplicate entries");
            UnityEngine.Resources.Loaded.Clear();mod.RefreshMenuTextureBrowserEntries();Check(entries.Count==0&&mod.selectedTextureBrowserIndex==-1,"Empty scene clears old resource references");
            Console.WriteLine("PASS: "+checks+" production menu resource discovery assertions (Unity API substitutes; not a live game render).");
        }
    }
}