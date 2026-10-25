using System.Collections.Generic;
using System.IO;
using System.Text;
using Modding;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        internal void RefreshPreserveManagerEntries()
        {
            textureBrowserMenuMode = false;
            textureBrowserEntityMode = false;
            textureBrowserNearbyMode = false;
            textureBrowserAtlasMode = false;
            textureBrowserPreserveMode = true;
            textureBrowserProjectileMode = false;
            textureBrowserEntries.Clear();
            textureBrowserListScroll = Vector2.zero;
            textureBrowserDetailScroll = Vector2.zero;
            selectedTextureBrowserIndex = -1;
            ResetTextureBrowserPreviewView();

            AddPreserveManagerRendererEntries();
            AddPreserveManagerPathEntries("Path Rule", "path", preservedObjectPaths);
            AddPreserveManagerPathEntries("Group Rule", "groupPath", preservedGroupObjectPaths);
            AddPreserveManagerPathEntries("Projectile Rule", "projectilePath", preservedProjectileObjectPaths);
            AddPreserveManagerNameEntries("Sprite Rule", "sprite", preservedSpriteNames);
            AddPreserveManagerNameEntries("Texture Rule", "texture", preservedTextureNames);
            AddPreserveManagerNameEntries("Material Rule", "material", preservedMaterialNames);
            AddPreserveManagerNameEntries("Legacy Asset Rule", "asset", preservedAssetNames);

            if (textureBrowserEntries.Count > 0)
            {
                selectedTextureBrowserIndex = 0;
            }

            textureBrowserNeedsRefresh = false;
            Log("ShaderStripMod preserve manager refreshed: " + GetTotalPreserveRuleCount() + " rules.");
        }

        private void AddPreserveManagerRendererEntries()
        {
            List<RendererPreserveRule> rules = new List<RendererPreserveRule>(preservedRendererRules);
            rules.Sort(CompareRendererPreserveRules);
            for (int i = 0; i < rules.Count; i++)
            {
                RendererPreserveRule rule = rules[i];
                if (rule == null)
                {
                    continue;
                }

                bool ignored = IsIgnoredRendererPreserveRule(rule);
                TextureBrowserEntry entry = new TextureBrowserEntry();
                entry.Kind = ignored ? (rule.Projectile ? "Ignored Projectile Renderer Rule" : "Ignored Renderer Rule") : (rule.Projectile ? "Projectile Renderer Rule" : "Renderer Rule");
                entry.Name = CleanUnityPath(rule.ObjectPath) + "  " + CleanUnityName(rule.RendererType);
                entry.PreservePath = CleanUnityPath(rule.ObjectPath);
                entry.SceneName = CleanUnityName(rule.SceneName);
                entry.ObjectPath = CleanUnityPath(rule.ObjectPath);
                entry.RendererType = CleanUnityName(rule.RendererType);
                entry.PreserveManagerRule = true;
                entry.PreserveManagerPathRule = false;
                entry.PreserveManagerRuleKind = rule.Projectile ? "projectileRenderer" : "renderer";
                entry.PreserveRuleValue = GetRendererPreserveRuleKey(rule);
                entry.Details = "ruleType=" + entry.PreserveManagerRuleKind +
                    "\nstatus=" + (ignored ? "ignored" : "effective") +
                    "\nscene=" + CleanUnityName(rule.SceneName) +
                    "\npath=" + CleanUnityPath(rule.ObjectPath) +
                    "\nrendererType=" + CleanUnityName(rule.RendererType) +
                    "\nsprite=" + CleanUnityName(rule.SpriteName) +
                    "\nmaterial=" + CleanUnityName(rule.MaterialName) +
                    "\ntexture=" + CleanUnityName(rule.TextureName);
                textureBrowserEntries.Add(entry);
            }
        }

        private void AddPreserveManagerPathEntries(string kind, string ruleKind, HashSet<string> rules)
        {
            List<string> paths = new List<string>(rules);
            paths.Sort(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                bool ignored = string.Equals(ruleKind, "projectilePath", System.StringComparison.OrdinalIgnoreCase) ? IsIgnoredProjectilePreservePath(path) : IsIgnoredPreservedObjectPathRule(path);
                TextureBrowserEntry entry = new TextureBrowserEntry();
                entry.Kind = ignored ? "Ignored " + kind : kind;
                entry.Name = path;
                entry.PreservePath = path;
                entry.PreserveManagerRule = true;
                entry.PreserveManagerPathRule = true;
                entry.PreserveManagerRuleKind = ruleKind;
                entry.PreserveRuleValue = path;
                entry.Details = "ruleType=" + ruleKind + "\nstatus=" + (ignored ? "ignored" : "effective") + "\npath=" + path;
                textureBrowserEntries.Add(entry);
            }
        }

        private void AddPreserveManagerNameEntries(string kind, string ruleKind, HashSet<string> rules)
        {
            List<string> names = new List<string>(rules);
            names.Sort(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                TextureBrowserEntry entry = new TextureBrowserEntry();
                entry.Kind = kind;
                entry.Name = name;
                entry.PreserveManagerRule = true;
                entry.PreserveManagerPathRule = false;
                entry.PreserveManagerRuleKind = ruleKind;
                entry.PreserveRuleValue = name;
                entry.Details = "ruleType=" + ruleKind + "\nstatus=effective\nname=" + name;
                textureBrowserEntries.Add(entry);
            }
        }

        private void UpdateEntityBrowserGroup(EntityBrowserGroup group, Renderer renderer, Vector3 origin)
        {
            if (group == null || renderer == null)
            {
                return;
            }

            group.RendererCount++;
            AddValue(group.Layers, GetLayerLabel(renderer.gameObject));
            AddValue(group.RendererTypes, renderer.GetType().Name);

            float distance = GetRendererDistance(renderer, origin);
            bool nearest = distance < group.Distance;
            if (nearest)
            {
                group.Distance = distance;
                group.Position = GetRendererCenter(renderer);
                group.PreviewRenderer = renderer;
            }

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            if (sprite != null)
            {
                AddValue(group.Sprites, GetObjectName(sprite));
                AddValue(group.Textures, GetObjectName(sprite.texture));
                if (group.PreviewSprite == null || nearest)
                {
                    group.PreviewSprite = sprite;
                    group.PreviewTexture = sprite.texture;
                }
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null)
            {
                return;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                AddValue(group.Materials, GetObjectName(material));
                Texture materialTexture = GetMaterialMainTexture(material);
                AddValue(group.Textures, GetObjectName(materialTexture));
                if (group.PreviewMaterial == null || nearest)
                {
                    group.PreviewMaterial = material;
                }

                if (group.PreviewTexture == null || (nearest && materialTexture != null))
                {
                    group.PreviewTexture = materialTexture;
                }
            }
        }

        private void CollectAtlasBrowserEntries(Renderer renderer, Vector3 origin, string sceneName, HashSet<string> seen)
        {
            if (renderer == null || renderer.gameObject == null)
            {
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null)
            {
                return;
            }

            string objectPath = GetObjectPath(renderer.gameObject);
            string cleanedObjectPath = CleanUnityPath(objectPath);
            string entityPath = GetEntityRootPath(cleanedObjectPath);
            string layer = GetLayerLabel(renderer.gameObject);
            string rendererType = renderer.GetType().Name;
            Vector3 position = GetRendererCenter(renderer);
            float distance = GetRendererDistance(renderer, origin);

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                Texture texture = GetMaterialMainTexture(material);
                string textureName = GetObjectName(texture);
                if (texture == null || texture.width <= 0 || texture.height <= 0 || string.IsNullOrEmpty(textureName))
                {
                    continue;
                }

                string materialName = GetObjectName(material);
                string key = sceneName + "\n" + cleanedObjectPath + "\n" + materialName + "\n" + textureName;
                if (!seen.Add(key))
                {
                    continue;
                }

                int score = ScoreAtlasExportSource(renderer, material, texture, cleanedObjectPath, entityPath);
                TextureBrowserEntry entry = new TextureBrowserEntry();
                entry.Kind = "Atlas";
                entry.Name = textureName;
                entry.Texture = texture;
                entry.Material = material;
                entry.Renderer = renderer;
                entry.HasDistance = true;
                entry.Distance = distance;
                entry.SceneName = sceneName;
                entry.ObjectPath = cleanedObjectPath;
                entry.PreservePath = entityPath;
                entry.Layer = layer;
                entry.RendererType = rendererType;
                entry.Position = position;
                entry.AtlasSource = true;
                entry.AtlasScore = score;
                entry.Details = "atlasTexture=" + textureName +
                    "\ntextureSize=" + GetTextureSize(texture) +
                    "\nmaterial=" + materialName +
                    "\nscene=" + sceneName +
                    "\npath=" + cleanedObjectPath +
                    "\nentityPath=" + entityPath +
                    "\nlayer=" + layer +
                    "\nrenderer=" + rendererType +
                    "\nposition=" + FormatVector3(position) +
                    "\ndistance=" + distance.ToString("0.00") +
                    "\natlasScore=" + score;
                textureBrowserEntries.Add(entry);
            }
        }

        private void AddEntityBrowserEntry(EntityBrowserGroup group)
        {
            if (group == null || string.IsNullOrEmpty(group.RootPath))
            {
                return;
            }

            TextureBrowserEntry entry = new TextureBrowserEntry();
            entry.Kind = group.Category;
            entry.Name = group.DisplayName;
            entry.Texture = group.PreviewTexture;
            entry.Sprite = group.PreviewSprite;
            entry.Material = group.PreviewMaterial;
            entry.Renderer = group.PreviewRenderer;
            entry.HasDistance = group.Distance < float.PositiveInfinity;
            entry.Distance = entry.HasDistance ? group.Distance : 0f;
            entry.SceneName = group.SceneName;
            entry.ObjectPath = group.RootPath;
            entry.PreservePath = group.RootPath;
            entry.Layer = JoinSorted(group.Layers);
            entry.RendererType = JoinSorted(group.RendererTypes);
            entry.Position = group.Position;
            entry.Details = "entityPath=" + group.RootPath +
                "\nscene=" + group.SceneName +
                "\ncategory=" + group.Category +
                "\nrenderers=" + group.RendererCount +
                "\nlayers=" + JoinSorted(group.Layers) +
                "\nrendererTypes=" + JoinSorted(group.RendererTypes) +
                "\nsprites=" + JoinSorted(group.Sprites) +
                "\ntextures=" + JoinSorted(group.Textures) +
                "\nmaterials=" + JoinSorted(group.Materials);
            textureBrowserEntries.Add(entry);
        }

        private bool IsEntityCandidateRenderer(Renderer renderer, string objectPath)
        {
            if (renderer == null || renderer.gameObject == null || string.IsNullOrEmpty(objectPath) || IsIgnoredEntityPath(objectPath) || IsHeroEntityPath(objectPath))
            {
                return false;
            }

            string rootPath = GetEntityRootPath(objectPath);
            if (IsHeroEntityRoot(rootPath))
            {
                return false;
            }

            string layerName = LayerMask.LayerToName(renderer.gameObject.layer);
            if (string.Equals(layerName, "Enemies", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(layerName, "Enemy Attack", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(layerName, "Corpse", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return ContainsAny(objectPath, BossNameHints) ||
                ContainsAny(objectPath, NpcNameHints) ||
                ContainsAny(objectPath, MobNameHints);
        }

        private bool IsIgnoredEntityPath(string objectPath)
        {
            string root = GetFirstPathSegment(objectPath);
            if (string.IsNullOrEmpty(root) || string.Equals(root, "_NPCs", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            for (int i = 0; i < EntityIgnoredRootPrefixes.Length; i++)
            {
                if (root.StartsWith(EntityIgnoredRootPrefixes[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetEntityRootPath(string objectPath)
        {
            string cleaned = CleanUnityPath(objectPath);
            if (string.IsNullOrEmpty(cleaned))
            {
                return "";
            }

            string[] parts = cleaned.Split('/');
            if (parts.Length == 0)
            {
                return "";
            }

            if (parts.Length > 1 && ContainsExact(parts[0], EntityContainerNames))
            {
                return parts[0] + "/" + parts[1];
            }

            return parts[0];
        }

        private string GetEntityDisplayName(string rootPath)
        {
            string cleaned = CleanUnityPath(rootPath);
            int separator = cleaned.LastIndexOf('/');
            return separator >= 0 ? cleaned.Substring(separator + 1) : cleaned;
        }

        private string GetEntityCategory(string rootPath, Renderer renderer)
        {
            if (ContainsAny(rootPath, BossNameHints) || rootPath.StartsWith("Battle Scene/", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Boss";
            }

            if (rootPath.StartsWith("_NPCs/", System.StringComparison.OrdinalIgnoreCase) || ContainsAny(rootPath, NpcNameHints))
            {
                return "NPC";
            }

            if (ContainsAny(rootPath, MobNameHints))
            {
                return "Mob";
            }

            if (renderer != null && renderer.gameObject != null)
            {
                string layerName = LayerMask.LayerToName(renderer.gameObject.layer);
                if (string.Equals(layerName, "Enemies", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(layerName, "Enemy Attack", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(layerName, "Corpse", System.StringComparison.OrdinalIgnoreCase))
                {
                    return "Mob";
                }
            }

            return "Entity";
        }

        private bool IsHeroEntityRoot(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                return false;
            }

            return string.Equals(rootPath, "Knight", System.StringComparison.OrdinalIgnoreCase) ||
                rootPath.EndsWith("/Knight", System.StringComparison.OrdinalIgnoreCase) ||
                rootPath.StartsWith("Stag Lift/Knight/", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsHeroEntityPath(string objectPath)
        {
            string cleaned = CleanUnityPath(objectPath);
            if (string.IsNullOrEmpty(cleaned))
            {
                return false;
            }

            return string.Equals(cleaned, "Knight", System.StringComparison.OrdinalIgnoreCase) ||
                cleaned.StartsWith("Knight/", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "Stag Lift/Knight", System.StringComparison.OrdinalIgnoreCase) ||
                cleaned.StartsWith("Stag Lift/Knight/", System.StringComparison.OrdinalIgnoreCase);
        }

        private string GetFirstPathSegment(string path)
        {
            string cleaned = CleanUnityPath(path);
            int separator = cleaned.IndexOf('/');
            return separator >= 0 ? cleaned.Substring(0, separator) : cleaned;
        }

        private bool ContainsAny(string value, string[] hints)
        {
            if (string.IsNullOrEmpty(value) || hints == null)
            {
                return false;
            }

            for (int i = 0; i < hints.Length; i++)
            {
                if (!string.IsNullOrEmpty(hints[i]) && value.IndexOf(hints[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsExact(string value, string[] values)
        {
            if (string.IsNullOrEmpty(value) || values == null)
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(value, values[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddValue(HashSet<string> values, string value)
        {
            string cleaned = CleanUnityName(value);
            if (!string.IsNullOrEmpty(cleaned))
            {
                values.Add(cleaned);
            }
        }

        private string JoinSorted(HashSet<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "";
            }

            List<string> sorted = new List<string>(values);
            sorted.Sort(System.StringComparer.OrdinalIgnoreCase);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(sorted[i]);
            }

            return builder.ToString();
        }

        private void CollectNearbyTextureEntries(Renderer renderer, Vector3 origin, string sceneName)
        {
            float distance = GetRendererDistance(renderer, origin);
            string path = GetObjectPath(renderer.gameObject);
            string layer = GetLayerLabel(renderer.gameObject);
            string rendererType = renderer.GetType().Name;
            Vector3 position = GetRendererCenter(renderer);

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            Texture spriteTexture = sprite != null ? sprite.texture : null;
            if (sprite != null)
            {
                AddNearbyTextureBrowserEntry(
                    "Nearby Sprite",
                    sprite.name,
                    spriteTexture,
                    sprite,
                    null,
                    distance,
                    sceneName,
                    path,
                    layer,
                    rendererType,
                    position,
                    renderer,
                    "spriteTexture=" + GetObjectName(spriteTexture));
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null)
            {
                return;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                Texture materialTexture = GetMaterialMainTexture(material);
                string shaderName = material.shader != null ? CleanUnityName(material.shader.name) : "";
                AddNearbyTextureBrowserEntry(
                    "Nearby Material",
                    material.name,
                    materialTexture != null ? materialTexture : spriteTexture,
                    sprite,
                    material,
                    distance,
                    sceneName,
                    path,
                    layer,
                    rendererType,
                    position,
                    renderer,
                    "materialTexture=" + GetObjectName(materialTexture) +
                    "\nsprite=" + GetObjectName(sprite) +
                    "\nspriteTexture=" + GetObjectName(spriteTexture) +
                    "\nshader=" + shaderName);
            }
        }

        private void AddNearbyTextureBrowserEntry(string kind, string name, Texture texture, Sprite sprite, Material material, float distance, string sceneName, string objectPath, string layer, string rendererType, Vector3 position, Renderer renderer, string details)
        {
            string cleanedName = CleanUnityName(name);
            if (string.IsNullOrEmpty(cleanedName))
            {
                cleanedName = GetObjectName(texture);
            }

            if (string.IsNullOrEmpty(cleanedName))
            {
                return;
            }

            TextureBrowserEntry entry = new TextureBrowserEntry();
            entry.Kind = kind;
            entry.Name = cleanedName;
            entry.Texture = texture;
            entry.Sprite = sprite;
            entry.Material = material;
            entry.HasDistance = true;
            entry.Distance = distance;
            entry.SceneName = sceneName;
            entry.ObjectPath = objectPath;
            entry.PreservePath = objectPath;
            entry.Layer = layer;
            entry.RendererType = rendererType;
            entry.Position = position;
            entry.Renderer = renderer;
            entry.Details = "distance=" + distance.ToString("0.00") +
                "\nscene=" + sceneName +
                "\npath=" + objectPath +
                "\nlayer=" + layer +
                "\nrenderer=" + rendererType +
                "\nposition=" + FormatVector3(position) +
                "\n" + details;
            textureBrowserEntries.Add(entry);
        }

        private bool IsRoomRenderer(Renderer renderer, string sceneName)
        {
            if (renderer == null || renderer.gameObject == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                return false;
            }

            try
            {
                return IsCurrentGameplayScene(renderer.gameObject.scene, sceneName);
            }
            catch
            {
                return false;
            }
        }

        private bool IsCurrentGameplayScene(UnityEngine.SceneManagement.Scene rendererScene, string activeSceneName)
        {
            if (!rendererScene.IsValid() || string.IsNullOrEmpty(activeSceneName) || IsMenuScene(activeSceneName))
            {
                return false;
            }

            string rendererSceneName = rendererScene.name;
            if (string.IsNullOrEmpty(rendererSceneName) || IsNonGameplayScene(rendererSceneName))
            {
                return false;
            }

            if (string.Equals(rendererSceneName, activeSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IsRelatedAdditiveSceneName(rendererSceneName, activeSceneName);
        }

        private bool IsRelatedAdditiveSceneName(string rendererSceneName, string activeSceneName)
        {
            if (string.IsNullOrEmpty(rendererSceneName) || string.IsNullOrEmpty(activeSceneName))
            {
                return false;
            }

            return rendererSceneName.StartsWith(activeSceneName + "_", System.StringComparison.OrdinalIgnoreCase) ||
                activeSceneName.StartsWith(rendererSceneName + "_", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsNonGameplayScene(string sceneName)
        {
            return IsMenuScene(sceneName) ||
                string.Equals(sceneName, "DontDestroyOnLoad", System.StringComparison.OrdinalIgnoreCase);
        }

        private string GetRendererSceneName(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject == null)
            {
                return "";
            }

            try
            {
                UnityEngine.SceneManagement.Scene scene = renderer.gameObject.scene;
                return scene.IsValid() ? scene.name : "";
            }
            catch
            {
                return "";
            }
        }

        private Vector3 GetNearbyTextureOrigin()
        {
            HeroController hero = HeroController.SilentInstance;
            if (hero != null)
            {
                return hero.transform.position;
            }

            Camera main = Camera.main;
            if (main != null)
            {
                return main.transform.position;
            }

            return Vector3.zero;
        }

        private Vector3 GetRendererCenter(Renderer renderer)
        {
            if (renderer == null)
            {
                return Vector3.zero;
            }

            try
            {
                return renderer.bounds.center;
            }
            catch
            {
                return renderer.transform != null ? renderer.transform.position : Vector3.zero;
            }
        }

        private float GetRendererDistance(Renderer renderer, Vector3 origin)
        {
            Vector3 center = GetRendererCenter(renderer);
            float dx = center.x - origin.x;
            float dy = center.y - origin.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private int CompareNearbyTextureBrowserEntries(TextureBrowserEntry left, TextureBrowserEntry right)
        {
            int distanceCompare = left.Distance.CompareTo(right.Distance);
            if (distanceCompare != 0)
            {
                return distanceCompare;
            }

            return string.Compare(left.Name, right.Name, System.StringComparison.OrdinalIgnoreCase);
        }

        private int CompareEntityBrowserEntries(TextureBrowserEntry left, TextureBrowserEntry right)
        {
            int categoryCompare = string.Compare(left.Kind, right.Kind, System.StringComparison.OrdinalIgnoreCase);
            if (categoryCompare != 0)
            {
                return categoryCompare;
            }

            if (left.HasDistance && right.HasDistance)
            {
                int distanceCompare = left.Distance.CompareTo(right.Distance);
                if (distanceCompare != 0)
                {
                    return distanceCompare;
                }
            }

            return string.Compare(left.Name, right.Name, System.StringComparison.OrdinalIgnoreCase);
        }

        private int CompareAtlasBrowserEntries(TextureBrowserEntry left, TextureBrowserEntry right)
        {
            if (left.HasDistance && right.HasDistance)
            {
                int distanceCompare = left.Distance.CompareTo(right.Distance);
                if (distanceCompare != 0)
                {
                    return distanceCompare;
                }
            }

            int scoreCompare = right.AtlasScore.CompareTo(left.AtlasScore);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            int nameCompare = string.Compare(left.Name, right.Name, System.StringComparison.OrdinalIgnoreCase);
            if (nameCompare != 0)
            {
                return nameCompare;
            }

            return string.Compare(left.ObjectPath, right.ObjectPath, System.StringComparison.OrdinalIgnoreCase);
        }

        private void AddTextureBrowserEntry(HashSet<string> seen, string kind, string name, Texture texture, Sprite sprite, Material material, string details)
        {
            string cleanedName = CleanUnityName(name);
            if (string.IsNullOrEmpty(cleanedName))
            {
                return;
            }

            string key = kind + "\n" + cleanedName + "\n" + GetObjectName(texture);
            if (!seen.Add(key))
            {
                return;
            }

            TextureBrowserEntry entry = new TextureBrowserEntry();
            entry.Kind = kind;
            entry.Name = cleanedName;
            entry.Texture = texture;
            entry.Sprite = sprite;
            entry.Material = material;
            entry.Details = details;
            textureBrowserEntries.Add(entry);
        }

        private int CompareTextureBrowserEntries(TextureBrowserEntry left, TextureBrowserEntry right)
        {
            int kindCompare = string.Compare(left.Kind, right.Kind, System.StringComparison.OrdinalIgnoreCase);
            if (kindCompare != 0)
            {
                return kindCompare;
            }

            return string.Compare(left.Name, right.Name, System.StringComparison.OrdinalIgnoreCase);
        }

        private string GetTextureSize(Texture texture)
        {
            if (texture == null)
            {
                return "";
            }

            return texture.width + "x" + texture.height;
        }

    }
}
