using System.Collections.Generic;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private sealed class PreservedRendererProxy
        {
            internal Renderer Source;
            internal GameObject ProxyObject;
            internal Renderer ProxyRenderer;
            internal int LastSeenFrame;
            internal string SourcePath;
        }

        private sealed class RendererPreserveRule
        {
            internal string SceneName;
            internal string ObjectPath;
            internal string RendererType;
            internal string SpriteName;
            internal string MaterialName;
            internal string TextureName;
            internal string RuleKey;
            internal bool Projectile;
        }

        private sealed class RecentRendererSnapshot
        {
            internal int InstanceId;
            internal Renderer Renderer;
            internal float LastSeenTime;
            internal string SceneName;
            internal string ObjectPath;
            internal string Layer;
            internal string RendererType;
            internal string SpriteName;
            internal string MaterialName;
            internal string TextureName;
            internal Vector3 Position;
            internal Texture Texture;
            internal Sprite Sprite;
            internal Material Material;
        }

        private sealed class TextureBrowserEntry
        {
            internal string Kind;
            internal string Name;
            internal string Details;
            internal string PreservePath;
            internal Texture Texture;
            internal Sprite Sprite;
            internal Material Material;
            internal Renderer Renderer;
            internal bool MenuResource;
            internal bool HasDistance;
            internal float Distance;
            internal string SceneName;
            internal string ObjectPath;
            internal string Layer;
            internal string RendererType;
            internal int RendererInstanceId;
            internal Vector3 Position;
            internal bool AtlasSource;
            internal int AtlasScore;
            internal bool PreserveManagerRule;
            internal bool PreserveManagerPathRule;
            internal string PreserveManagerRuleKind;
            internal string PreserveRuleValue;
        }

        private sealed class TextureExportSource
        {
            internal Texture Texture;
            internal Sprite Sprite;
            internal Material Material;
            internal Renderer Renderer;
            internal string SourcePath;
            internal string SourceDescription;
            internal bool SpriteRegion;
            internal int Score;
        }

        private sealed class EntityBrowserGroup
        {
            internal string RootPath;
            internal string SceneName;
            internal string DisplayName;
            internal string Category;
            internal int RendererCount;
            internal float Distance;
            internal Vector3 Position;
            internal Texture PreviewTexture;
            internal Sprite PreviewSprite;
            internal Material PreviewMaterial;
            internal Renderer PreviewRenderer;
            internal readonly HashSet<string> Layers = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            internal readonly HashSet<string> RendererTypes = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            internal readonly HashSet<string> Sprites = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            internal readonly HashSet<string> Textures = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            internal readonly HashSet<string> Materials = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        }
    }
}
