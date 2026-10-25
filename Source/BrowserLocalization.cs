using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private string uiLanguage="English";
        private float backgroundSettingsContentHeight=1000;
        private string UiText(string value) { return UiLocalization.Text(value,uiLanguage); }
        private void SetUiLanguage(string value)
        {
            value=UiLocalization.NormalizeLanguage(value);if(value==uiLanguage) return;
            uiLanguage=value;SaveExportSettings();
        }
        private readonly System.Collections.Generic.Dictionary<GUIStyle,System.Collections.Generic.Dictionary<int,GUIStyle>> uiFittedStyles=new System.Collections.Generic.Dictionary<GUIStyle,System.Collections.Generic.Dictionary<int,GUIStyle>>();
        private GUIStyle FitUiStyle(Rect rect,string text,GUIStyle style)
        {
            System.Collections.Generic.Dictionary<int,GUIStyle> sizes;
            if(!uiFittedStyles.TryGetValue(style,out sizes))
            {
                // Skin replacement can occur on scene changes; keep the style cache bounded.
                if(uiFittedStyles.Count>=32) uiFittedStyles.Clear();
                sizes=new System.Collections.Generic.Dictionary<int,GUIStyle>();uiFittedStyles.Add(style,sizes);
            }
            int size=style.fontSize>0?style.fontSize:12;var content=new GUIContent(text);
            while(true)
            {
                GUIStyle fitted;
                if(!sizes.TryGetValue(size,out fitted)) { fitted=new GUIStyle(style) {fontSize=size};sizes.Add(size,fitted); }
                if(size<=9 || (fitted.wordWrap ? fitted.CalcHeight(content,rect.width)<=rect.height : fitted.CalcSize(content).x<=rect.width)) return fitted;
                size--;
            }
        }
        private bool UiButton(Rect rect,string text) { return UiButton(rect,text,GUI.skin.button); }
        private bool UiButton(Rect rect,string text,GUIStyle style)
        { text=UiText(text);return GUI.Button(rect,new GUIContent(text),FitUiStyle(rect,text,style)); }
        private void UiLabel(Rect rect,string text) { UiLabel(rect,text,GUI.skin.label); }
        private void UiLabel(Rect rect,string text,GUIStyle style)
        { text=UiText(text);GUI.Label(rect,new GUIContent(text),style.wordWrap?style:FitUiStyle(rect,text,style)); }
        private bool UiToggle(Rect rect,bool value,string text)
        { text=UiText(text);return GUI.Toggle(rect,value,new GUIContent(text),FitUiStyle(rect,text,GUI.skin.toggle)); }
        private void UiBox(Rect rect,string text) { GUI.Box(rect,UiText(text)); }
    }
}
