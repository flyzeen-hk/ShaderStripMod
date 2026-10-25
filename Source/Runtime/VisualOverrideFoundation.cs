using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ShaderStripMod.Runtime;
using ShaderStripMod.VisualRules;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private const string VisualRulesFileName = "ShaderStripMod.visual-rules.txt";
        private const string ReplacementsDirectoryName = "replacements";

        private VisualRuleRuntimeManager visualRuleRuntimeManager;
        private string visualRulesFilePath = "";
        private string visualRulesBackupDirectory = "";
        private string replacementsDirectory = "";
        private string lastVisualRulesMessage = "Visual-rules foundation is not initialized.";
        private bool visualOverridesEnabled;
        private bool colorOverridesEnabled;
        private bool textureReplacementsEnabled;
        private bool staticDisplayEnabled;
        private bool visualOverrideRuntimeProbeLogged;

        private void InitializeVisualOverridesFoundation()
        {
            try
            {
                string baseDirectory = GetVisualOverrideBaseDirectory();
                visualRulesFilePath = Path.Combine(baseDirectory, VisualRulesFileName);
                replacementsDirectory = Path.Combine(baseDirectory, ReplacementsDirectoryName);
                visualRulesBackupDirectory = Path.Combine(replacementsDirectory, "backups");
                visualRuleRuntimeManager = new VisualRuleRuntimeManager(visualRulesFilePath, visualRulesBackupDirectory);

                VisualRuleLoadState state = visualRuleRuntimeManager.Initialize();
                lastVisualRulesMessage = state.Message;
                Log("ShaderStripMod visual-rules foundation initialized: " + state.Message + " File=" + visualRulesFilePath + ".");
            }
            catch (Exception ex)
            {
                visualRuleRuntimeManager = null;
                lastVisualRulesMessage = "Visual-rules foundation initialization failed: " + ex.Message;
                Log("ShaderStripMod " + lastVisualRulesMessage);
            }
        }

        private string GetVisualOverrideBaseDirectory()
        {
            string path = !string.IsNullOrEmpty(settingsFilePath) ? settingsFilePath : preserveFilePath;
            string directory = !string.IsNullOrEmpty(path) ? Path.GetDirectoryName(path) : "";
            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.GetDirectoryName(typeof(ShaderStripMod).Assembly.Location);
            }

            return string.IsNullOrEmpty(directory) ? Environment.CurrentDirectory : directory;
        }

        private void ReloadVisualRulesFoundation()
        {
            if (visualRuleRuntimeManager == null)
            {
                InitializeVisualOverridesFoundation();
                return;
            }

            try
            {
                VisualRuleLoadState state = visualRuleRuntimeManager.Reload();
                lastVisualRulesMessage = state.Message;
                Log("ShaderStripMod visual rules reloaded: " + state.Message);
            }
            catch (Exception ex)
            {
                lastVisualRulesMessage = "Visual-rules reload failed: " + ex.Message;
                Log("ShaderStripMod " + lastVisualRulesMessage);
            }
        }

        private void InvalidateVisualOverrideResolutionCache()
        {
            if (visualRuleRuntimeManager != null)
            {
                visualRuleRuntimeManager.InvalidateResolutionCache();
            }
        }

        private void OnVisualOverrideFoundationSceneChanged()
        {
            visualOverrideRuntimeProbeLogged = false;
            InvalidateVisualOverrideResolutionCache();
        }

        private RendererIdentity BuildRendererIdentityFromRuntime(Renderer renderer, int materialIndex)
        {
            if (renderer == null || renderer.gameObject == null)
            {
                return null;
            }

            RendererIdentityInput input = new RendererIdentityInput();
            input.SceneName = GetRendererSceneName(renderer);
            input.RendererType = renderer.GetType().Name;
            input.RendererComponentIndex = GetRendererComponentIndex(renderer);
            input.MaterialIndex = materialIndex;
            input.ProjectileFamily = IsProjectileCandidateRenderer(renderer)
                ? GetProjectileFamilyPath(GetObjectPath(renderer.gameObject))
                : "";
            input.IsPreserved = RendererIsPreserved(renderer);

            List<Transform> hierarchy = new List<Transform>();
            Transform current = renderer.transform;
            while (current != null)
            {
                hierarchy.Add(current);
                current = current.parent;
            }

            for (int i = hierarchy.Count - 1; i >= 0; i--)
            {
                Transform segment = hierarchy[i];
                input.Hierarchy.Add(new RendererHierarchySegment
                {
                    Name = segment.name,
                    SiblingIndex = segment.GetSiblingIndex()
                });
            }

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            Texture spriteTexture = sprite != null ? sprite.texture : null;
            input.SpriteName = GetObjectName(sprite);

            Material material = GetRendererMaterialAt(renderer, materialIndex);
            input.MaterialName = GetObjectName(material);
            Texture materialTexture;
            input.TexturePropertyName = GetFirstMaterialTextureProperty(material, out materialTexture);
            Texture identityTexture = materialTexture != null ? materialTexture : spriteTexture;
            input.TextureName = GetObjectName(identityTexture);
            if (identityTexture != null)
            {
                input.TextureWidth = identityTexture.width;
                input.TextureHeight = identityTexture.height;
            }

            return RendererIdentityBuilder.Build(input);
        }

        private int GetRendererComponentIndex(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject == null)
            {
                return 0;
            }

            Component[] components = renderer.gameObject.GetComponents(renderer.GetType());
            if (components == null)
            {
                return 0;
            }

            for (int i = 0; i < components.Length; i++)
            {
                if (ReferenceEquals(components[i], renderer))
                {
                    return i;
                }
            }

            return 0;
        }

        private Material GetRendererMaterialAt(Renderer renderer, int materialIndex)
        {
            if (renderer == null)
            {
                return null;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return null;
            }

            if (materialIndex >= 0 && materialIndex < materials.Length)
            {
                return materials[materialIndex];
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

        private int GetEntryMaterialIndex(TextureBrowserEntry entry)
        {
            if (entry == null || entry.Renderer == null)
            {
                return -1;
            }

            Material[] materials = entry.Renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                if (ReferenceEquals(materials[i], entry.Material))
                {
                    return i;
                }
            }

            return 0;
        }

        private string GetFirstMaterialTextureProperty(Material material, out Texture texture)
        {
            texture = null;
            if (material == null)
            {
                return "";
            }

            for (int i = 0; i < MaterialTextureProperties.Length; i++)
            {
                try
                {
                    string propertyName = MaterialTextureProperties[i];
                    if (!material.HasProperty(propertyName))
                    {
                        continue;
                    }

                    Texture candidate = material.GetTexture(propertyName);
                    if (candidate != null)
                    {
                        texture = candidate;
                        return propertyName;
                    }
                }
                catch
                {
                }
            }

            return "";
        }

        private string BuildVisualOverrideDiagnostics(TextureBrowserEntry entry)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Visual overrides (read-only foundation):");
            builder.AppendLine("Master enabled: " + FormatBool(visualOverridesEnabled));
            builder.AppendLine("Color channel enabled: " + FormatBool(colorOverridesEnabled));
            builder.AppendLine("Replacement channel enabled: " + FormatBool(textureReplacementsEnabled));
            builder.AppendLine("Static-display channel enabled: " + FormatBool(staticDisplayEnabled));
            builder.AppendLine("Runtime mutation: disabled in Parts 1-2");
            builder.AppendLine("Rules file: " + visualRulesFilePath);
            builder.AppendLine("Replacement root (lazy): " + replacementsDirectory);
            builder.AppendLine("Load state: " + lastVisualRulesMessage);

            if (visualRuleRuntimeManager == null)
            {
                builder.AppendLine("Resolver: unavailable");
                return builder.ToString();
            }

            builder.AppendLine("Active rules: " + visualRuleRuntimeManager.ActiveRuleCount);
            builder.AppendLine("Diagnostics: " + visualRuleRuntimeManager.DiagnosticCount);
            Renderer renderer = entry != null ? entry.Renderer : null;
            RendererIdentity identity = BuildRendererIdentityFromRuntime(renderer, GetEntryMaterialIndex(entry));
            if (identity == null)
            {
                builder.AppendLine("Live identity: unavailable for this entry");
            }
            else
            {
                ResolvedVisualOverride resolved = visualRuleRuntimeManager.Resolve(identity);
                builder.AppendLine("Identity: " + identity.BuildStableRendererKey());
                AppendResolvedChannelDiagnostics(builder, "Color", resolved.Color);
                AppendResolvedChannelDiagnostics(builder, "Replacement", resolved.Replacement);
                AppendResolvedChannelDiagnostics(builder, "Static display", resolved.StaticDisplay);
            }

            builder.AppendLine("Resolution cache: entries=" + visualRuleRuntimeManager.ResolutionCacheCount +
                ", hits=" + visualRuleRuntimeManager.ResolutionCacheHits +
                ", misses=" + visualRuleRuntimeManager.ResolutionCacheMisses +
                ", evictions=" + visualRuleRuntimeManager.ResolutionCacheEvictions);
            return builder.ToString();
        }

        private static void AppendResolvedChannelDiagnostics(StringBuilder builder, string label, ResolvedVisualChannel channel)
        {
            string result = channel.BlockingRule != null
                ? "blocked by " + channel.BlockingRule.Id
                : (channel.Winner != null ? "winner " + channel.Winner.Id : "no winner");
            builder.AppendLine(label + ": " + result + "; candidates=" + channel.Decisions.Count);
            for (int i = 0; i < channel.Decisions.Count; i++)
            {
                VisualRuleMatchDecision decision = channel.Decisions[i];
                builder.AppendLine("  - " + (decision.Rule != null ? decision.Rule.Id : "<null>") + ": " + decision.Reason);
            }
        }

        private void ProbeVisualOverridesFoundation(HeroController hero)
        {
            if (visualOverrideRuntimeProbeLogged || hero == null || visualRuleRuntimeManager == null)
            {
                return;
            }

            try
            {
                SpriteRenderer[] renderers = hero.GetComponentsInChildren<SpriteRenderer>(true);
                if (renderers == null || renderers.Length == 0)
                {
                    return;
                }

                Renderer renderer = renderers[0];
                int materialIndex = GetEntryMaterialIndex(new TextureBrowserEntry
                {
                    Renderer = renderer,
                    Material = GetFirstRendererMaterial(renderer)
                });
                RendererIdentity identity = BuildRendererIdentityFromRuntime(renderer, materialIndex);
                if (identity == null)
                {
                    return;
                }

                ResolvedVisualOverride resolved = visualRuleRuntimeManager.Resolve(identity);
                visualOverrideRuntimeProbeLogged = true;
                Log("ShaderStripMod visual-rules runtime probe: identity=" + identity.BuildStableRendererKey() +
                    ", rules=" + visualRuleRuntimeManager.ActiveRuleCount +
                    ", color=" + GetResolvedChannelSummary(resolved.Color) +
                    ", replacement=" + GetResolvedChannelSummary(resolved.Replacement) +
                    ", static=" + GetResolvedChannelSummary(resolved.StaticDisplay) +
                    ". No renderer changes were made.");
            }
            catch (Exception ex)
            {
                visualOverrideRuntimeProbeLogged = true;
                Log("ShaderStripMod visual-rules runtime probe failed: " + ex.Message);
            }
        }

        private static string GetResolvedChannelSummary(ResolvedVisualChannel channel)
        {
            if (channel.BlockingRule != null) return "blocked:" + channel.BlockingRule.Id;
            return channel.Winner != null ? "winner:" + channel.Winner.Id : "none";
        }
    }
}
