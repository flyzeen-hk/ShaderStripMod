using System;
using System.Globalization;
using System.IO;
using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private GUIStyle customMenuLabelStyle,customMenuButtonStyle;
        private float customMenuContentHeight=1200;
        private void MenuDesignLabel(ref float y,float width,string text)
        {
            text=UiText(text);float h=Mathf.Max(20,customMenuLabelStyle.CalcHeight(new GUIContent(text),width));
            UiLabel(new Rect(0,y,width,h),text,customMenuLabelStyle);y+=h+6;
        }
        private void DrawCustomMenuTab(Rect content)
        {
            if(customMenuLabelStyle==null)customMenuLabelStyle=new GUIStyle(GUI.skin.label){wordWrap=true};
            if(customMenuButtonStyle==null)customMenuButtonStyle=new GUIStyle(GUI.skin.button){wordWrap=true};
            RefreshMenuMediaFiles();UiBox(content,"");var view=new Rect(content.x+8,content.y+8,content.width-16,content.height-16);
            float width=Mathf.Max(1,view.width-20),y=0,half=(width-6)/2;
            textureBrowserDetailScroll=GUI.BeginScrollView(view,textureBrowserDetailScroll,new Rect(0,0,width,Mathf.Max(view.height,customMenuContentHeight)));
            MenuDesignLabel(ref y,width,"Custom title menu");
            MenuDesignLabel(ref y,width,"Background, logo and button text. Applies to the title menu; game buttons keep their actions.");
            MenuDesignLabel(ref y,width,customMenuMessage);
            if(!MenuMediaPolicy.IsTitleScene(GetActiveSceneName()))MenuDesignLabel(ref y,width,"Menu design can be edited here. Preview it in the title menu.");
            if(backgroundSettingsLabelStyle==null)backgroundSettingsLabelStyle=new GUIStyle(GUI.skin.label){wordWrap=true};
            bool enabled=DrawWrappedToggle(ref y,width,customMenuEnabled,"Enable custom menu");
            bool chooseMedia=UiButton(new Rect(0,y,width,30),"Import background: image / GIF / video",customMenuButtonStyle);y+=36;
            MenuDesignLabel(ref y,width,UiText("Background: ")+(string.IsNullOrEmpty(customMenuFile)?UiText("Not selected"):customMenuFile));
            bool paused=DrawWrappedToggle(ref y,width,customMenuPaused,"Pause menu animation");
            bool restart=UiButton(new Rect(0,y,width,28),"Restart / retry media");y+=34;
            MenuDesignLabel(ref y,width,"Background placement");
            string layout=customMenuLayout;
            if(DrawModeButton(new Rect(0,y,width,26),"Fill screen (crop edges)",layout=="Cover"))layout="Cover";y+=30;
            if(DrawModeButton(new Rect(0,y,width,26),"Fit entire image (keep proportions)",layout=="Contain"))layout="Contain";y+=30;
            if(DrawModeButton(new Rect(0,y,width,26),"Stretch to screen",layout=="Stretch"))layout="Stretch";y+=34;
            bool chooseLogo=UiButton(new Rect(0,y,half,30),"Import logo (PNG / JPG)",customMenuButtonStyle);
            bool clearLogo=UiButton(new Rect(half+6,y,half,30),"Use game logo",customMenuButtonStyle);y+=36;
            MenuDesignLabel(ref y,width,UiText("Logo: ")+(string.IsNullOrEmpty(customMenuLogoFile)?UiText("Game logo"):customMenuLogoFile));
            MenuDesignLabel(ref y,width,"Button text color (keeps selection brightness)");
            string color=customMenuTextColor;float column=(width-12)/3;
            for(int i=0;i<MenuTextColorNames.Length;i++)
            {
                string name=MenuTextColorNames[i];if(DrawModeButton(new Rect((i%3)*(column+6),y+(i/3)*30,column,26),name,color==name))color=name;
            }
            y+=90;
            MenuDesignLabel(ref y,width,"Button text size");
            float scale=GUI.HorizontalSlider(new Rect(0,y,width-65,24),customMenuTextScale,.75f,1.35f);
            UiLabel(new Rect(width-60,y,60,24),Mathf.RoundToInt(scale*100)+"%");y+=32;
            bool reset=UiButton(new Rect(0,y,width,28),"Reset menu design");y+=36;
            MenuDesignLabel(ref y,width,"Imported menu media");
            string background=null,logo=null,remove=null;
            foreach(string name in menuMediaFiles)
            {
                MenuDesignLabel(ref y,width,name);
                float third=(width-12)/3;
                if(UiButton(new Rect(0,y,third,28),"As background"))background=name;
                bool old=GUI.enabled;GUI.enabled=old&&MenuMediaPolicy.IsImage(Path.GetExtension(name));
                if(UiButton(new Rect(third+6,y,third,28),"As logo"))logo=name;GUI.enabled=old;
                if(UiButton(new Rect(2*(third+6),y,third,28),"Remove"))remove=name;y+=36;
            }
            MenuDesignLabel(ref y,width,"PNG/JPG backgrounds and logos, animated GIF, MP4/WebM/MOV/M4V/AVI video. Video is silent and looped; codecs depend on Unity/Windows. MP4 H.264 is the preferred format.");
            MenuDesignLabel(ref y,width,"F9 pause also freezes menu animation. Media removal keeps a recoverable copy in menu-media/removed.");
            if(!visualsActive)MenuDesignLabel(ref y,width,"Enable the mod visuals with F12 to preview your menu.");
            GUI.EndScrollView();customMenuContentHeight=y+16;
            bool changed=enabled!=customMenuEnabled||paused!=customMenuPaused||layout!=customMenuLayout||color!=customMenuTextColor||Mathf.Abs(scale-customMenuTextScale)>.001f;
            if(changed)
            {
                customMenuEnabled=enabled;customMenuPaused=paused;customMenuLayout=layout;customMenuTextColor=color;customMenuTextScale=scale;
                SaveExportSettings();UpdateCustomMenu();UpdateImageBackground();
            }
            if(restart){ReleaseMenuPlayback();UpdateCustomMenu();}
            if(background!=null){customMenuFile=background;customMenuEnabled=true;ReleaseMenuPlayback();SaveExportSettings();UpdateCustomMenu();}
            if(logo!=null){customMenuLogoFile=logo;customMenuEnabled=true;ReleaseCustomMenuLogo();SaveExportSettings();UpdateCustomMenu();}
            if(clearLogo){customMenuLogoFile="";ReleaseCustomMenuLogo();SaveExportSettings();}
            if(reset){customMenuEnabled=false;customMenuFile=customMenuLogoFile="";customMenuPaused=false;customMenuLayout="Cover";customMenuTextColor="Original";customMenuTextScale=1;ReleaseCustomMenu();SaveExportSettings();UpdateImageBackground();}
            if(remove!=null)RemoveCustomMenuMedia(remove);
            if(chooseMedia)ChooseCustomMenuMedia(false);if(chooseLogo)ChooseCustomMenuMedia(true);
        }
    }
}
