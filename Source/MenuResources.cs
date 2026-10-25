using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        internal void RefreshMenuTextureBrowserEntries()
        {
            ReleaseMaskEditor();ReleaseMenuObjectPreview();textureBrowserSearch="";
            textureBrowserEntityMode = textureBrowserNearbyMode = textureBrowserAtlasMode = textureBrowserPreserveMode = textureBrowserProjectileMode = false;
            textureBrowserMenuMode = true;
            textureBrowserEntries.Clear();
            textureBrowserListScroll = textureBrowserDetailScroll = Vector2.zero;
            selectedTextureBrowserIndex = -1;
            ResetTextureBrowserPreviewView();
            var seen = new HashSet<int>();
            foreach (var graphic in Resources.FindObjectsOfTypeAll<Graphic>())
            {
                if (graphic == null || IsTmpText(graphic.GetType()) || !IsMenuResource(graphic)) continue;
                try
                {
                    var image = graphic as Image;
                    var text = graphic as Text;
                    Sprite sprite = image != null ? (image.overrideSprite != null ? image.overrideSprite : image.sprite) : null;
                    var material = graphic.materialForRendering;
                    // A font atlas is a resource, not a rendered screenshot of the label.
                    AddMenuResourceEntry(graphic, text != null ? "Menu Text" : (image != null ? "Menu Image" : "Menu " + graphic.GetType().Name),
                        graphic.mainTexture, sprite, material, null, text != null ? text.text : "", text != null ? GetObjectName(text.font) : "");
                    seen.Add(graphic.GetInstanceID());
                }
                catch (Exception ex) { Log("Menu graphic skipped: " + ex.Message); }
            }
            foreach (var text in Resources.FindObjectsOfTypeAll<TextMesh>())
            {
                if (text == null || !IsMenuResource(text)) continue;
                var renderer = text.GetComponent<Renderer>();
                var material = renderer != null ? renderer.sharedMaterial : (text.font != null ? text.font.material : null);
                AddMenuResourceEntry(text, "Menu TextMesh", GetMaterialMainTexture(material), null, material, renderer, text.text, GetObjectName(text.font));
                if (renderer != null) seen.Add(renderer.GetInstanceID());
            }
            // Hollow Knight versions/mods can use either legacy TextMeshPro or current TMP.
            // No assembly dependency: recognize the text base type, then read public resource properties.
            foreach (var component in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (component == null || !IsTmpText(component.GetType()) || !IsMenuResource(component)) continue;
                try
                {
                    var renderer = component.GetComponent<Renderer>();
                    var font = ReadMenuProperty(component, "font") as UnityEngine.Object;
                    var material = ReadMenuProperty(component, "fontSharedMaterial") as Material;
                    if (material == null && renderer != null) material = renderer.sharedMaterial;
                    Texture texture = GetMaterialMainTexture(material);
                    string label = ReadMenuProperty(component, "text") as string ?? "";
                    var pages = ReadMenuProperty(font, "atlasTextures") as Texture[];
                    if (texture == null) texture = ReadMenuProperty(font, "atlas") as Texture;
                    if (texture == null && pages != null && pages.Length > 0) texture = pages[0];
                    AddMenuResourceEntry(component, "Menu TMP Text", texture, null, material, renderer, label, GetObjectName(font));
                    if (pages != null)
                        for (int i = 0; i < pages.Length; i++)
                            if (pages[i] != null && pages[i] != texture)
                                AddMenuResourceEntry(component, "Menu Font Atlas " + i, pages[i], null, null, null, label, GetObjectName(font));
                    if (renderer != null) seen.Add(renderer.GetInstanceID());
                    seen.Add(component.GetInstanceID());
                }
                catch (Exception ex) { Log("Menu text skipped: " + ex.Message); }
            }
            CollectTk2dMenuResources(seen);
            foreach (var renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (renderer == null || seen.Contains(renderer.GetInstanceID()) || IsProxyRenderer(renderer) || !IsMenuResource(renderer)) continue;
                try
                {
                    var sr = renderer as SpriteRenderer;
                    var sprite = sr != null ? sr.sprite : null;
                    var materials = renderer.sharedMaterials;
                    if (sprite != null)
                        AddMenuResourceEntry(renderer, "Menu Sprite", sprite.texture, sprite, renderer.sharedMaterial, renderer, "", "");
                    if (materials == null) continue;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var material = materials[i];
                        if (material == null) continue;
                        AddMenuResourceEntry(renderer, "Menu Material " + i, GetMaterialMainTexture(material), null, material, renderer, "", "");
                    }
                }
                catch (Exception ex) { Log("Menu renderer skipped: " + ex.Message); }
            }
            textureBrowserEntries.Sort(CompareTextureBrowserEntries);
            if (textureBrowserEntries.Count > 0) selectedTextureBrowserIndex = 0;
            textureBrowserNeedsRefresh = false;
            Log("Menu resource browser refreshed: " + textureBrowserEntries.Count + " entries (including inactive menu pages).");
        }
        private bool IsMenuResource(Component component)
        {
            if (component == null || component.gameObject == null) return false;
            var obj = component.gameObject;
            var scene = obj.scene;
            var canvas = component.GetComponentInParent<Canvas>();
            // GetComponentInParent skips inactive parents in some Unity versions. Walk transforms too.
            var menuCanvas = UIManager.instance != null ? UIManager.instance.UICanvas : null;
            var menuRoot=ReadMenuProperty(UIManager.instance,"transform") as Transform;
            var title=ReadMenuProperty(UIManager.instance,"gameTitle") as Renderer;
            bool inMenuCanvas = title!=null&&title.gameObject==obj, hasCanvas = canvas != null;
            for (var parent = component.transform; parent != null; parent = parent.parent)
            {
                if (parent.GetComponent<Canvas>() != null) hasCanvas = true;
                if ((menuCanvas != null && parent == menuCanvas.transform) || (menuRoot!=null&&parent==menuRoot)) inMenuCanvas = true;
            }
            string layer = LayerMask.LayerToName(obj.layer);
            bool menuLayer = string.Equals(layer, "uGUI", StringComparison.OrdinalIgnoreCase) || string.Equals(layer, "Menu", StringComparison.OrdinalIgnoreCase) || string.Equals(layer, "UI", StringComparison.OrdinalIgnoreCase);
            return MenuResourcePolicy.Includes(scene.name, scene.IsValid(), inMenuCanvas, hasCanvas || (IsMenuScene(GetActiveSceneName())&&string.Equals(scene.name,"DontDestroyOnLoad",StringComparison.OrdinalIgnoreCase)), menuLayer);
        }
        private static bool IsTmpText(Type type)
        {
            for (; type != null; type = type.BaseType)
                if (type.FullName == "TMPro.TMP_Text" || type.FullName == "TMPro.TextMeshPro" || type.FullName == "TMPro.TextMeshProUGUI") return true;
            return false;
        }
        private static object ReadMenuProperty(object source, string name)
        {
            if (source == null) return null;
            var property = source.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(source, null);
            var field = source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            return field != null ? field.GetValue(source) : null;
        }
        private void AddMenuResourceEntry(Component source, string kind, Texture texture, Sprite sprite, Material material, Renderer renderer, string text, string font)
        {
            var obj = source.gameObject;
            string resource = sprite != null ? GetObjectName(sprite) : (!string.IsNullOrEmpty(font) ? font : GetObjectName(texture));
            string path = GetObjectPath(obj);
            string details = "scene=" + obj.scene.name + "\npath=" + path + "\ncomponent=" + source.GetType().FullName +
                "\nactive=" + obj.activeInHierarchy + "\nlayer=" + GetLayerLabel(obj) + "\ntexture=" + GetObjectName(texture) +
                "\nmaterial=" + GetObjectName(material) + "\nsprite=" + GetObjectName(sprite) + "\nfont=" + font + "\ntext=" + text;
            if (!string.IsNullOrEmpty(font)) details += "\nPreview uses the label artwork when its renderer geometry is available. Export Atlas keeps the full font texture; the label text is listed above.";
            textureBrowserEntries.Add(new TextureBrowserEntry {
                Kind = kind, Name = obj.name + (string.IsNullOrEmpty(resource) ? "" : " | " + resource), Details = details,
                Texture = texture, Sprite = sprite, Material = material, Renderer = renderer,
                MenuResource = true, SceneName = obj.scene.name, ObjectPath = path, Layer = GetLayerLabel(obj), RendererType = source.GetType().Name, Position = source.transform.position
            });
        }
    }
}