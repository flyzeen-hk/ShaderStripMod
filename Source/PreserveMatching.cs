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
        private bool IsProxyRenderer(Renderer renderer)
        {
            if (renderer == null || renderer.transform == null || proxyRoot == null)
            {
                return false;
            }

            return renderer.transform == proxyRoot.transform || renderer.transform.IsChildOf(proxyRoot.transform);
        }

        private bool RendererHasPreservedAsset(Renderer renderer)
        {
            if (renderer == null || !HasAnyAssetPreserveRules())
            {
                return false;
            }

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            if (spriteRenderer != null)
            {
                Sprite sprite = spriteRenderer.sprite;
                if (IsPreservedSprite(sprite) ||
                    (sprite != null && IsPreservedTexture(sprite.texture)) ||
                    IsPreservedAssetName(sprite) ||
                    (sprite != null && IsPreservedAssetName(sprite.texture)))
                {
                    return true;
                }
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null)
            {
                return false;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                if (IsPreservedMaterial(material) ||
                    IsPreservedTexture(GetMaterialMainTexture(material)) ||
                    IsPreservedAssetName(material) ||
                    IsPreservedAssetName(GetMaterialMainTexture(material)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool RendererHasPreservedPath(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject == null || !HasPathPreserveRules())
            {
                return false;
            }

            return IsPreservedObjectPath(renderer.gameObject);
        }

        private bool RendererHasProjectilePath(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject == null)
            {
                return false;
            }

            return RendererHasProjectileRendererRule(renderer) || IsProjectileObjectPath(renderer.gameObject);
        }

        private bool RendererIsPreserved(Renderer renderer)
        {
            return HasActiveSpriteMask(renderer) || RendererHasPreservedRendererRule(renderer) || RendererHasPreservedPath(renderer) || RendererHasPreservedAsset(renderer);
        }

        private bool ShouldPreserveMaterial(Renderer renderer, Material material)
        {
            if (renderer != null && IsProxyRenderer(renderer))
            {
                return true;
            }

            if (material == null)
            {
                return renderer != null && (RendererHasPreservedRendererRule(renderer) || RendererHasPreservedPath(renderer));
            }

            if (HasActiveSpriteMask(renderer) || RendererHasPreservedRendererRule(renderer))
            {
                return true;
            }

            if (RendererHasPreservedPath(renderer))
            {
                return true;
            }

            if (IsPreservedMaterial(material) ||
                IsPreservedTexture(GetMaterialMainTexture(material)) ||
                IsPreservedAssetName(material) ||
                IsPreservedAssetName(GetMaterialMainTexture(material)))
            {
                return true;
            }

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            if (spriteRenderer == null)
            {
                return false;
            }

            Sprite sprite = spriteRenderer.sprite;
            return IsPreservedSprite(sprite) ||
                (sprite != null && IsPreservedTexture(sprite.texture)) ||
                IsPreservedAssetName(sprite) ||
                (sprite != null && IsPreservedAssetName(sprite.texture));
        }

        private bool RendererHasPreservedRendererRule(Renderer renderer)
        {
            bool projectile;
            return RendererHasPreservedRendererRule(renderer, out projectile);
        }

        private bool RendererHasProjectileRendererRule(Renderer renderer)
        {
            bool projectile;
            return RendererHasPreservedRendererRule(renderer, out projectile) && projectile;
        }

        private bool RendererHasPreservedRendererRule(Renderer renderer, out bool projectile)
        {
            projectile = false;
            if (renderer == null || renderer.gameObject == null || preservedRendererRules.Count == 0)
            {
                return false;
            }

            string sceneName = GetRendererSceneName(renderer);
            string objectPath = CleanUnityPath(GetObjectPath(renderer.gameObject));
            string rendererType = CleanUnityName(renderer.GetType().Name);
            if (string.IsNullOrEmpty(objectPath))
            {
                return false;
            }

            EnsureRendererPreserveRuleIndex();
            string matchKey = GetRendererPreserveRuleMatchKey(sceneName, objectPath, rendererType);
            if (!string.IsNullOrEmpty(matchKey))
            {
                if (preservedProjectileRendererExactMatchKeys.Contains(matchKey))
                {
                    projectile = true;
                    return true;
                }

                if (preservedRendererExactMatchKeys.Contains(matchKey))
                {
                    if (!preservedRendererRuleIndexHasFallbackRules)
                    {
                        return true;
                    }
                }

                if (!preservedRendererRuleIndexHasFallbackRules)
                {
                    return false;
                }
            }

            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                RendererPreserveRule rule = preservedRendererRules[i];
                if (IsIgnoredRendererPreserveRule(rule))
                {
                    continue;
                }

                if (!RendererMatchesPreserveRule(rule, renderer, sceneName, objectPath, rendererType))
                {
                    continue;
                }

                projectile = rule.Projectile;
                return true;
            }

            return false;
        }

        private void EnsureRendererPreserveRuleIndex()
        {
            if (!preservedRendererRuleIndexDirty)
            {
                return;
            }

            preservedRendererExactMatchKeys.Clear();
            preservedProjectileRendererExactMatchKeys.Clear();
            preservedRendererRuleIndexHasFallbackRules = false;

            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                RendererPreserveRule rule = preservedRendererRules[i];
                if (IsIgnoredRendererPreserveRule(rule))
                {
                    continue;
                }

                string sceneName = CleanUnityName(rule.SceneName);
                string objectPath = CleanUnityPath(rule.ObjectPath);
                string rendererType = CleanUnityName(rule.RendererType);
                if (RendererPreserveRuleHasAssetPredicates(rule))
                {
                    preservedRendererRuleIndexHasFallbackRules = true;
                    continue;
                }

                string matchKey = GetRendererPreserveRuleMatchKey(sceneName, objectPath, rendererType);
                if (string.IsNullOrEmpty(matchKey))
                {
                    preservedRendererRuleIndexHasFallbackRules = true;
                    continue;
                }

                if (rule.Projectile)
                {
                    preservedRendererRuleIndexHasFallbackRules = true;
                    preservedProjectileRendererExactMatchKeys.Add(matchKey);
                }
                else
                {
                    preservedRendererExactMatchKeys.Add(matchKey);
                }
            }

            preservedRendererRuleIndexDirty = false;
        }

        private string GetRendererPreserveRuleMatchKey(string sceneName, string objectPath, string rendererType)
        {
            string cleanedScene = CleanUnityName(sceneName);
            string cleanedPath = CleanUnityPath(objectPath);
            string cleanedType = CleanUnityName(rendererType);
            if (string.IsNullOrEmpty(cleanedScene) || string.IsNullOrEmpty(cleanedPath) || string.IsNullOrEmpty(cleanedType))
            {
                return "";
            }

            return cleanedScene + "|" + cleanedPath + "|" + cleanedType;
        }

        private bool RendererPreserveRuleHasAssetPredicates(RendererPreserveRule rule)
        {
            return rule != null &&
                (!string.IsNullOrEmpty(CleanUnityName(rule.SpriteName)) ||
                    !string.IsNullOrEmpty(CleanUnityName(rule.MaterialName)) ||
                    !string.IsNullOrEmpty(CleanUnityName(rule.TextureName)));
        }

        private bool RendererMatchesPreserveRule(RendererPreserveRule rule, Renderer renderer, string sceneName, string objectPath, string rendererType)
        {
            if (rule == null || string.IsNullOrEmpty(objectPath) || IsIgnoredRendererPreserveRule(rule))
            {
                return false;
            }

            string rulePath = CleanUnityPath(rule.ObjectPath);
            if (string.IsNullOrEmpty(rulePath))
            {
                return false;
            }

            if (rule.Projectile)
            {
                if (!ProjectilePathMatchesRule(objectPath, rulePath, false))
                {
                    return false;
                }
            }
            else if (!string.Equals(objectPath, rulePath, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string ruleType = CleanUnityName(rule.RendererType);
            if (!string.IsNullOrEmpty(ruleType) && !string.Equals(rendererType, ruleType, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string ruleScene = CleanUnityName(rule.SceneName);
            if (!string.IsNullOrEmpty(ruleScene) && !string.Equals(sceneName, ruleScene, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return RendererMatchesPreserveRuleAssetPredicates(rule, renderer);
        }

        private bool RendererMatchesPreserveRuleAssetPredicates(RendererPreserveRule rule, Renderer renderer)
        {
            string spriteName = CleanUnityName(rule != null ? rule.SpriteName : "");
            string materialName = CleanUnityName(rule != null ? rule.MaterialName : "");
            string textureName = CleanUnityName(rule != null ? rule.TextureName : "");
            if (string.IsNullOrEmpty(spriteName) && string.IsNullOrEmpty(materialName) && string.IsNullOrEmpty(textureName))
            {
                return true;
            }

            if (renderer == null)
            {
                return false;
            }

            bool spriteMatched = string.IsNullOrEmpty(spriteName);
            bool materialMatched = string.IsNullOrEmpty(materialName);
            bool textureMatched = string.IsNullOrEmpty(textureName);

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                if (!spriteMatched && string.Equals(GetObjectName(spriteRenderer.sprite), spriteName, System.StringComparison.OrdinalIgnoreCase))
                {
                    spriteMatched = true;
                }

                if (!textureMatched && string.Equals(GetObjectName(spriteRenderer.sprite.texture), textureName, System.StringComparison.OrdinalIgnoreCase))
                {
                    textureMatched = true;
                }
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null)
                    {
                        continue;
                    }

                    if (!materialMatched && string.Equals(GetObjectName(material), materialName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        materialMatched = true;
                    }

                    if (!textureMatched && string.Equals(GetObjectName(GetMaterialMainTexture(material)), textureName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        textureMatched = true;
                    }

                    if (spriteMatched && materialMatched && textureMatched)
                    {
                        return true;
                    }
                }
            }

            return spriteMatched && materialMatched && textureMatched;
        }

        private bool IsPreservedSprite(UnityEngine.Object obj)
        {
            return obj != null && IsPreservedSprite(obj.name);
        }

        private bool IsPreservedSprite(string name)
        {
            string cleaned = CleanUnityName(name);
            return !string.IsNullOrEmpty(cleaned) && preservedSpriteNames.Contains(cleaned);
        }

        private bool IsPreservedTexture(UnityEngine.Object obj)
        {
            return obj != null && IsPreservedTexture(obj.name);
        }

        private bool IsPreservedTexture(string name)
        {
            string cleaned = CleanUnityName(name);
            return !string.IsNullOrEmpty(cleaned) && preservedTextureNames.Contains(cleaned);
        }

        private bool IsPreservedMaterial(UnityEngine.Object obj)
        {
            return obj != null && IsPreservedMaterial(obj.name);
        }

        private bool IsPreservedMaterial(string name)
        {
            string cleaned = CleanUnityName(name);
            return !string.IsNullOrEmpty(cleaned) && preservedMaterialNames.Contains(cleaned);
        }

        private bool IsPreservedAssetName(UnityEngine.Object obj)
        {
            return obj != null && IsPreservedAssetName(obj.name);
        }

        private bool IsPreservedAssetName(string name)
        {
            string cleaned = CleanUnityName(name);
            return !string.IsNullOrEmpty(cleaned) && preservedAssetNames.Contains(cleaned);
        }

        private bool IsPreservedObjectPath(GameObject obj)
        {
            return obj != null && IsPreservedObjectPath(GetObjectPath(obj));
        }

        private bool IsPreservedObjectPath(string objectPath)
        {
            string cleanedPath = CleanUnityPath(objectPath);
            if (string.IsNullOrEmpty(cleanedPath))
            {
                return false;
            }

            return IsExactPreservedObjectPath(cleanedPath) ||
                IsGroupPreservedObjectPath(cleanedPath) ||
                IsProjectileObjectPath(cleanedPath);
        }

        private bool IsExactPreservedObjectPath(string cleanedPath)
        {
            return PathMatchesAnyRule(cleanedPath, preservedObjectPaths, false, false);
        }

        private bool IsGroupPreservedObjectPath(string cleanedPath)
        {
            return PathMatchesAnyRule(cleanedPath, preservedGroupObjectPaths, true, false);
        }

        private bool IsProjectileObjectPath(GameObject obj)
        {
            return obj != null && IsProjectileObjectPath(GetObjectPath(obj));
        }

        private bool IsProjectileObjectPath(string objectPath)
        {
            string cleanedPath = CleanUnityPath(objectPath);
            return !string.IsNullOrEmpty(cleanedPath) && PathMatchesAnyRule(cleanedPath, preservedProjectileObjectPaths, true, true);
        }

        private bool PathMatchesAnyRule(string cleanedPath, HashSet<string> rules, bool includeChildren, bool projectile)
        {
            if (string.IsNullOrEmpty(cleanedPath) || rules == null || rules.Count == 0)
            {
                return false;
            }

            foreach (string rule in rules)
            {
                if (projectile ? IsIgnoredProjectilePreservePath(rule) : IsIgnoredPreservedObjectPathRule(rule))
                {
                    continue;
                }

                string cleanedRule = CleanUnityPath(rule);
                if (string.IsNullOrEmpty(cleanedRule))
                {
                    continue;
                }

                if (string.Equals(cleanedPath, cleanedRule, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (includeChildren && cleanedPath.StartsWith(cleanedRule + "/", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (projectile && ProjectilePathMatchesRule(cleanedPath, cleanedRule, includeChildren))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ProjectilePathMatchesRule(string cleanedPath, string cleanedRule, bool includeChildren)
        {
            string familyPath = GetProjectileFamilyPath(cleanedPath);
            string familyRule = GetProjectileFamilyPath(cleanedRule);
            if (string.IsNullOrEmpty(familyPath) || string.IsNullOrEmpty(familyRule))
            {
                return false;
            }

            if (string.Equals(familyPath, familyRule, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return includeChildren && familyPath.StartsWith(familyRule + "/", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsIgnoredPreservedObjectPathRule(string preservedPath)
        {
            string cleanedPath = CleanUnityPath(preservedPath);
            if (string.IsNullOrEmpty(cleanedPath))
            {
                return true;
            }

            string root = GetFirstPathSegment(cleanedPath);
            return string.Equals(cleanedPath, "_Enemies", System.StringComparison.OrdinalIgnoreCase) ||
                root.StartsWith("sprite_set_", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsIgnoredProjectilePreservePath(string preservedPath)
        {
            string cleanedPath = CleanUnityPath(preservedPath);
            if (IsIgnoredPreservedObjectPathRule(cleanedPath))
            {
                return true;
            }

            return string.Equals(cleanedPath, "GG_Arena_Prefab/BG", System.StringComparison.OrdinalIgnoreCase) ||
                cleanedPath.StartsWith("GG_Arena_Prefab/BG/", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsIgnoredRendererPreserveRule(RendererPreserveRule rule)
        {
            if (rule == null)
            {
                return true;
            }

            return IsIgnoredRendererPreserveRule(rule.Projectile ? "projectileRenderer" : "renderer", rule.ObjectPath);
        }

        private bool IsIgnoredRendererPreserveRule(string ruleKind, string objectPath)
        {
            string cleanedPath = CleanUnityPath(objectPath);
            if (string.IsNullOrEmpty(cleanedPath) || IsIgnoredPreservedObjectPathRule(cleanedPath))
            {
                return true;
            }

            return string.Equals(ruleKind, "projectileRenderer", System.StringComparison.OrdinalIgnoreCase) &&
                IsIgnoredProjectilePreservePath(cleanedPath);
        }

        private void StripAllMaterials()
        {
            Shader basic = Shader.Find("Sprites/Default");
            if (basic == null)
            {
                return;
            }

            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsProxyRenderer(renderer))
                {
                    continue;
                }

                if (renderer.gameObject == null ||
                    renderer.gameObject.layer < 0 ||
                    renderer.gameObject.layer > 31 ||
                    (visibleMask & (1 << renderer.gameObject.layer)) == 0)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (ShouldSkipMaterialDuringStrip(renderer, material, basic))
                    {
                        continue;
                    }

                    if (!strippedShaders.ContainsKey(material))
                    {
                        strippedShaders[material] = material.shader;
                    }

                    material.shader = basic;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private bool ShouldSkipMaterialDuringStrip(Renderer renderer, Material material, Shader basic)
        {
            return material == null ||
                material.shader == basic ||
                IsVisualOverrideOwnedMaterial(material) ||
                ShouldPreserveMaterial(renderer, material);
        }

        private void RestoreShaders()
        {
            foreach (KeyValuePair<Material, Shader> pair in strippedShaders)
            {
                if (pair.Key != null)
                {
                    pair.Key.shader = pair.Value;
                }
            }

            strippedShaders.Clear();
        }
    }
}

