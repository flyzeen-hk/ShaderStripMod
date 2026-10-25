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
        private string BuildTextureBrowserInfo(TextureBrowserEntry entry)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Name: " + entry.Name);
            builder.AppendLine("Kind: " + entry.Kind);
            builder.AppendLine("Preserved: " + (IsTextureBrowserEntryPreserved(entry) ? "yes" : "no"));
            var mask = GetSpriteMask(entry.Renderer);
            builder.AppendLine("Sprite area mask: " + (mask == null ? "none" : (mask.Enabled ? "enabled" : "disabled")));
            if (mask != null) builder.AppendLine("Mask points: " + mask.Points.Count + "; edit in the Mask tab.");
            builder.AppendLine("Sprite rule: " + (IsEntrySpritePreserved(entry) ? "yes" : "no"));
            builder.AppendLine("Texture rule: " + (IsEntryTexturePreserved(entry) ? "yes" : "no"));
            builder.AppendLine("Material rule: " + (IsEntryMaterialPreserved(entry) ? "yes" : "no"));
            builder.AppendLine("Object rule: " + (IsEntryObjectPreserved(entry) ? "yes" : "no"));
            builder.AppendLine("Group rule: " + (IsEntryGroupPreserved(entry) ? "yes" : "no"));
            builder.AppendLine("Projectile rule: " + (IsEntryProjectilePreserved(entry) ? "yes" : "no"));
            builder.AppendLine("Renderer rule: " + (IsEntryRendererPreserved(entry, false) ? "yes" : "no"));
            builder.AppendLine("Projectile renderer rule: " + (IsEntryRendererPreserved(entry, true) ? "yes" : "no"));
            builder.AppendLine("Export folder mode: " + exportFolderMode);
            builder.AppendLine("Export folder: " + exportDirectory);
            if (!string.IsNullOrEmpty(lastTextureExportMessage))
            {
                builder.AppendLine("Last export: " + lastTextureExportMessage);
            }

            if (!string.IsNullOrEmpty(entry.PreservePath))
            {
                builder.AppendLine("Preserve path: " + entry.PreservePath);
            }

            string projectilePath = GetEntryProjectilePreservePath(entry);
            if (!string.IsNullOrEmpty(projectilePath) && !string.Equals(projectilePath, entry.PreservePath, System.StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendLine("Projectile path: " + projectilePath);
            }

            if (entry.HasDistance)
            {
                builder.AppendLine("Distance: " + entry.Distance.ToString("0.00"));
                builder.AppendLine("Scene: " + entry.SceneName);
                builder.AppendLine("Object: " + entry.ObjectPath);
                builder.AppendLine("Layer: " + entry.Layer);
                builder.AppendLine("Renderer: " + entry.RendererType);
                builder.AppendLine("Position: " + FormatVector3(entry.Position));
            }

            builder.AppendLine("Preview texture: " + GetObjectName(entry.Texture));
            if (entry.Texture != null)
            {
                builder.AppendLine("Texture size: " + GetTextureSize(entry.Texture));
            }

            if (entry.AtlasSource)
            {
                builder.AppendLine("Atlas source: yes");
                builder.AppendLine("Atlas score: " + entry.AtlasScore);
            }

            if (entry.Sprite != null)
            {
                builder.AppendLine("Sprite texture: " + GetObjectName(entry.Sprite.texture));
                builder.AppendLine("Sprite rect: " + entry.Sprite.rect);
            }

            if (entry.Material != null)
            {
                builder.AppendLine("Material texture: " + GetObjectName(GetMaterialMainTexture(entry.Material)));
                builder.AppendLine("Shader: " + (entry.Material.shader != null ? CleanUnityName(entry.Material.shader.name) : ""));
            }

            if (!string.IsNullOrEmpty(entry.Details))
            {
                builder.AppendLine();
                builder.AppendLine(entry.Details);
            }

            if (entry.Renderer != null || IsProjectileEntry(entry))
            {
                builder.AppendLine();
                builder.Append(BuildProjectileDiagnostics(entry));
            }

            builder.AppendLine();
            builder.Append(BuildVisualOverrideDiagnostics(entry));

            builder.AppendLine();
            builder.AppendLine("Preserve file:");
            builder.AppendLine(preserveFilePath);
            return builder.ToString();
        }

        private string BuildProjectileDiagnostics(TextureBrowserEntry entry)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Projectile diagnostics:");
            builder.AppendLine("Projectile entry: " + FormatBool(IsProjectileEntry(entry)));
            builder.AppendLine("Preserved by: " + BuildEntryPreserveReasonList(entry));
            builder.AppendLine("Active scene: " + GetActiveSceneName());
            builder.AppendLine("Preserved sync interval: " + FormatSettingsFloat(GetPreservedRendererSyncInterval()) + "s");
            builder.AppendLine("Draw source cache count: " + preservedRendererDrawSources.Count + " / " + maxPreservedRenderers);
            builder.AppendLine("CommandBuffer cameras: " + preservedRendererCommandBuffers.Count);

            Renderer renderer = entry != null ? entry.Renderer : null;
            if (renderer == null || renderer.gameObject == null)
            {
                builder.AppendLine("Live renderer: no");
                builder.AppendLine("Skip reason: no live renderer reference in this browser entry");
                return builder.ToString();
            }

            string sceneName = GetActiveSceneName();
            bool sceneMatch = IsRendererInCurrentScene(renderer, sceneName);
            bool hasMaterials = RendererHasMaterials(renderer);
            bool canDraw = CanDrawPreservedRenderer(renderer, sceneName);

            builder.AppendLine("Live renderer: yes");
            builder.AppendLine("Renderer instance: " + renderer.GetInstanceID());
            builder.AppendLine("Renderer type: " + renderer.GetType().Name);
            builder.AppendLine("Renderer enabled: " + FormatBool(renderer.enabled));
            builder.AppendLine("Object active: " + FormatBool(renderer.gameObject.activeInHierarchy));
            builder.AppendLine("Renderer scene: " + GetRendererSceneName(renderer));
            builder.AppendLine("Scene match: " + FormatBool(sceneMatch));
            builder.AppendLine("Layer: " + GetLayerLabel(renderer.gameObject));
            builder.AppendLine("Layer already visible: " + FormatBool(IsRendererLayerAlreadyVisible(renderer)));
            builder.AppendLine("Particle renderer: " + FormatBool(IsParticleSystemRenderer(renderer)));
            builder.AppendLine("Native pipeline: " + FormatBool(IsRendererLayerAlreadyVisible(renderer) || IsRendererPromotedByMod(renderer)));
            builder.AppendLine("Promoted by mod: " + FormatBool(IsRendererPromotedByMod(renderer)));
            builder.AppendLine("Projectile candidate: " + FormatBool(IsProjectileCandidateRenderer(renderer)));
            builder.AppendLine("Projectile path match: " + FormatBool(RendererHasProjectilePath(renderer)));
            builder.AppendLine("Renderer preserved: " + FormatBool(RendererIsPreserved(renderer)));
            builder.AppendLine("Expensive skip: " + FormatBool(IsExpensivePreservedRenderer(renderer)));
            builder.AppendLine("Has materials: " + FormatBool(hasMaterials));
            builder.AppendLine("Can draw: " + FormatBool(canDraw));
            builder.AppendLine("In draw source cache: " + FormatBool(preservedRendererDrawSources.Contains(renderer)));
            builder.AppendLine("Proxy exists: " + FormatBool(preservedRendererProxies.ContainsKey(renderer)));
            builder.AppendLine("Skip reason: " + GetPreservedRendererSkipReason(renderer, sceneName));
            return builder.ToString();
        }

        private string BuildEntryPreserveReasonList(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return "none";
            }

            StringBuilder builder = new StringBuilder();
            AppendPreserveReason(builder, "projectileRenderer", IsEntryRendererPreserved(entry, true));
            AppendPreserveReason(builder, "renderer", IsEntryRendererPreserved(entry, false));
            AppendPreserveReason(builder, "projectilePath", IsEntryProjectilePreserved(entry));
            AppendPreserveReason(builder, "path", IsEntryObjectPreserved(entry));
            AppendPreserveReason(builder, "groupPath", IsEntryGroupPreserved(entry));
            AppendPreserveReason(builder, "sprite", IsEntrySpritePreserved(entry));
            AppendPreserveReason(builder, "texture", IsEntryTexturePreserved(entry));
            AppendPreserveReason(builder, "material", IsEntryMaterialPreserved(entry));
            AppendPreserveReason(builder, "asset", IsEntryLegacyAssetPreserved(entry));
            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private void AppendPreserveReason(StringBuilder builder, string reason, bool active)
        {
            if (builder == null || string.IsNullOrEmpty(reason) || !active)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(reason);
        }

        private bool IsRendererInCurrentScene(Renderer renderer, string sceneName)
        {
            if (renderer == null || renderer.gameObject == null)
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

        private bool RendererHasMaterials(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            return materials != null && materials.Length > 0;
        }

        private string GetPreservedRendererSkipReason(Renderer renderer, string sceneName)
        {
            if (renderer == null)
            {
                return "renderer is missing";
            }

            if (renderer.gameObject == null)
            {
                return "renderer has no GameObject";
            }

            if (IsProxyRenderer(renderer))
            {
                return "renderer is ShaderStripMod proxy";
            }

            if (!renderer.enabled)
            {
                return "renderer disabled";
            }

            if (!renderer.gameObject.activeInHierarchy)
            {
                return "object inactive";
            }

            if (IsRendererLayerAlreadyVisible(renderer) && !IsRendererPromotedByMod(renderer))
            {
                return "renderer layer is already visible to the main camera";
            }

            if (IsExpensivePreservedRenderer(renderer))
            {
                return "particle renderer without projectile path/renderer rule";
            }

            if (!IsRendererInCurrentScene(renderer, sceneName))
            {
                return "scene mismatch or non-gameplay scene";
            }

            if (!RendererIsPreserved(renderer))
            {
                return "no matching preserve rule";
            }

            if (!RendererHasMaterials(renderer))
            {
                return "renderer has no shared materials";
            }

            return "none";
        }

        private string FormatBool(bool value)
        {
            return value ? "yes" : "no";
        }

        private bool IsTextureBrowserEntryPreserved(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            return HasActiveSpriteMask(entry.Renderer) || IsEntryObjectPreserved(entry) ||
                IsEntryGroupPreserved(entry) ||
                IsEntryProjectilePreserved(entry) ||
                IsEntryRendererPreserved(entry, false) ||
                IsEntryRendererPreserved(entry, true) ||
                IsEntrySpritePreserved(entry) ||
                IsEntryTexturePreserved(entry) ||
                IsEntryMaterialPreserved(entry) ||
                IsEntryLegacyAssetPreserved(entry);
        }

        private bool IsEntryRendererPreserved(TextureBrowserEntry entry, bool projectile)
        {
            RendererPreserveRule rule = BuildRendererPreserveRule(entry, projectile);
            string key = GetRendererPreserveRuleKey(rule);
            return !string.IsNullOrEmpty(key) && preservedRendererRuleKeys.Contains(key);
        }

        private bool IsEntryObjectPreserved(TextureBrowserEntry entry)
        {
            string path = GetEntryPreservePath(entry);
            return !string.IsNullOrEmpty(path) && preservedObjectPaths.Contains(path);
        }

        private bool IsEntryGroupPreserved(TextureBrowserEntry entry)
        {
            string path = GetEntryPreservePath(entry);
            return !string.IsNullOrEmpty(path) && preservedGroupObjectPaths.Contains(path);
        }

        private bool IsEntryProjectilePreserved(TextureBrowserEntry entry)
        {
            string path = GetEntryProjectilePreservePath(entry);
            return !string.IsNullOrEmpty(path) && preservedProjectileObjectPaths.Contains(path);
        }

        private bool IsEntrySpritePreserved(TextureBrowserEntry entry)
        {
            string name = GetEntrySpriteName(entry);
            return !string.IsNullOrEmpty(name) && preservedSpriteNames.Contains(name);
        }

        private bool IsEntryTexturePreserved(TextureBrowserEntry entry)
        {
            string name = GetEntryTextureName(entry);
            return !string.IsNullOrEmpty(name) && preservedTextureNames.Contains(name);
        }

        private bool IsEntryMaterialPreserved(TextureBrowserEntry entry)
        {
            string name = GetEntryMaterialName(entry);
            return !string.IsNullOrEmpty(name) && preservedMaterialNames.Contains(name);
        }

        private bool IsEntryLegacyAssetPreserved(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            return IsPreservedAssetName(entry.Name) ||
                IsPreservedAssetName(GetEntrySpriteName(entry)) ||
                IsPreservedAssetName(GetEntryTextureName(entry)) ||
                IsPreservedAssetName(GetEntryMaterialName(entry));
        }

        private string GetEntryPreservePath(TextureBrowserEntry entry)
        {
            return entry != null ? CleanUnityPath(entry.PreservePath) : "";
        }

        private string GetEntryProjectilePreservePath(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return "";
            }

            string path = !string.IsNullOrEmpty(entry.ObjectPath) ? entry.ObjectPath : entry.PreservePath;
            string projectilePath = entry.Renderer != null && entry.Renderer.gameObject != null
                ? GetProjectileRootPath(entry.Renderer.gameObject)
                : GetProjectileRootPath(path, entry.Layer);
            if (!string.IsNullOrEmpty(projectilePath))
            {
                return projectilePath;
            }

            if (IsProjectileEntry(entry))
            {
                return GetEntryPreservePath(entry);
            }

            return "";
        }

        private string GetEntrySpriteName(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return "";
            }

            if (entry.Sprite != null)
            {
                return CleanUnityName(entry.Sprite.name);
            }

            if (!string.IsNullOrEmpty(entry.Kind) && entry.Kind.IndexOf("Sprite", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CleanUnityName(entry.Name);
            }

            return "";
        }

        private string GetEntryTextureName(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return "";
            }

            string name = GetObjectName(entry.Texture);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (entry.Sprite != null)
            {
                return GetObjectName(entry.Sprite.texture);
            }

            return GetObjectName(entry.Material != null ? GetMaterialMainTexture(entry.Material) : null);
        }

        private string GetEntryMaterialName(TextureBrowserEntry entry)
        {
            return entry != null && entry.Material != null ? CleanUnityName(entry.Material.name) : "";
        }

        private RendererPreserveRule BuildRendererPreserveRule(TextureBrowserEntry entry, bool projectile)
        {
            if (entry == null)
            {
                return null;
            }

            if (entry.Renderer != null)
            {
                return BuildRendererPreserveRule(entry.Renderer, projectile);
            }

            string path = !string.IsNullOrEmpty(entry.ObjectPath) ? entry.ObjectPath : entry.PreservePath;
            RendererPreserveRule rule = new RendererPreserveRule();
            rule.Projectile = projectile || IsProjectileEntry(entry);
            rule.SceneName = entry.SceneName;
            rule.ObjectPath = path;
            rule.RendererType = entry.RendererType;
            if (!rule.Projectile)
            {
                rule.SpriteName = GetEntrySpriteName(entry);
                rule.MaterialName = GetEntryMaterialName(entry);
                rule.TextureName = GetEntryTextureName(entry);
            }
            NormalizeRendererPreserveRule(rule);
            return rule;
        }

        private RendererPreserveRule BuildRendererPreserveRule(Renderer renderer, bool projectile)
        {
            if (renderer == null || renderer.gameObject == null)
            {
                return null;
            }

            RendererPreserveRule rule = new RendererPreserveRule();
            rule.Projectile = projectile || IsProjectileCandidateRenderer(renderer);
            rule.SceneName = GetRendererSceneName(renderer);
            rule.ObjectPath = GetObjectPath(renderer.gameObject);
            rule.RendererType = renderer.GetType().Name;

            if (!rule.Projectile)
            {
                SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    rule.SpriteName = GetObjectName(spriteRenderer.sprite);
                    rule.TextureName = GetObjectName(spriteRenderer.sprite.texture);
                }

                Material material = GetFirstRendererMaterial(renderer);
                if (material != null)
                {
                    rule.MaterialName = GetObjectName(material);
                    if (string.IsNullOrEmpty(rule.TextureName))
                    {
                        rule.TextureName = GetObjectName(GetMaterialMainTexture(material));
                    }
                }
            }

            NormalizeRendererPreserveRule(rule);
            return rule;
        }

        private Material GetFirstRendererMaterial(Renderer renderer)
        {
            if (renderer == null)
            {
                return null;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null)
            {
                return null;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                {
                    return materials[i];
                }
            }

            return null;
        }

        private bool IsProjectileEntry(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            return ContainsProjectileHint(entry.Kind) ||
                ContainsProjectileHint(entry.Name) ||
                ContainsProjectileHint(entry.ObjectPath) ||
                ContainsProjectileHint(entry.PreservePath) ||
                IsProjectileLayerLabel(entry.Layer);
        }

        private bool ContainsProjectileHint(string value)
        {
            return ContainsAny(value, ProjectileNameHints);
        }

        private bool IsProjectileCandidateRenderer(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject == null || IsHeroEntityPath(GetObjectPath(renderer.gameObject)))
            {
                return false;
            }

            string path = GetObjectPath(renderer.gameObject);
            if (IsIgnoredProjectilePreservePath(path))
            {
                return false;
            }

            Transform current = renderer.transform;
            while (current != null)
            {
                GameObject currentObject = current.gameObject;
                if (currentObject != null &&
                    (IsProjectileLayerName(LayerMask.LayerToName(currentObject.layer)) || ContainsProjectileHint(currentObject.name)))
                {
                    return true;
                }

                current = current.parent;
            }

            return ContainsProjectileHint(path);
        }

        private bool IsProjectileLayerName(string layerName)
        {
            return string.Equals(layerName, "Attack", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(layerName, "Enemy Attack", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsProjectileLayerLabel(string layer)
        {
            string cleaned = CleanUnityName(layer);
            return string.Equals(cleaned, "Attack", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "Enemy Attack", System.StringComparison.OrdinalIgnoreCase) ||
                cleaned.StartsWith("Attack(", System.StringComparison.OrdinalIgnoreCase) ||
                cleaned.StartsWith("Enemy Attack(", System.StringComparison.OrdinalIgnoreCase);
        }

        private string GetProjectileRootPath(string objectPath, string layer)
        {
            string cleanedPath = CleanUnityPath(objectPath);
            if (string.IsNullOrEmpty(cleanedPath) || IsHeroEntityPath(cleanedPath) || IsIgnoredProjectilePreservePath(cleanedPath))
            {
                return "";
            }

            string[] parts = cleanedPath.Split('/');
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('/');
                }

                builder.Append(parts[i]);
                if (ContainsProjectileHint(parts[i]))
                {
                    string projectilePath = builder.ToString();
                    projectilePath = GetProjectileFamilyPath(projectilePath);
                    return IsIgnoredProjectilePreservePath(projectilePath) ? "" : projectilePath;
                }
            }

            if (IsProjectileLayerLabel(layer))
            {
                cleanedPath = GetProjectileFamilyPath(cleanedPath);
                return IsIgnoredProjectilePreservePath(cleanedPath) ? "" : cleanedPath;
            }

            return "";
        }

        private string GetProjectileRootPath(GameObject obj)
        {
            GameObject root = GetProjectileRootObject(obj);
            if (root == null)
            {
                return "";
            }

            string path = GetProjectileFamilyPath(GetObjectPath(root));
            return IsIgnoredProjectilePreservePath(path) ? "" : path;
        }

        private GameObject GetProjectileRootObject(GameObject obj)
        {
            if (obj == null || IsHeroEntityPath(GetObjectPath(obj)))
            {
                return null;
            }

            List<Transform> hierarchy = new List<Transform>();
            Transform current = obj.transform;
            while (current != null)
            {
                hierarchy.Add(current);
                current = current.parent;
            }

            for (int i = hierarchy.Count - 1; i >= 0; i--)
            {
                Transform candidate = hierarchy[i];
                if (candidate == null || candidate.gameObject == null)
                {
                    continue;
                }

                GameObject candidateObject = candidate.gameObject;
                if (ContainsProjectileHint(candidateObject.name) ||
                    IsProjectileLayerName(LayerMask.LayerToName(candidateObject.layer)))
                {
                    return candidateObject;
                }
            }

            return null;
        }

        private void ToggleRendererPreserveRule(RendererPreserveRule rule, bool preserve)
        {
            string key = GetRendererPreserveRuleKey(rule);
            if (string.IsNullOrEmpty(key))
            {
                lastTextureExportMessage = "Renderer rule failed: no renderer path.";
                return;
            }

            if (preserve)
            {
                AddRendererPreserveRule(rule);
            }
            else
            {
                RemoveRendererPreserveRule(key);
            }

            RefreshAfterBrowserPreserveChange();
        }

        private int AddRendererRulesUnderEntryPath(TextureBrowserEntry entry, bool projectile)
        {
            string rootPath = projectile ? GetEntryProjectilePreservePath(entry) : GetEntryPreservePath(entry);
            if (string.IsNullOrEmpty(rootPath))
            {
                return 0;
            }

            string sceneName = GetActiveSceneName();
            int added = 0;
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.gameObject == null || IsProxyRenderer(renderer))
                {
                    continue;
                }

                bool rendererInScope = projectile
                    ? IsRendererInCurrentScene(renderer, sceneName)
                    : IsRoomRenderer(renderer, sceneName);
                if (!rendererInScope)
                {
                    continue;
                }

                if (IsParticleSystemRenderer(renderer) && !projectile)
                {
                    continue;
                }

                string rendererPath = CleanUnityPath(GetObjectPath(renderer.gameObject));
                bool pathMatched = projectile
                    ? ProjectilePathMatchesRule(rendererPath, rootPath, true)
                    : string.Equals(rendererPath, rootPath, System.StringComparison.OrdinalIgnoreCase) ||
                        rendererPath.StartsWith(rootPath + "/", System.StringComparison.OrdinalIgnoreCase);
                if (!pathMatched)
                {
                    continue;
                }

                if (AddRendererPreserveRule(BuildRendererPreserveRule(renderer, projectile)))
                {
                    added++;
                }
            }

            if (added > 0)
            {
                RefreshAfterBrowserPreserveChange();
            }

            return added;
        }

        private void SetPreserveNameRule(HashSet<string> rules, string name, bool preserve)
        {
            string cleaned = CleanUnityName(name);
            if (string.IsNullOrEmpty(cleaned))
            {
                return;
            }

            if (preserve)
            {
                rules.Add(cleaned);
            }
            else
            {
                rules.Remove(cleaned);
            }

            RefreshAfterBrowserPreserveChange();
        }

        private void SetPreservePathRule(HashSet<string> rules, string path, bool preserve)
        {
            string cleanedPath = CleanUnityPath(path);
            if (string.IsNullOrEmpty(cleanedPath))
            {
                return;
            }

            if (preserve)
            {
                if (rules == preservedProjectileObjectPaths && IsIgnoredProjectilePreservePath(cleanedPath))
                {
                    Log("ShaderStripMod ignored projectile preserve path from browser: " + cleanedPath + ".");
                    return;
                }

                if (IsIgnoredPreservedObjectPathRule(cleanedPath))
                {
                    Log("ShaderStripMod ignored broad preserve path from browser: " + cleanedPath + ".");
                    return;
                }

                rules.Add(cleanedPath);
            }
            else
            {
                rules.Remove(cleanedPath);
            }

            RefreshAfterBrowserPreserveChange();
        }

        private void ClearPreserveRulesFromBrowser(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            var maskRule = GetSpriteMask(entry.Renderer);
            if (maskRule != null) RemoveSpriteMask(maskRule);

            string path = GetEntryPreservePath(entry);
            if (!string.IsNullOrEmpty(path))
            {
                preservedObjectPaths.Remove(path);
                preservedGroupObjectPaths.Remove(path);
                preservedProjectileObjectPaths.Remove(path);
                RemoveRendererRulesForPath(path);
            }

            string projectilePath = GetEntryProjectilePreservePath(entry);
            if (!string.IsNullOrEmpty(projectilePath) && !string.Equals(projectilePath, path, System.StringComparison.OrdinalIgnoreCase))
            {
                preservedProjectileObjectPaths.Remove(projectilePath);
                RemoveRendererRulesForPath(projectilePath);
            }

            RendererPreserveRule rendererRule = BuildRendererPreserveRule(entry, false);
            string rendererKey = GetRendererPreserveRuleKey(rendererRule);
            if (!string.IsNullOrEmpty(rendererKey))
            {
                RemoveRendererPreserveRule(rendererKey);
            }

            RendererPreserveRule projectileRule = BuildRendererPreserveRule(entry, true);
            string projectileKey = GetRendererPreserveRuleKey(projectileRule);
            if (!string.IsNullOrEmpty(projectileKey))
            {
                RemoveRendererPreserveRule(projectileKey);
            }

            string sprite = GetEntrySpriteName(entry);
            if (!string.IsNullOrEmpty(sprite))
            {
                preservedSpriteNames.Remove(sprite);
                preservedAssetNames.Remove(sprite);
            }

            string texture = GetEntryTextureName(entry);
            if (!string.IsNullOrEmpty(texture))
            {
                preservedTextureNames.Remove(texture);
                preservedAssetNames.Remove(texture);
            }

            string material = GetEntryMaterialName(entry);
            if (!string.IsNullOrEmpty(material))
            {
                preservedMaterialNames.Remove(material);
                preservedAssetNames.Remove(material);
            }

            string name = CleanUnityName(entry.Name);
            if (!string.IsNullOrEmpty(name))
            {
                preservedAssetNames.Remove(name);
            }

            RefreshAfterBrowserPreserveChange();
        }

        private void RemoveRendererRulesForPath(string path)
        {
            string cleanedPath = CleanUnityPath(path);
            if (string.IsNullOrEmpty(cleanedPath) || preservedRendererRules.Count == 0)
            {
                return;
            }

            bool removed = false;
            for (int i = preservedRendererRules.Count - 1; i >= 0; i--)
            {
                RendererPreserveRule rule = preservedRendererRules[i];
                string rulePath = rule != null ? CleanUnityPath(rule.ObjectPath) : "";
                if (string.Equals(rulePath, cleanedPath, System.StringComparison.OrdinalIgnoreCase) ||
                    rulePath.StartsWith(cleanedPath + "/", System.StringComparison.OrdinalIgnoreCase))
                {
                    preservedRendererRuleKeys.Remove(GetRendererPreserveRuleKey(rule));
                    preservedRendererRules.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                preservedRendererRuleIndexDirty = true;
            }
        }

        private void RefreshAfterBrowserPreserveChange()
        {
            SavePreserveList();
            RestoreShaders();
            RestorePromotedEffects();
            RestorePreservedRendererDraws();
            RestorePreservedRendererProxies();
            MarkPreservedRendererCacheDirty();
            if (visualsActive)
            {
                ApplyStrip();
            }
        }

    }
}

