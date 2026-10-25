using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        public static bool operator ==(Object a,Object b) {bool an=ReferenceEquals(a,null)||a.destroyed,bn=ReferenceEquals(b,null)||b.destroyed;return an&&bn||!an&&!bn&&ReferenceEquals(a,b);}
        public static bool operator !=(Object a,Object b) {return !(a==b);}
        public override bool Equals(object value) {return ReferenceEquals(this,value);}
        public override int GetHashCode() {return base.GetHashCode();}
    }
    public struct Scene {public bool valid;public bool IsValid(){return valid;} }
    public class Component:Object {public GameObject gameObject;public Transform transform {get{return gameObject.transform;}} }
    public sealed class Transform:Object {public Transform parent;internal GameObject owner;}
    public class Behaviour:Component {bool value=true;public bool enabled {get{if(destroyed)throw new Exception("Destroyed enabled read");return value;}set{if(destroyed)throw new Exception("Destroyed enabled write");this.value=value;}} }
    public class Renderer:Behaviour {}
    public sealed class SpriteRenderer:Renderer {}
    public sealed class ParticleSystemRenderer:Renderer {}
    public sealed class Canvas:Behaviour {}
    public sealed class AudioSource:Behaviour {public bool playing=true;public float volume=.7f;}
    public sealed class MenuLogic:Behaviour {public int progress=19;}
    public sealed class ParticleSystem:Component {public bool playing=true;public int simulationSteps=33;}
    public sealed class Texture:Object {}
    public sealed class GameObject:Object
    {
        public bool active=true;public Scene scene=new Scene {valid=true};public Transform transform=new Transform();
        readonly List<Component> components=new List<Component>();readonly List<GameObject> children=new List<GameObject>();
        public GameObject(){transform.owner=this;}
        public T Add<T>() where T:Component,new(){var component=new T {gameObject=this};components.Add(component);return component;}
        public GameObject Child(){var child=new GameObject();child.transform.parent=transform;children.Add(child);return child;}
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component
        {var result=new List<T>();Collect(result,includeInactive);return result.ToArray();}
        void Collect<T>(List<T> result,bool includeInactive) where T:Component
        {if(!active&&!includeInactive)return;foreach(var component in components)if(component is T)result.Add((T)component);foreach(var child in children)child.Collect(result,includeInactive);}
    }
    public static class Time {public static float unscaledTime;}
}
public sealed class MenuStyleTitle:UnityEngine.Object {public UnityEngine.SpriteRenderer Title;}
public sealed class MenuStyles:UnityEngine.Object
{
    public sealed class MenuStyle {public UnityEngine.GameObject styleObject;}
    public static MenuStyles Instance;public MenuStyle[] styles;public int CurrentStyle;public UnityEngine.SpriteRenderer blackSolid;public MenuStyleTitle title;
}
public sealed class UIManager {public static UIManager instance;public UnityEngine.Canvas UICanvas;public UnityEngine.SpriteRenderer gameTitle;}
namespace ShaderStripMod
{
    internal sealed class MenuMediaPlayback {internal bool Failed;internal UnityEngine.Texture Texture;}
    public sealed partial class ShaderStripMod
    {
        bool visualsActive=true,customMenuEnabled=true,titleScene=true;
        MenuMediaPlayback menuPlayback=new MenuMediaPlayback {Texture=new UnityEngine.Texture()};
        bool IsCustomMenuActive(){return visualsActive&&customMenuEnabled&&titleScene;}
        static int checks;
        static void Check(bool condition,string label){if(!condition)throw new Exception(label);checks++;}
        static MenuStyles Owner(params UnityEngine.GameObject[] roots)
        {var styles=new MenuStyles {styles=new MenuStyles.MenuStyle[roots.Length]};for(int i=0;i<roots.Length;i++)styles.styles[i]=new MenuStyles.MenuStyle {styleObject=roots[i]};return styles;}
        public static void Main()
        {
            UnityEngine.Time.unscaledTime=1;
            var root=new UnityEngine.GameObject();var secondRoot=new UnityEngine.GameObject {active=false};
            var background=root.Add<UnityEngine.SpriteRenderer>();var particleRenderer=root.Child().Add<UnityEngine.ParticleSystemRenderer>();
            var particles=particleRenderer.gameObject.Add<UnityEngine.ParticleSystem>();var music=root.Add<UnityEngine.AudioSource>();var logic=root.Add<UnityEngine.MenuLogic>();
            var inactive=secondRoot.Add<UnityEngine.SpriteRenderer>();var originallyOff=root.Child().Add<UnityEngine.SpriteRenderer>();originallyOff.enabled=false;
            var logo=root.Child().Add<UnityEngine.SpriteRenderer>();var styleLogo=root.Child().Add<UnityEngine.SpriteRenderer>();
            var fade=root.Child().Add<UnityEngine.SpriteRenderer>();var canvas=root.Child().Add<UnityEngine.Canvas>();var button=canvas.gameObject.Child().Add<UnityEngine.SpriteRenderer>();
            var logoChild=logo.gameObject.Child().Add<UnityEngine.SpriteRenderer>();
            var unrelated=new UnityEngine.GameObject().Add<UnityEngine.SpriteRenderer>();
            var styles=Owner(root,secondRoot);styles.title=new MenuStyleTitle {Title=styleLogo};styles.blackSolid=fade;MenuStyles.Instance=styles;
            UIManager.instance=new UIManager {gameTitle=logo,UICanvas=canvas};
            var mod=new ShaderStripMod();mod.UpdateMenuBackgroundDecorations();
            Check(!background.enabled&&!particleRenderer.enabled&&!inactive.enabled,"Ready custom media hides only title style artwork, including inactive styles");
            Check(logo.enabled&&styleLogo.enabled&&logoChild.enabled&&fade.enabled&&button.enabled,"Logo, logo children, fade curtain and actual button Canvas survive");
            Check(unrelated.enabled,"Renderers outside registered style roots are untouched");
            Check(root.active&&!secondRoot.active&&music.enabled&&music.playing&&music.volume==.7f&&logic.enabled&&logic.progress==19,"Background suppression never disables GameObjects, audio or menu logic");
            Check(particles.playing&&particles.simulationSteps==33,"Particle simulation continues while only its renderer is suppressed");
            background.enabled=true;styles.CurrentStyle=1;mod.UpdateMenuBackgroundDecorations();
            Check(!background.enabled&&!inactive.enabled,"Game style switch and external reenable are reasserted immediately");
            mod.ReleaseMenuBackgroundDecorations();
            Check(background.enabled&&particleRenderer.enabled&&inactive.enabled&&!originallyOff.enabled,"Release restores each original renderer enabled value, including false");
            mod.ReleaseMenuBackgroundDecorations();Check(background.enabled&&!originallyOff.enabled,"Restoration is idempotent");
            mod.menuPlayback=null;mod.UpdateMenuBackgroundDecorations();Check(background.enabled&&mod.menuDecorationOriginalEnabled.Count==0,"Logo-only design retains standard menu artwork");
            mod.menuPlayback=new MenuMediaPlayback();mod.UpdateMenuBackgroundDecorations();Check(background.enabled&&mod.menuDecorationOriginalEnabled.Count==0,"Preparing media retains ordinary menu until Texture is ready");
            mod.menuPlayback.Texture=new UnityEngine.Texture();mod.menuPlayback.Failed=true;mod.UpdateMenuBackgroundDecorations();Check(background.enabled,"Failed media with stale Texture cannot hide ordinary menu");
            mod.menuPlayback.Failed=false;mod.UpdateMenuBackgroundDecorations();mod.menuPlayback.Failed=true;mod.UpdateMenuBackgroundDecorations();Check(background.enabled&&particleRenderer.enabled,"Playback failure restores standard menu artwork");
            mod.menuPlayback.Failed=false;mod.UpdateMenuBackgroundDecorations();mod.menuPlayback.Texture=null;mod.UpdateMenuBackgroundDecorations();Check(background.enabled,"Lost media Texture releases suppression");
            mod.menuPlayback.Texture=new UnityEngine.Texture();mod.UpdateMenuBackgroundDecorations();mod.titleScene=false;mod.UpdateMenuBackgroundDecorations();Check(background.enabled&&mod.menuDecorationOriginalEnabled.Count==0,"Leaving title scene restores decoration state");
            mod.titleScene=true;mod.UpdateMenuBackgroundDecorations();mod.visualsActive=false;mod.UpdateMenuBackgroundDecorations();Check(background.enabled,"F12 disabling mod restores title artwork");
            mod.visualsActive=true;mod.UpdateMenuBackgroundDecorations();mod.customMenuEnabled=false;mod.UpdateMenuBackgroundDecorations();Check(background.enabled,"Disabling custom design restores standard title artwork");
            mod.customMenuEnabled=true;mod.UpdateMenuBackgroundDecorations();MenuStyles.Instance=null;mod.UpdateMenuBackgroundDecorations();Check(background.enabled,"Destroyed menu style owner restores tracked resources");
            MenuStyles.Instance=styles;mod.UpdateMenuBackgroundDecorations();var replacementRoot=new UnityEngine.GameObject();var replacement=replacementRoot.Add<UnityEngine.SpriteRenderer>();styles.styles[0].styleObject=replacementRoot;mod.UpdateMenuBackgroundDecorations();
            Check(background.enabled&&particleRenderer.enabled&&!replacement.enabled,"Replacing root restores resources no longer owned and suppresses new root immediately");
            var added=replacementRoot.Child().Add<UnityEngine.SpriteRenderer>();UnityEngine.Time.unscaledTime+=.6f;mod.UpdateMenuBackgroundDecorations();Check(!added.enabled,"Dynamically added style renderer is discovered on bounded rescan");
            var newOwner=Owner(root);MenuStyles.Instance=newOwner;mod.UpdateMenuBackgroundDecorations();Check(replacement.enabled&&added.enabled&&!background.enabled,"Changing MenuStyles instance transfers and restores resource ownership");
            mod.ReleaseMenuBackgroundDecorations();originallyOff.enabled=false;MenuStyles.Instance=Owner(originallyOff.gameObject);mod.UpdateMenuBackgroundDecorations();originallyOff.enabled=true;mod.ReleaseMenuBackgroundDecorations();Check(originallyOff.enabled,"External enabled value wins compare-before-restore policy");
            var destroyed=new UnityEngine.GameObject().Add<UnityEngine.SpriteRenderer>();MenuStyles.Instance=Owner(destroyed.gameObject);mod.UpdateMenuBackgroundDecorations();destroyed.destroyed=true;mod.ReleaseMenuBackgroundDecorations();Check(mod.menuDecorationOriginalEnabled.Count==0,"Destroyed Unity renderer is never read or written during restoration");
            var invalidRoot=new UnityEngine.GameObject {scene=new UnityEngine.Scene {valid=false}};var asset=invalidRoot.Add<UnityEngine.SpriteRenderer>();MenuStyles.Instance=Owner(invalidRoot);mod.UpdateMenuBackgroundDecorations();Check(asset.enabled,"Prefab assets without a valid scene are untouched");
            mod.ReleaseMenuBackgroundDecorations();MenuStyles.Instance=null;UIManager.instance=null;
            Console.WriteLine("PASS: "+checks+" custom menu background decoration assertions (production renderer suppression and restoration). ");
        }
    }
}
