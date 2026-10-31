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
        private GUIStyle ruleButtonStyle;
        private Rect RuleButtonRect(Rect area,int count,int index)
        {
            var cell=RuleControlsLayout.Button(area.width,count,index);
            return new Rect(area.x+cell.X,area.y+cell.Y,cell.Width,cell.Height);
        }
        private bool DrawRuleButton(Rect rect,string label,bool active=false,bool available=true)
        {
            if(ruleButtonStyle==null)ruleButtonStyle=new GUIStyle(GUI.skin.button){wordWrap=true};
            bool enabled=GUI.enabled;Color color=GUI.backgroundColor;
            try
            {
                GUI.enabled=enabled&&available;
                if(active)GUI.backgroundColor=new Color(.35f,.85f,1,1);
                return UiButton(rect,active ? "✓ "+UiText(label) : label,ruleButtonStyle);
            }
            finally{GUI.enabled=enabled;GUI.backgroundColor=color;}
        }
        private void DrawRendererPreserveControls(TextureBrowserEntry entry, Rect rect)
        {
            RendererPreserveRule rendererRule = BuildRendererPreserveRule(entry, false);
            string rendererKey = GetRendererPreserveRuleKey(rendererRule);
            bool rendererPreserved = !string.IsNullOrEmpty(rendererKey) && preservedRendererRuleKeys.Contains(rendererKey);
            if (string.IsNullOrEmpty(rendererKey))
            {
                DrawRuleButton(RuleButtonRect(rect,4,0),"No renderer",false,false);
            }
            else if (DrawRuleButton(RuleButtonRect(rect,4,0),"Renderer",rendererPreserved))
            {
                ToggleRendererPreserveRule(rendererRule, !rendererPreserved);
            }

            if (DrawRuleButton(RuleButtonRect(rect,4,1),"Path Renderers",false,!string.IsNullOrEmpty(GetEntryPreservePath(entry))))
            {
                int added = AddRendererRulesUnderEntryPath(entry, false);
                lastTextureExportMessage = "Renderer rules added under path: " + added + ".";
            }

            string projectilePath = GetEntryProjectilePreservePath(entry);
            bool projectilePreserved = !string.IsNullOrEmpty(projectilePath) && preservedProjectileObjectPaths.Contains(projectilePath);
            if (string.IsNullOrEmpty(projectilePath))
            {
                DrawRuleButton(RuleButtonRect(rect,4,2),"No projectile",false,false);
            }
            else if (DrawRuleButton(RuleButtonRect(rect,4,2),"Projectile",projectilePreserved))
            {
                SetPreservePathRule(preservedProjectileObjectPaths, projectilePath, !projectilePreserved);
                lastTextureExportMessage = (projectilePreserved ? "Projectile path removed: " : "Projectile path added: ") + projectilePath + ".";
            }

            if (DrawRuleButton(RuleButtonRect(rect,4,3),"Clear",false,IsTextureBrowserEntryPreserved(entry)))
            {
                ClearPreserveRulesFromBrowser(entry);
            }
        }

        private void DrawAssetPreserveControls(TextureBrowserEntry entry, Rect rect)
        {
            string spriteName = GetEntrySpriteName(entry);
            if (DrawRuleButton(RuleButtonRect(rect,4,0),"Sprite",IsEntrySpritePreserved(entry),!string.IsNullOrEmpty(spriteName)))
            {
                SetPreserveNameRule(preservedSpriteNames, spriteName, !IsEntrySpritePreserved(entry));
            }

            string textureName = GetEntryTextureName(entry);
            if (DrawRuleButton(RuleButtonRect(rect,4,1),"Texture",IsEntryTexturePreserved(entry),!string.IsNullOrEmpty(textureName)))
            {
                SetPreserveNameRule(preservedTextureNames, textureName, !IsEntryTexturePreserved(entry));
            }

            string materialName = GetEntryMaterialName(entry);
            if (DrawRuleButton(RuleButtonRect(rect,4,2),"Material",IsEntryMaterialPreserved(entry),!string.IsNullOrEmpty(materialName)))
            {
                SetPreserveNameRule(preservedMaterialNames, materialName, !IsEntryMaterialPreserved(entry));
            }

            if (DrawRuleButton(RuleButtonRect(rect,4,3),"Clear",false,IsTextureBrowserEntryPreserved(entry)))
            {
                ClearPreserveRulesFromBrowser(entry);
            }
        }

        private void DrawPathPreserveControls(TextureBrowserEntry entry, Rect rect)
        {
            string path = GetEntryPreservePath(entry);
            if (string.IsNullOrEmpty(path))
            {
                UiLabel(new Rect(rect.x, rect.y + 2f, rect.width, rect.height), "No object path.");
                return;
            }

            if (DrawRuleButton(RuleButtonRect(rect,3,0),"Object",IsEntryObjectPreserved(entry)))
            {
                SetPreservePathRule(preservedObjectPaths, path, !IsEntryObjectPreserved(entry));
            }

            if (DrawRuleButton(RuleButtonRect(rect,3,1),"Group",IsEntryGroupPreserved(entry)))
            {
                SetPreservePathRule(preservedGroupObjectPaths, path, !IsEntryGroupPreserved(entry));
            }

            if (DrawRuleButton(RuleButtonRect(rect,3,2),"Projectile",IsEntryProjectilePreserved(entry)))
            {
                SetPreservePathRule(preservedProjectileObjectPaths, path, !IsEntryProjectilePreserved(entry));
            }
        }

        private GUIStyle preserveManagerWrappedStyle;
        private float preserveManagerContentHeight = 600f;
        private void DrawPreserveManagerDetails(Rect detailRect)
        {
            UiBox(detailRect, "");
            Rect local = new Rect(detailRect.x + 12f, detailRect.y + 10f, detailRect.width - 24f, detailRect.height - 20f);
            if (preserveManagerWrappedStyle == null) preserveManagerWrappedStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            float buttonWidth = Mathf.Max(1f, (local.width - 12f) / 3f);
            bool reload = UiButton(new Rect(local.x, local.y, buttonWidth, 26f), "Reload");
            bool compact = UiButton(new Rect(local.x + buttonWidth + 6f, local.y, buttonWidth, 26f), "Compact");
            bool clean = UiButton(new Rect(local.x + 2f * (buttonWidth + 6f), local.y, buttonWidth, 26f), "Clean Ignored");
            Rect viewport = new Rect(local.x, local.y + 34f, local.width, Mathf.Max(1f, local.height - 34f));
            float width = Mathf.Max(1f, viewport.width - 20f);
            textureBrowserDetailScroll = GUI.BeginScrollView(viewport, textureBrowserDetailScroll,
                new Rect(0, 0, width, Mathf.Max(viewport.height, preserveManagerContentHeight)));
            float y = 0;
            DrawPreserveManagerWrappedLabel(ref y, width, "Rules: renderer=" + preservedRendererRules.Count + ", path=" + preservedObjectPaths.Count + ", group=" + preservedGroupObjectPaths.Count + ", projectile=" + preservedProjectileObjectPaths.Count + ", sprite=" + preservedSpriteNames.Count + ", texture=" + preservedTextureNames.Count + ", material=" + preservedMaterialNames.Count + ", legacy=" + preservedAssetNames.Count + ", ignored=" + CountIgnoredPreservePathRules());
            DrawPreserveManagerWrappedLabel(ref y, width, lastPreserveManagerMessage);
            TextureBrowserEntry entry = selectedTextureBrowserIndex >= 0 && selectedTextureBrowserIndex < textureBrowserEntries.Count ? textureBrowserEntries[selectedTextureBrowserIndex] : null;
            bool remove = false;
            if (entry == null) DrawPreserveManagerWrappedLabel(ref y, width, "No preserve rule selected.");
            else
            {
                DrawPreserveManagerWrappedLabel(ref y, width, entry.Kind + ": " + entry.Name);
                remove = UiButton(new Rect(0, y, Mathf.Min(140f, width), 26f), "Remove Rule");
                y += 34f;
                DrawPreserveManagerWrappedLabel(ref y, width, BuildPreserveManagerInfo(entry));
                DrawPreserveManagerWrappedLabel(ref y, width, BuildPreserveRuntimeSummary());
                DrawPreserveManagerWrappedLabel(ref y, width, GetPreserveManagerRuntimeReport(entry));
            }
            preserveManagerContentHeight = y + 8f;
            GUI.EndScrollView();
            if (reload) { ReloadPreserveList(); lastPreserveManagerMessage = "Preserve file reloaded."; RefreshPreserveManagerEntries(); }
            if (compact) CompactPreserveListFromManager();
            if (clean) CleanIgnoredPreserveRulesFromManager();
            if (remove && entry != null) RemovePreserveRuleFromManager(entry);
        }
        private void DrawPreserveManagerWrappedLabel(ref float y, float width, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            // Allow wrapping inside long filesystem/object paths without changing the saved rules.
            string display = UiText(text).Replace("\\", "\\\u200B").Replace("/", "/\u200B");
            float height = preserveManagerWrappedStyle.CalcHeight(new GUIContent(display), width);
            UiLabel(new Rect(0, y, width, height), display, preserveManagerWrappedStyle);
            y += height + 10f;
        }
        private int CountIgnoredPreservePathRules()
        {
            int count = 0;
            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                if (IsIgnoredRendererPreserveRule(preservedRendererRules[i]))
                {
                    count++;
                }
            }

            foreach (string path in preservedObjectPaths)
            {
                if (IsIgnoredPreservedObjectPathRule(path))
                {
                    count++;
                }
            }

            foreach (string path in preservedGroupObjectPaths)
            {
                if (IsIgnoredPreservedObjectPathRule(path))
                {
                    count++;
                }
            }

            foreach (string path in preservedProjectileObjectPaths)
            {
                if (IsIgnoredProjectilePreservePath(path))
                {
                    count++;
                }
            }

            return count;
        }

        private bool RemovePreserveRuleValue(string ruleKind, string value)
        {
            if (string.IsNullOrEmpty(ruleKind))
            {
                return preservedAssetNames.Remove(value) || preservedObjectPaths.Remove(value);
            }

            if (string.Equals(ruleKind, "path", System.StringComparison.OrdinalIgnoreCase))
            {
                return preservedObjectPaths.Remove(value);
            }

            if (string.Equals(ruleKind, "renderer", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ruleKind, "projectileRenderer", System.StringComparison.OrdinalIgnoreCase))
            {
                return RemoveRendererPreserveRule(value);
            }

            if (string.Equals(ruleKind, "groupPath", System.StringComparison.OrdinalIgnoreCase))
            {
                return preservedGroupObjectPaths.Remove(value);
            }

            if (string.Equals(ruleKind, "projectilePath", System.StringComparison.OrdinalIgnoreCase))
            {
                return preservedProjectileObjectPaths.Remove(value);
            }

            if (string.Equals(ruleKind, "sprite", System.StringComparison.OrdinalIgnoreCase))
            {
                return preservedSpriteNames.Remove(value);
            }

            if (string.Equals(ruleKind, "texture", System.StringComparison.OrdinalIgnoreCase))
            {
                return preservedTextureNames.Remove(value);
            }

            if (string.Equals(ruleKind, "material", System.StringComparison.OrdinalIgnoreCase))
            {
                return preservedMaterialNames.Remove(value);
            }

            return preservedAssetNames.Remove(value);
        }

        private bool RemoveRendererPreserveRule(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                RendererPreserveRule rule = preservedRendererRules[i];
                if (rule == null)
                {
                    continue;
                }

                string ruleKey = GetRendererPreserveRuleKey(rule);
                if (!string.Equals(ruleKey, key, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                preservedRendererRules.RemoveAt(i);
                preservedRendererRuleKeys.Remove(ruleKey);
                preservedRendererRuleIndexDirty = true;
                return true;
            }

            return false;
        }

        private string BuildPreserveManagerInfo(TextureBrowserEntry entry)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Preserve file:");
            builder.AppendLine(preserveFilePath);
            builder.AppendLine();
            string ruleKind = entry != null && !string.IsNullOrEmpty(entry.PreserveManagerRuleKind) ? entry.PreserveManagerRuleKind : (entry != null && entry.PreserveManagerPathRule ? "path" : "asset");
            builder.AppendLine("Rule type: " + ruleKind);
            builder.AppendLine("Rule value: " + (entry != null ? entry.PreserveRuleValue : ""));
            if (entry != null && entry.PreserveManagerPathRule)
            {
                bool ignored = string.Equals(ruleKind, "projectilePath", System.StringComparison.OrdinalIgnoreCase) ? IsIgnoredProjectilePreservePath(entry.PreserveRuleValue) : IsIgnoredPreservedObjectPathRule(entry.PreserveRuleValue);
                builder.AppendLine("Status: " + (ignored ? "ignored by runtime" : "effective"));
                if (string.Equals(ruleKind, "groupPath", System.StringComparison.OrdinalIgnoreCase))
                {
                    builder.AppendLine("Effect: preserves renderers on this object path and its children.");
                }
                else if (string.Equals(ruleKind, "projectilePath", System.StringComparison.OrdinalIgnoreCase))
                {
                    builder.AppendLine("Effect: preserves this projectile path and children, including limited particle renderers.");
                }
                else
                {
                    builder.AppendLine("Effect: preserves renderers on this exact object path only.");
                }

                if (ignored)
                {
                    builder.AppendLine("Clean Ignored can remove this rule. The rule is already skipped while rendering.");
                }
            }
            else if (string.Equals(ruleKind, "renderer", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ruleKind, "projectileRenderer", System.StringComparison.OrdinalIgnoreCase))
            {
                bool ignored = entry != null && IsIgnoredRendererPreserveRule(entry.PreserveManagerRuleKind, entry.PreservePath);
                builder.AppendLine("Status: " + (ignored ? "ignored by runtime" : "effective"));
                builder.AppendLine(string.Equals(ruleKind, "projectileRenderer", System.StringComparison.OrdinalIgnoreCase) ? "Effect: preserves one projectile renderer path/type and allows its particle renderer when matched." : "Effect: preserves one exact renderer path/type without preserving the whole texture atlas or object tree.");
                if (ignored)
                {
                    builder.AppendLine("Clean Ignored can remove this rule. The rule is already skipped while rendering.");
                }
            }
            else
            {
                builder.AppendLine("Status: effective");
                builder.AppendLine("Effect: preserves matching " + ruleKind + " names.");
            }

            if (entry != null && !string.IsNullOrEmpty(entry.Details))
            {
                builder.AppendLine();
                builder.AppendLine(entry.Details);
            }

            return builder.ToString();
        }

        private void RemovePreserveRuleFromManager(TextureBrowserEntry entry)
        {
            if (entry == null || !entry.PreserveManagerRule)
            {
                return;
            }

            string value = entry.PreserveRuleValue;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            bool changed = RemovePreserveRuleValue(entry.PreserveManagerRuleKind, value);
            if (!changed)
            {
                lastPreserveManagerMessage = "Rule was already absent.";
                RefreshPreserveManagerEntries();
                return;
            }

            string backup = BackupPreserveFile("before_remove_rule");
            SavePreserveList();
            RefreshAfterPreserveManagerChange();
            lastPreserveManagerMessage = "Removed preserve rule. Backup: " + backup;
        }

        private void CleanIgnoredPreserveRulesFromManager()
        {
            List<string> removeRenderers = new List<string>();
            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                RendererPreserveRule rule = preservedRendererRules[i];
                if (IsIgnoredRendererPreserveRule(rule))
                {
                    string key = GetRendererPreserveRuleKey(rule);
                    if (!string.IsNullOrEmpty(key))
                    {
                        removeRenderers.Add(key);
                    }
                }
            }

            List<string> remove = new List<string>();
            foreach (string path in preservedObjectPaths)
            {
                if (IsIgnoredPreservedObjectPathRule(path))
                {
                    remove.Add(path);
                }
            }

            List<string> removeGroups = new List<string>();
            foreach (string path in preservedGroupObjectPaths)
            {
                if (IsIgnoredPreservedObjectPathRule(path))
                {
                    removeGroups.Add(path);
                }
            }

            List<string> removeProjectiles = new List<string>();
            foreach (string path in preservedProjectileObjectPaths)
            {
                if (IsIgnoredProjectilePreservePath(path))
                {
                    removeProjectiles.Add(path);
                }
            }

            if (removeRenderers.Count == 0 && remove.Count == 0 && removeGroups.Count == 0 && removeProjectiles.Count == 0)
            {
                lastPreserveManagerMessage = "No ignored rules found.";
                RefreshPreserveManagerEntries();
                return;
            }

            string backup = BackupPreserveFile("before_clean_ignored");
            for (int i = 0; i < removeRenderers.Count; i++)
            {
                RemoveRendererPreserveRule(removeRenderers[i]);
            }

            for (int i = 0; i < remove.Count; i++)
            {
                preservedObjectPaths.Remove(remove[i]);
            }

            for (int i = 0; i < removeGroups.Count; i++)
            {
                preservedGroupObjectPaths.Remove(removeGroups[i]);
            }

            for (int i = 0; i < removeProjectiles.Count; i++)
            {
                preservedProjectileObjectPaths.Remove(removeProjectiles[i]);
            }

            SavePreserveList();
            RefreshAfterPreserveManagerChange();
            lastPreserveManagerMessage = "Removed ignored rules: " + (removeRenderers.Count + remove.Count + removeGroups.Count + removeProjectiles.Count) + ". Backup: " + backup;
        }

        private void CompactPreserveListFromManager()
        {
            string backup = BackupPreserveFile("before_compact");
            SavePreserveList();
            RefreshAfterPreserveManagerChange();
            lastPreserveManagerMessage = "Preserve file compacted. Backup: " + backup;
        }

        private void RefreshAfterPreserveManagerChange()
        {
            RestoreShaders();
            RestorePromotedEffects();
            RestorePreservedRendererDraws();
            RestorePreservedRendererProxies();
            MarkPreservedRendererCacheDirty();
            if (visualsActive)
            {
                ApplyStrip();
            }

            RefreshPreserveManagerEntries();
            preserveManagerRuntimeReportDirty = true;
        }

        private string BackupPreserveFile(string reason)
        {
            if (string.IsNullOrEmpty(preserveFilePath) || !File.Exists(preserveFilePath))
            {
                return "";
            }

            try
            {
                string directory = Path.GetDirectoryName(preserveFilePath);
                if (string.IsNullOrEmpty(directory))
                {
                    directory = ".";
                }

                string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(directory, PreserveFileName + ".bak_" + reason + "_" + stamp);
                backupPath = GetUniqueFilePath(backupPath);
                File.Copy(preserveFilePath, backupPath);
                return backupPath;
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod preserve backup failed: " + ex.Message);
                return "";
            }
        }

    }
}

