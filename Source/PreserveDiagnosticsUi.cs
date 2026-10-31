using System.Text;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private string preserveManagerRuntimeReport = "";
        private string preserveManagerRuntimeReportKey = "";
        private bool preserveManagerRuntimeReportDirty = true;

        private string BuildPreserveRuntimeSummary()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Runtime draw state (last sync):");
            builder.AppendLine("Scene: " + GetActiveSceneName());
            if (!visualsActive)
            {
                builder.AppendLine("Visuals: disabled (F12). Preserve counters are cleared until visuals are enabled again.");
            }
            builder.AppendLine("Draw sources: " + preserveRunStats.Sources + " / " + maxPreservedRenderers +
                (preserveRunStats.Sources >= maxPreservedRenderers ? " (draw limit reached)" : ""));
            if (preserveRunStats.CandidateMatched > 0 || preserveRunStats.SkippedByDrawLimit > 0 || preserveRunStats.SkippedByParticleLimit > 0)
            {
                builder.AppendLine("Last full scan: matched " + preserveRunStats.CandidateMatched +
                    ", skipped by draw limit " + preserveRunStats.SkippedByDrawLimit +
                    ", by particle limit " + preserveRunStats.SkippedByParticleLimit + ".");
            }
            else
            {
                builder.AppendLine("Last full scan: pending. Close the browser and let gameplay draw for a moment.");
            }

            builder.AppendLine("Last sync: drawn " + preserveRunStats.Drawn + ", blocked " + preserveRunStats.Stale + ".");
            if (preserveRunStats.Stale > 0)
            {
                builder.AppendLine("Blocked reasons: disabled " + preserveRunStats.SkippedDisabled +
                    ", inactive " + preserveRunStats.SkippedInactive +
                    ", already visible " + preserveRunStats.SkippedAlreadyVisible +
                    ", particle " + preserveRunStats.SkippedExpensiveParticle +
                    ", other scene " + preserveRunStats.SkippedOtherScene +
                    ", no rule " + preserveRunStats.SkippedNoRule +
                    ", no materials " + preserveRunStats.SkippedNoMaterials + ".");
            }

            builder.AppendLine("Last drawn: " + DescribeRendererForDiagnostics(preserveLastDrawnRenderer, false) + ".");
            if (preserveRunStats.Stale > 0)
            {
                builder.AppendLine("Last blocked: " + DescribeRendererForDiagnostics(preserveLastSkippedRenderer, true) + ".");
            }

            return builder.ToString();
        }

        private string DescribeRendererForDiagnostics(Renderer renderer, bool includeSkipReason)
        {
            if (renderer == null || renderer.gameObject == null)
            {
                return "none";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(renderer.GetType().Name);
            builder.Append(" at ");
            builder.Append(CleanUnityPath(GetObjectPath(renderer.gameObject)));
            builder.Append(" [layer ");
            builder.Append(GetLayerLabel(renderer.gameObject));
            builder.Append("]");
            if (includeSkipReason)
            {
                builder.Append("; reason: ");
                builder.Append(GetPreservedRendererSkipReason(renderer, GetActiveSceneName()));
            }

            builder.Append("; rules: ");
            builder.Append(BuildRendererPreserveReasonList(renderer));
            builder.Append("; in draw cache: ");
            builder.Append(FormatBool(preservedRendererDrawSources.Contains(renderer)));
            return builder.ToString();
        }

        private bool IsPreserveManagerEntryIgnored(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            string ruleKind = !string.IsNullOrEmpty(entry.PreserveManagerRuleKind) ? entry.PreserveManagerRuleKind : (entry.PreserveManagerPathRule ? "path" : "asset");
            if (entry.PreserveManagerPathRule)
            {
                return string.Equals(ruleKind, "projectilePath", System.StringComparison.OrdinalIgnoreCase)
                    ? IsIgnoredProjectilePreservePath(entry.PreserveRuleValue)
                    : IsIgnoredPreservedObjectPathRule(entry.PreserveRuleValue);
            }

            if (string.Equals(ruleKind, "renderer", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ruleKind, "projectileRenderer", System.StringComparison.OrdinalIgnoreCase))
            {
                return IsIgnoredRendererPreserveRule(ruleKind, entry.PreservePath);
            }

            return false;
        }

        private string GetPreserveManagerRuntimeReport(TextureBrowserEntry entry)
        {
            string key = BuildPreserveRuntimeReportKey(entry);
            if (preserveManagerRuntimeReportDirty || !string.Equals(key, preserveManagerRuntimeReportKey, System.StringComparison.Ordinal))
            {
                preserveManagerRuntimeReport = BuildPreserveRuleLiveReport(entry);
                preserveManagerRuntimeReportKey = key;
                preserveManagerRuntimeReportDirty = false;
            }

            return preserveManagerRuntimeReport;
        }

        private string BuildPreserveRuntimeReportKey(TextureBrowserEntry entry)
        {
            if (entry == null)
            {
                return "none|" + GetActiveSceneName();
            }

            return selectedTextureBrowserIndex + "|" + entry.PreserveManagerRuleKind + "|" + entry.PreserveRuleValue +
                "|" + entry.PreservePath + "|" + (entry.Renderer != null ? entry.Renderer.GetInstanceID() : 0) +
                "|" + GetActiveSceneName() + "|" + GetTotalPreserveRuleCount() + "|" + (visualsActive ? "on" : "off");
        }

        private string BuildPreserveRuleLiveReport(TextureBrowserEntry entry)
        {
            if (entry == null || !entry.PreserveManagerRule)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();
            string sceneName = GetActiveSceneName();
            builder.AppendLine("Selected rule live check (scene: " + sceneName + "):");
            builder.AppendLine("Visuals: " + (visualsActive
                ? "enabled (F12); counts scan loaded renderers for this rule."
                : "disabled (F12); runtime draw state is cleared, counts below are a dry scan of loaded renderers."));
            builder.AppendLine("Rule: " + entry.PreserveManagerRuleKind + " = " + entry.PreserveRuleValue);
            builder.AppendLine("Matching model: renderer rules use scene + object path + renderer type; path/group rules use object hierarchy; sprite/texture/material rules use asset names. Atlas regions are not detected by meaning, so a sprite rule keeps the region used by that renderer, not an automatically recognized bench or enemy.");

            if (IsPreserveManagerEntryIgnored(entry))
            {
                builder.AppendLine("Status: ignored by runtime, so this rule promotes no renderers.");
                return builder.ToString();
            }

            int matched = 0;
            int inScene = 0;
            int drawable = 0;
            int cached = 0;
            int drawLimit = 0;
            int particleLimit = 0;
            int disabled = 0;
            int inactive = 0;
            int alreadyVisible = 0;
            int expensive = 0;
            int otherScene = 0;
            int noRule = 0;
            int noMaterials = 0;
            int proxy = 0;
            Renderer chosen = null;

            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!RendererMatchesPreserveManagerRule(entry, renderer))
                {
                    continue;
                }

                matched++;
                bool inDrawCache = preservedRendererDrawSources.Contains(renderer);
                if (inDrawCache)
                {
                    cached++;
                }

                if (IsRendererInCurrentScene(renderer, sceneName))
                {
                    inScene++;
                }

                PreserveSkipKind kind = ClassifyPreservedRenderer(renderer, sceneName);
                if (kind == PreserveSkipKind.Drawable)
                {
                    drawable++;
                    if (chosen == null || (inDrawCache && !preservedRendererDrawSources.Contains(chosen)))
                    {
                        chosen = renderer;
                    }

                    if (!inDrawCache)
                    {
                        if (preservedRendererDrawSources.Count >= maxPreservedRenderers)
                        {
                            drawLimit++;
                        }
                        else if (IsParticleSystemRenderer(renderer) &&
                            !RendererHasProjectileRendererRule(renderer) &&
                            CountProjectileParticleDrawSources() >= maxProjectileParticleRenderers)
                        {
                            particleLimit++;
                        }
                    }

                    continue;
                }

                switch (kind)
                {
                    case PreserveSkipKind.Disabled:
                        disabled++;
                        break;
                    case PreserveSkipKind.Inactive:
                        inactive++;
                        break;
                    case PreserveSkipKind.AlreadyVisible:
                        alreadyVisible++;
                        break;
                    case PreserveSkipKind.ExpensiveParticle:
                        expensive++;
                        break;
                    case PreserveSkipKind.OtherScene:
                        otherScene++;
                        break;
                    case PreserveSkipKind.NoRule:
                        noRule++;
                        break;
                    case PreserveSkipKind.NoMaterials:
                        noMaterials++;
                        break;
                    case PreserveSkipKind.ProxyRenderer:
                        proxy++;
                        break;
                }
            }

            builder.AppendLine("Matched loaded renderers: " + matched + " (in current scene: " + inScene + ").");
            builder.AppendLine("Drawable now: " + drawable + "; in draw source cache: " + cached + ".");
            if (drawLimit > 0 || particleLimit > 0)
            {
                builder.AppendLine("Skipped by limits: draw " + drawLimit + " / " + maxPreservedRenderers +
                    ", particle " + particleLimit + " / " + maxProjectileParticleRenderers + ".");
            }

            if (disabled + inactive + alreadyVisible + expensive + otherScene + noRule + noMaterials + proxy > 0)
            {
                builder.AppendLine("Blocked: disabled " + disabled +
                    ", inactive " + inactive +
                    ", already visible " + alreadyVisible +
                    ", particle " + expensive +
                    ", other scene " + otherScene +
                    ", no rule " + noRule +
                    ", no materials " + noMaterials +
                    ", proxy " + proxy + ".");
            }

            if (chosen == null)
            {
                builder.AppendLine(matched == 0
                    ? "Chosen renderer: none. No loaded renderer matches this rule in the current scene."
                    : "Chosen renderer: none. Renderers match the rule, but none can be drawn now. Check the block reasons above.");
                return builder.ToString();
            }

            builder.AppendLine("Chosen renderer: " + chosen.GetType().Name);
            builder.AppendLine("Object path: " + CleanUnityPath(GetObjectPath(chosen.gameObject)));
            builder.AppendLine("Layer: " + GetLayerLabel(chosen.gameObject));
            builder.AppendLine("Rule(s): " + BuildRendererPreserveReasonList(chosen));
            builder.AppendLine("Status: " + GetPreservedRendererSkipReason(chosen, sceneName) + ".");
            builder.AppendLine("In draw cache: " + FormatBool(preservedRendererDrawSources.Contains(chosen)) +
                "; proxy: " + FormatBool(preservedRendererProxies.ContainsKey(chosen)) + ".");
            return builder.ToString();
        }

        private RendererPreserveRule FindPreservedRendererRuleByEntry(TextureBrowserEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.PreserveRuleValue))
            {
                return null;
            }

            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                RendererPreserveRule rule = preservedRendererRules[i];
                if (rule == null)
                {
                    continue;
                }

                if (string.Equals(GetRendererPreserveRuleKey(rule), entry.PreserveRuleValue, System.StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }

            return null;
        }

        private bool RendererMatchesPreserveManagerRule(TextureBrowserEntry entry, Renderer renderer)
        {
            if (entry == null || renderer == null || renderer.gameObject == null || IsProxyRenderer(renderer))
            {
                return false;
            }

            string ruleKind = !string.IsNullOrEmpty(entry.PreserveManagerRuleKind) ? entry.PreserveManagerRuleKind : "";
            string ruleValue = entry.PreserveRuleValue;
            if (string.IsNullOrEmpty(ruleValue))
            {
                return false;
            }

            string sceneName = GetRendererSceneName(renderer);
            string objectPath = CleanUnityPath(GetObjectPath(renderer.gameObject));
            string rendererType = CleanUnityName(renderer.GetType().Name);

            if (string.Equals(ruleKind, "renderer", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ruleKind, "projectileRenderer", System.StringComparison.OrdinalIgnoreCase))
            {
                RendererPreserveRule rule = FindPreservedRendererRuleByEntry(entry);
                return rule != null && RendererMatchesPreserveRule(rule, renderer, sceneName, objectPath, rendererType);
            }

            string cleanedRule = CleanUnityPath(ruleValue);
            if (string.Equals(ruleKind, "path", System.StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(objectPath, cleanedRule, System.StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(ruleKind, "groupPath", System.StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(objectPath, cleanedRule, System.StringComparison.OrdinalIgnoreCase) ||
                    objectPath.StartsWith(cleanedRule + "/", System.StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(ruleKind, "projectilePath", System.StringComparison.OrdinalIgnoreCase))
            {
                return ProjectilePathMatchesRule(objectPath, cleanedRule, true);
            }

            string cleanedValue = CleanUnityName(ruleValue);
            if (string.Equals(ruleKind, "sprite", System.StringComparison.OrdinalIgnoreCase))
            {
                SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
                return spriteRenderer != null && spriteRenderer.sprite != null &&
                    string.Equals(GetObjectName(spriteRenderer.sprite), cleanedValue, System.StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(ruleKind, "texture", System.StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(GetRendererTextureName(renderer), cleanedValue, System.StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(ruleKind, "material", System.StringComparison.OrdinalIgnoreCase))
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null)
                {
                    return false;
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && string.Equals(GetObjectName(materials[i]), cleanedValue, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (string.Equals(ruleKind, "asset", System.StringComparison.OrdinalIgnoreCase))
            {
                SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    if (string.Equals(GetObjectName(spriteRenderer.sprite), cleanedValue, System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(GetObjectName(spriteRenderer.sprite.texture), cleanedValue, System.StringComparison.OrdinalIgnoreCase))
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

                    if (string.Equals(GetObjectName(material), cleanedValue, System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(GetObjectName(GetMaterialMainTexture(material)), cleanedValue, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private string GetRendererTextureName(Renderer renderer)
        {
            if (renderer == null)
            {
                return "";
            }

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                return GetObjectName(spriteRenderer.sprite.texture);
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        continue;
                    }

                    string name = GetObjectName(GetMaterialMainTexture(materials[i]));
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }

            return "";
        }
    }
}
