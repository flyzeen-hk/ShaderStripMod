using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private const int TextureBrowserMenuDesignTab=7;
        private bool customMenuEnabled,customMenuPaused;
        private string customMenuFile="",customMenuLogoFile="",customMenuLayout="Cover",customMenuTextColor="Original";
        private float customMenuTextScale=1;
        private string menuMediaDirectory,customMenuMessage="Import a background or logo to create your menu.";
        private readonly List<string> menuMediaFiles=new List<string>();
        private bool menuMediaFilesDirty=true;
        private MenuMediaPlayback menuPlayback;
        private string menuPlaybackName;
        private Material menuBackgroundMaterial;
        private Texture2D customLogoTexture;
        private Sprite customLogoSprite;
        private string loadedLogoName,customLogoMessage="";
        private SpriteRenderer customLogoRenderer;
        private Sprite originalLogoSprite;
        private float nextMenuStyleScan;
        private readonly List<Component> menuStyledTexts=new List<Component>();
        private readonly MenuValueOverride<Component,Color> menuTextColors=new MenuValueOverride<Component,Color>();
        private readonly MenuValueOverride<Component,float> menuTextSizes=new MenuValueOverride<Component,float>();
        private readonly MenuValueOverride<tk2dTextMesh,Vector3> menuTextScales=new MenuValueOverride<tk2dTextMesh,Vector3>();
        private readonly MenuValueOverride<tk2dTextMesh,Color> menuTextGradientColors=new MenuValueOverride<tk2dTextMesh,Color>();
        private void InitializeCustomMenuPaths()
        {menuMediaDirectory=Path.Combine(Path.GetDirectoryName(settingsFilePath)??Application.persistentDataPath,"menu-media");menuMediaFilesDirty=true;}
        private bool IsCustomMenuActive(){return visualsActive&&customMenuEnabled&&MenuMediaPolicy.IsTitleScene(GetActiveSceneName());}
        private void RefreshMenuMediaFiles()
        {
            if(!menuMediaFilesDirty)return;menuMediaFilesDirty=false;menuMediaFiles.Clear();
            try
            {
                if(string.IsNullOrEmpty(menuMediaDirectory))InitializeCustomMenuPaths();
                if(!Directory.Exists(menuMediaDirectory))return;
                foreach(string path in Directory.GetFiles(menuMediaDirectory))if(MenuMediaPolicy.IsSupported(Path.GetExtension(path)))menuMediaFiles.Add(Path.GetFileName(path));
                menuMediaFiles.Sort(StringComparer.OrdinalIgnoreCase);
            }catch(Exception ex){customMenuMessage="Menu media error: "+ex.Message;}
        }
        private void UpdateCustomMenu()
        {
            if(!IsCustomMenuActive()){ReleaseCustomMenu();return;}
            if(menuPlaybackName!=customMenuFile)
            {
                ReleaseMenuPlayback();menuPlaybackName=customMenuFile;
                if(!string.IsNullOrEmpty(customMenuFile))
                {
                    try{menuPlayback=new MenuMediaPlayback(MenuMediaPolicy.Resolve(menuMediaDirectory,customMenuFile),customMenuPaused||(textureBrowserVisible&&pauseGameWhileBrowserOpen));}
                    catch(Exception ex){customMenuMessage="Menu media error: "+ex.Message;}
                }
            }
            if(menuPlayback!=null){menuPlayback.Update(customMenuPaused||(textureBrowserVisible&&pauseGameWhileBrowserOpen));customMenuMessage=menuPlayback.Message;}
            UpdateMenuBackgroundDecorations();UpdateCustomMenuLogo();UpdateCustomMenuTextStyle();
            if(!string.IsNullOrEmpty(customLogoMessage))customMenuMessage=customLogoMessage;
        }
        private bool TryGetCustomMenuBackground(out Texture texture,out Material material)
        {
            texture=null;material=null;
            if(!IsCustomMenuActive()||menuPlayback==null||menuPlayback.Texture==null)return false;
            texture=menuPlayback.Texture;
            if(menuBackgroundMaterial==null)
            {
                var shader=Shader.Find("Sprites/Default");if(shader==null)return false;
                menuBackgroundMaterial=new Material(shader) {name="ShaderStripMod Custom Menu",hideFlags=HideFlags.HideAndDontSave};
            }
            menuBackgroundMaterial.mainTexture=texture;material=menuBackgroundMaterial;return true;
        }
        private void ReleaseMenuPlayback()
        {
            ReleaseMenuBackgroundDecorations();HideImageBackground();
            if(menuPlayback!=null)menuPlayback.Dispose();menuPlayback=null;menuPlaybackName=null;
            if(menuBackgroundMaterial!=null)UnityEngine.Object.Destroy(menuBackgroundMaterial);menuBackgroundMaterial=null;
        }
        private void ReleaseCustomMenu()
        {
            ReleaseMenuBackgroundDecorations();
            if(menuPlayback!=null||menuPlaybackName!=null||menuBackgroundMaterial!=null)ReleaseMenuPlayback();
            ReleaseCustomMenuLogo();ReleaseCustomMenuTextStyle();
        }
        private string ImportMenuMedia(bool logo)
        {
            string chosen=ShowBackgroundImageFilePicker(logo ? "Logo images\0*.png;*.jpg;*.jpeg\0\0" : "Menu media\0*.png;*.jpg;*.jpeg;*.gif;*.mp4;*.webm;*.mov;*.m4v;*.avi\0\0",logo ? "Choose your menu logo" : "Choose menu background (image, GIF or video)",menuMediaDirectory);
            if(string.IsNullOrEmpty(chosen))return null;
            string ext=Path.GetExtension(chosen);
            if(logo ? !MenuMediaPolicy.IsImage(ext) : !MenuMediaPolicy.IsSupported(ext))throw new IOException(logo ? "Choose a PNG/JPG logo." : "Unsupported menu media format.");
            var info=new FileInfo(chosen);long limit=MenuMediaPolicy.IsVideo(ext)?1024L*1024*1024:64L*1024*1024;
            if(!info.Exists||info.Length==0||info.Length>limit)throw new IOException("File is empty or exceeds the media size limit.");
            Directory.CreateDirectory(menuMediaDirectory);
            string name=Path.GetFileName(chosen),destination=MenuMediaPolicy.Resolve(menuMediaDirectory,name);
            if(!string.Equals(Path.GetFullPath(chosen),destination,StringComparison.OrdinalIgnoreCase))
            {
                int n=2;string stem=Path.GetFileNameWithoutExtension(name);
                while(File.Exists(destination)){name=stem+" ("+n+++")"+ext;destination=MenuMediaPolicy.Resolve(menuMediaDirectory,name);}
                File.Copy(chosen,destination,false);
            }
            menuMediaFilesDirty=true;return name;
        }
        private void ChooseCustomMenuMedia(bool logo)
        {
            try
            {
                string name=ImportMenuMedia(logo);if(name==null)return;
                if(logo){customMenuLogoFile=name;ReleaseCustomMenuLogo();}else{customMenuFile=name;ReleaseMenuPlayback();}
                customMenuEnabled=true;SaveExportSettings();UpdateCustomMenu();if(visualsActive)ApplyStrip();
                customMenuMessage=logo ? "Custom logo selected." : "Custom menu background selected.";
            }catch(Exception ex){customMenuMessage="Menu media error: "+ex.Message;}
        }
        private void RemoveCustomMenuMedia(string name)
        {
            bool releasedVideo=false;
            try
            {
                string path=MenuMediaPolicy.Resolve(menuMediaDirectory,name);
                // Images and decoded GIFs do not keep the source file open. Keep their live textures
                // until the move succeeds, so a failed removal leaves the current design intact.
                if(customMenuFile==name&&MenuMediaPolicy.IsVideo(Path.GetExtension(name)))
                {ReleaseMenuPlayback();releasedVideo=true;}
                string removed=Path.Combine(menuMediaDirectory,"removed");Directory.CreateDirectory(removed);
                File.Move(path,Path.Combine(removed,Guid.NewGuid().ToString("N")+"_"+name));
                if(customMenuFile==name){customMenuFile="";ReleaseMenuPlayback();}
                if(customMenuLogoFile==name){customMenuLogoFile="";ReleaseCustomMenuLogo();}
                menuMediaFilesDirty=true;
                SaveExportSettings();customMenuMessage="Menu media removed (recoverable copy saved).";
            }
            catch(Exception ex)
            {
                // The saved selections have not changed. Recreate a released video on the next update.
                if(releasedVideo)menuPlaybackName=null;
                customMenuMessage="Menu media error: "+ex.Message;
            }
        }
    }
}
