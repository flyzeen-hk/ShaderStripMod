using System.Collections.Generic;
using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private readonly Dictionary<Renderer,bool> menuDecorationOriginalEnabled=new Dictionary<Renderer,bool>();
        private readonly HashSet<Renderer> menuDecorationRenderers=new HashSet<Renderer>();
        private readonly List<Renderer> menuDecorationRemove=new List<Renderer>();
        private readonly List<GameObject> menuDecorationRoots=new List<GameObject>();
        private MenuStyles menuDecorationOwner;
        private int menuDecorationStyle=-1;
        private float nextMenuDecorationScan;
        private bool IsMenuDecorationProtected(Renderer renderer,MenuStyles styles)
        {
            if(renderer==null)return true;
            var ui=UIManager.instance;
            var logo=ui!=null ? ui.gameTitle : null;
            var styleLogo=styles!=null&&styles.title!=null ? styles.title.Title : null;
            if(renderer==(styles!=null ? styles.blackSolid : null)||renderer==logo||renderer==styleLogo)return true;
            // Menu artwork can be nested by other mods; retain logos and the actual button canvas.
            var canvas=ui!=null ? ui.UICanvas : null;
            for(var parent=renderer.transform;parent!=null;parent=parent.parent)
                if((logo!=null&&parent==logo.transform)||(styleLogo!=null&&parent==styleLogo.transform)||(canvas!=null&&parent==canvas.transform))return true;
            return false;
        }
        private bool MenuDecorationRootsChanged(MenuStyles styles)
        {
            if(styles!=menuDecorationOwner||styles.CurrentStyle!=menuDecorationStyle||styles.styles.Length!=menuDecorationRoots.Count)return true;
            for(int i=0;i<styles.styles.Length;i++)
                if((styles.styles[i]!=null ? styles.styles[i].styleObject : null)!=menuDecorationRoots[i])return true;
            return false;
        }
        private void UpdateMenuBackgroundDecorations()
        {
            // A logo-only design or failed/loading media must retain the ordinary menu artwork.
            if(!IsCustomMenuActive()||menuPlayback==null||menuPlayback.Failed||menuPlayback.Texture==null)
            {ReleaseMenuBackgroundDecorations();return;}
            var styles=MenuStyles.Instance;
            if(styles==null||styles.styles==null){ReleaseMenuBackgroundDecorations();return;}
            if(MenuDecorationRootsChanged(styles)||Time.unscaledTime>=nextMenuDecorationScan)
            {
                menuDecorationOwner=styles;menuDecorationStyle=styles.CurrentStyle;nextMenuDecorationScan=Time.unscaledTime+.5f;
                menuDecorationRoots.Clear();menuDecorationRenderers.Clear();
                foreach(var style in styles.styles)
                {
                    var root=style!=null ? style.styleObject : null;menuDecorationRoots.Add(root);
                    if(root==null||!root.scene.IsValid())continue;
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                        if(renderer!=null&&!IsMenuDecorationProtected(renderer,styles))menuDecorationRenderers.Add(renderer);
                }
            }
            menuDecorationRemove.Clear();
            foreach(var pair in menuDecorationOriginalEnabled)
                if(pair.Key==null||!menuDecorationRenderers.Contains(pair.Key)||IsMenuDecorationProtected(pair.Key,styles))menuDecorationRemove.Add(pair.Key);
            foreach(var renderer in menuDecorationRemove)
            {
                bool original=menuDecorationOriginalEnabled[renderer];
                // Restore only the value still owned by this mod; an external enabled=true wins.
                if(renderer!=null&&!renderer.enabled)renderer.enabled=original;
                menuDecorationOriginalEnabled.Remove(renderer);
            }
            foreach(var renderer in menuDecorationRenderers)
            {
                if(renderer==null||IsMenuDecorationProtected(renderer,styles))continue;
                if(!menuDecorationOriginalEnabled.ContainsKey(renderer))menuDecorationOriginalEnabled.Add(renderer,renderer.enabled);
                // Suppress rendering only. Particle simulation, music and menu scripts keep running.
                if(renderer.enabled)renderer.enabled=false;
            }
        }
        private void ReleaseMenuBackgroundDecorations()
        {
            foreach(var pair in menuDecorationOriginalEnabled)
                if(pair.Key!=null&&!pair.Key.enabled)pair.Key.enabled=pair.Value;
            menuDecorationOriginalEnabled.Clear();menuDecorationRenderers.Clear();menuDecorationRoots.Clear();menuDecorationRemove.Clear();
            menuDecorationOwner=null;menuDecorationStyle=-1;nextMenuDecorationScan=0;
        }
    }
}
