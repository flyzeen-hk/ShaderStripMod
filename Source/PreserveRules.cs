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
        private void LoadPreserveList()
        {
            if(string.IsNullOrEmpty(preserveFilePath)) InitializeListFiles();
            var sets=new[]{preservedAssetNames,preservedSpriteNames,preservedTextureNames,preservedMaterialNames,
                preservedObjectPaths,preservedGroupObjectPaths,preservedProjectileObjectPaths,preservedRendererRuleKeys,
                preservedRendererExactMatchKeys,preservedProjectileRendererExactMatchKeys};
            List<string>[] previousSets=null;
            List<RendererPreserveRule> previousRules=null;
            bool previousIndexDirty=preservedRendererRuleIndexDirty,previousFallback=preservedRendererRuleIndexHasFallbackRules;
            try
            {
                if(!File.Exists(preserveFilePath)) AtomicTextFile.Write(preserveFilePath,BuildDefaultPreserveFile());
                // Complete the read before changing live visibility; a locked/unreadable file leaves it intact.
                string[] lines=File.ReadAllLines(preserveFilePath,Encoding.UTF8);
                previousSets=new List<string>[sets.Length];
                for(int i=0;i<sets.Length;i++) previousSets[i]=new List<string>(sets[i]);
                previousRules=new List<RendererPreserveRule>(preservedRendererRules);
                foreach(var set in sets) set.Clear();
                preservedRendererRules.Clear();
                preservedRendererRuleIndexDirty=true;
                preservedRendererRuleIndexHasFallbackRules=false;
                foreach(string line in lines) AddPreserveEntriesFromLine(line);
                Log("ShaderStripMod loaded preserve entries from " + preserveFilePath + ": renderers=" + preservedRendererRules.Count + ", paths=" + preservedObjectPaths.Count + ", groups=" + preservedGroupObjectPaths.Count + ", projectiles=" + preservedProjectileObjectPaths.Count + ", sprites=" + preservedSpriteNames.Count + ", textures=" + preservedTextureNames.Count + ", materials=" + preservedMaterialNames.Count + ", legacyAssets=" + preservedAssetNames.Count + ".");
            }
            catch(System.Exception ex)
            {
                if(previousRules!=null)
                {
                    for(int i=0;i<sets.Length;i++) { sets[i].Clear();sets[i].UnionWith(previousSets[i]); }
                    preservedRendererRules.Clear();preservedRendererRules.AddRange(previousRules);
                    preservedRendererRuleIndexDirty=previousIndexDirty;
                    preservedRendererRuleIndexHasFallbackRules=previousFallback;
                }
                Log("ShaderStripMod preserve list load failed; previous entries kept: " + ex.Message);
            }
        }

        private void AddPreserveEntriesFromLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            string entry = line.Trim();
            if (entry.Length == 0 || entry.StartsWith("#") || entry.StartsWith("//"))
            {
                return;
            }

            bool addedKnownKey = false;
            RendererPreserveRule rendererRule = new RendererPreserveRule();
            bool hasRendererRule = false;
            string[] parts = entry.Split('\t');
            bool parseRendererRule = EntryContainsRendererRule(parts);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                int separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string key = part.Substring(0, separator).Trim();
                string value = part.Substring(separator + 1);
                if (parseRendererRule && ApplyRendererRulePart(rendererRule, key, value))
                {
                    hasRendererRule = true;
                    addedKnownKey = true;
                    continue;
                }

                if (IsPreserveGroupPathKey(key))
                {
                    AddPreserveGroupPath(value);
                    addedKnownKey = true;
                    continue;
                }

                if (IsPreserveProjectilePathKey(key))
                {
                    AddPreserveProjectilePath(value);
                    addedKnownKey = true;
                    continue;
                }

                if (IsPreservePathKey(key))
                {
                    AddPreservePath(value);
                    addedKnownKey = true;
                    continue;
                }

                if (IsPreserveSpriteKey(key))
                {
                    AddPreserveSpriteName(value);
                    addedKnownKey = true;
                    continue;
                }

                if (IsPreserveTextureKey(key))
                {
                    AddPreserveTextureName(value);
                    addedKnownKey = true;
                    continue;
                }

                if (IsPreserveMaterialKey(key))
                {
                    AddPreserveMaterialName(value);
                    addedKnownKey = true;
                    continue;
                }

                if (!IsPreserveEntryKey(key))
                {
                    continue;
                }

                AddPreserveName(value);
                addedKnownKey = true;
            }

            if (hasRendererRule)
            {
                AddRendererPreserveRule(rendererRule);
            }

            if (!addedKnownKey)
            {
                AddPreserveName(entry);
            }
        }

        private bool EntryContainsRendererRule(string[] parts)
        {
            if (parts == null)
            {
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i] != null ? parts[i].Trim() : "";
                int separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string key = part.Substring(0, separator).Trim();
                if (string.Equals(key, "rendererPath", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "rendererObjectPath", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "projectileRendererPath", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ApplyRendererRulePart(RendererPreserveRule rule, string key, string value)
        {
            if (rule == null)
            {
                return false;
            }

            if (string.Equals(key, "rendererPath", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "rendererObjectPath", System.StringComparison.OrdinalIgnoreCase))
            {
                rule.ObjectPath = CleanUnityPath(value);
                return true;
            }

            if (string.Equals(key, "projectileRendererPath", System.StringComparison.OrdinalIgnoreCase))
            {
                rule.ObjectPath = CleanUnityPath(value);
                rule.Projectile = true;
                return true;
            }

            if (string.Equals(key, "rendererType", System.StringComparison.OrdinalIgnoreCase))
            {
                rule.RendererType = CleanUnityName(value);
                return true;
            }

            if (string.Equals(key, "scene", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "rendererScene", System.StringComparison.OrdinalIgnoreCase))
            {
                rule.SceneName = CleanUnityName(value);
                return true;
            }

            if (string.Equals(key, "sprite", System.StringComparison.OrdinalIgnoreCase))
            {
                rule.SpriteName = CleanUnityName(value);
                return true;
            }

            if (string.Equals(key, "material", System.StringComparison.OrdinalIgnoreCase))
            {
                rule.MaterialName = CleanUnityName(value);
                return true;
            }

            if (string.Equals(key, "texture", System.StringComparison.OrdinalIgnoreCase))
            {
                rule.TextureName = CleanUnityName(value);
                return true;
            }

            return false;
        }

        private bool IsPreserveEntryKey(string key)
        {
            return string.Equals(key, "asset", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "legacy", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "name", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPreserveSpriteKey(string key)
        {
            return string.Equals(key, "sprite", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPreserveTextureKey(string key)
        {
            return string.Equals(key, "texture", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "spriteTexture", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "materialTexture", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "mainTexture", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPreserveMaterialKey(string key)
        {
            return string.Equals(key, "material", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPreservePathKey(string key)
        {
            return string.Equals(key, "path", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "objectPath", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "entity", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "root", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPreserveGroupPathKey(string key)
        {
            return string.Equals(key, "groupPath", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "group", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "childrenPath", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPreserveProjectilePathKey(string key)
        {
            return string.Equals(key, "projectilePath", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "projectile", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "attackPath", System.StringComparison.OrdinalIgnoreCase);
        }

        private void AddPreserveName(string name)
        {
            string cleaned = CleanUnityName(name);
            if (!string.IsNullOrEmpty(cleaned))
            {
                preservedAssetNames.Add(cleaned);
            }
        }

        private void AddPreserveSpriteName(string name)
        {
            string cleaned = CleanUnityName(name);
            if (!string.IsNullOrEmpty(cleaned))
            {
                preservedSpriteNames.Add(cleaned);
            }
        }

        private void AddPreserveTextureName(string name)
        {
            string cleaned = CleanUnityName(name);
            if (!string.IsNullOrEmpty(cleaned))
            {
                preservedTextureNames.Add(cleaned);
            }
        }

        private void AddPreserveMaterialName(string name)
        {
            string cleaned = CleanUnityName(name);
            if (!string.IsNullOrEmpty(cleaned))
            {
                preservedMaterialNames.Add(cleaned);
            }
        }

        private void AddPreservePath(string path)
        {
            string cleaned = CleanUnityPath(path);
            if (!string.IsNullOrEmpty(cleaned))
            {
                preservedObjectPaths.Add(cleaned);
            }
        }

        private void AddPreserveGroupPath(string path)
        {
            string cleaned = CleanUnityPath(path);
            if (!string.IsNullOrEmpty(cleaned))
            {
                preservedGroupObjectPaths.Add(cleaned);
            }
        }

        private void AddPreserveProjectilePath(string path)
        {
            string cleaned = CleanUnityPath(path);
            if (!string.IsNullOrEmpty(cleaned))
            {
                preservedProjectileObjectPaths.Add(cleaned);
            }
        }

        private bool AddRendererPreserveRule(RendererPreserveRule rule)
        {
            NormalizeRendererPreserveRule(rule);
            string key = GetRendererPreserveRuleKey(rule);
            if (string.IsNullOrEmpty(key) || preservedRendererRuleKeys.Contains(key))
            {
                return false;
            }

            rule.RuleKey = key;
            preservedRendererRuleKeys.Add(key);
            preservedRendererRules.Add(rule);
            preservedRendererRuleIndexDirty = true;
            return true;
        }

        private void NormalizeRendererPreserveRule(RendererPreserveRule rule)
        {
            if (rule == null)
            {
                return;
            }

            rule.SceneName = CleanUnityName(rule.SceneName);
            rule.ObjectPath = CleanUnityPath(rule.ObjectPath);
            rule.RendererType = CleanUnityName(rule.RendererType);
            rule.SpriteName = CleanUnityName(rule.SpriteName);
            rule.MaterialName = CleanUnityName(rule.MaterialName);
            rule.TextureName = CleanUnityName(rule.TextureName);
            if (rule.Projectile)
            {
                rule.SpriteName = "";
                rule.MaterialName = "";
                rule.TextureName = "";
            }
        }

        private string GetRendererPreserveRuleKey(RendererPreserveRule rule)
        {
            if (rule == null)
            {
                return "";
            }

            string path = CleanUnityPath(rule.ObjectPath);
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }

            string rendererType = CleanUnityName(rule.RendererType);
            string sceneName = CleanUnityName(rule.SceneName);
            string spriteName = CleanUnityName(rule.SpriteName);
            string materialName = CleanUnityName(rule.MaterialName);
            string textureName = CleanUnityName(rule.TextureName);
            return (rule.Projectile ? "projectileRenderer" : "renderer") + "|" + sceneName + "|" + path + "|" + rendererType + "|" + spriteName + "|" + materialName + "|" + textureName;
        }

        private void SavePreserveList()
        {
            if (string.IsNullOrEmpty(preserveFilePath))
            {
                InitializeListFiles();
            }

            try
            {
                List<string> entries = new List<string>(preservedAssetNames);
                entries.Sort(System.StringComparer.OrdinalIgnoreCase);
                List<string> sprites = new List<string>(preservedSpriteNames);
                sprites.Sort(System.StringComparer.OrdinalIgnoreCase);
                List<string> textures = new List<string>(preservedTextureNames);
                textures.Sort(System.StringComparer.OrdinalIgnoreCase);
                List<string> materials = new List<string>(preservedMaterialNames);
                materials.Sort(System.StringComparer.OrdinalIgnoreCase);
                List<string> paths = new List<string>(preservedObjectPaths);
                paths.Sort(System.StringComparer.OrdinalIgnoreCase);
                List<string> groups = new List<string>(preservedGroupObjectPaths);
                groups.Sort(System.StringComparer.OrdinalIgnoreCase);
                List<string> projectiles = new List<string>(preservedProjectileObjectPaths);
                projectiles.Sort(System.StringComparer.OrdinalIgnoreCase);
                List<RendererPreserveRule> rendererRules = new List<RendererPreserveRule>(preservedRendererRules);
                rendererRules.Sort(CompareRendererPreserveRules);

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("# ShaderStripMod preserve list");
                builder.AppendLine("# Edited by the in-game texture browser.");
                builder.AppendLine("# Press F9 to browse loaded textures/entities, F10 to dump to file, F11 to reload this file.");
                builder.AppendLine("# rendererPath= preserves one exact renderer by scene/object path/type.");
                builder.AppendLine("# path= is exact object only. groupPath= and projectilePath= include children.");
                builder.AppendLine("# sprite=, texture=, and material= are exact asset rules. asset= is a legacy broad rule.");
                builder.AppendLine();
                for (int i = 0; i < rendererRules.Count; i++)
                {
                    builder.AppendLine(BuildRendererPreserveRuleLine(rendererRules[i]));
                }

                AppendBlankLineIfNeeded(builder, rendererRules.Count, paths.Count);
                for (int i = 0; i < paths.Count; i++)
                {
                    builder.AppendLine("path=" + paths[i]);
                }

                AppendBlankLineIfNeeded(builder, paths.Count, groups.Count);
                for (int i = 0; i < groups.Count; i++)
                {
                    builder.AppendLine("groupPath=" + groups[i]);
                }

                AppendBlankLineIfNeeded(builder, paths.Count + groups.Count, projectiles.Count);
                for (int i = 0; i < projectiles.Count; i++)
                {
                    builder.AppendLine("projectilePath=" + projectiles[i]);
                }

                AppendBlankLineIfNeeded(builder, paths.Count + groups.Count + projectiles.Count, sprites.Count);
                for (int i = 0; i < sprites.Count; i++)
                {
                    builder.AppendLine("sprite=" + sprites[i]);
                }

                AppendBlankLineIfNeeded(builder, paths.Count + groups.Count + projectiles.Count + sprites.Count, textures.Count);
                for (int i = 0; i < textures.Count; i++)
                {
                    builder.AppendLine("texture=" + textures[i]);
                }

                AppendBlankLineIfNeeded(builder, paths.Count + groups.Count + projectiles.Count + sprites.Count + textures.Count, materials.Count);
                for (int i = 0; i < materials.Count; i++)
                {
                    builder.AppendLine("material=" + materials[i]);
                }

                AppendBlankLineIfNeeded(builder, paths.Count + groups.Count + projectiles.Count + sprites.Count + textures.Count + materials.Count, entries.Count);
                for (int i = 0; i < entries.Count; i++)
                {
                    builder.AppendLine("asset=" + entries[i]);
                }

                AtomicTextFile.Write(preserveFilePath,builder.ToString());
                Log("ShaderStripMod saved preserve entries to " + preserveFilePath + ": renderers=" + rendererRules.Count + ", paths=" + paths.Count + ", groups=" + groups.Count + ", projectiles=" + projectiles.Count + ", sprites=" + sprites.Count + ", textures=" + textures.Count + ", materials=" + materials.Count + ", legacyAssets=" + entries.Count + ".");
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod preserve list save failed: " + ex.Message);
            }
        }

        private int CompareRendererPreserveRules(RendererPreserveRule left, RendererPreserveRule right)
        {
            return string.Compare(GetRendererPreserveRuleKey(left), GetRendererPreserveRuleKey(right), System.StringComparison.OrdinalIgnoreCase);
        }

        private string BuildRendererPreserveRuleLine(RendererPreserveRule rule)
        {
            if (rule == null)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(rule.Projectile ? "projectileRendererPath=" : "rendererPath=");
            builder.Append(CleanUnityPath(rule.ObjectPath));
            AppendRulePart(builder, "rendererType", CleanUnityName(rule.RendererType));
            AppendRulePart(builder, "scene", CleanUnityName(rule.SceneName));
            AppendRulePart(builder, "sprite", CleanUnityName(rule.SpriteName));
            AppendRulePart(builder, "material", CleanUnityName(rule.MaterialName));
            AppendRulePart(builder, "texture", CleanUnityName(rule.TextureName));
            return builder.ToString();
        }

        private void AppendRulePart(StringBuilder builder, string key, string value)
        {
            if (builder == null || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                return;
            }

            builder.Append('\t');
            builder.Append(key);
            builder.Append('=');
            builder.Append(value);
        }

        private void AppendBlankLineIfNeeded(StringBuilder builder, int previousCount, int nextCount)
        {
            if (previousCount > 0 && nextCount > 0)
            {
                builder.AppendLine();
            }
        }

        private int GetTotalPreserveRuleCount()
        {
            return preservedObjectPaths.Count +
                preservedGroupObjectPaths.Count +
                preservedProjectileObjectPaths.Count +
                preservedRendererRules.Count +
                preservedSpriteNames.Count +
                preservedTextureNames.Count +
                preservedMaterialNames.Count +
                preservedAssetNames.Count;
        }

        private bool HasAnyAssetPreserveRules()
        {
            return preservedAssetNames.Count > 0 ||
                preservedSpriteNames.Count > 0 ||
                preservedTextureNames.Count > 0 ||
                preservedMaterialNames.Count > 0;
        }

        private bool HasPathPreserveRules()
        {
            return preservedObjectPaths.Count > 0 ||
                preservedGroupObjectPaths.Count > 0 ||
                preservedProjectileObjectPaths.Count > 0;
        }

        private bool HasProjectilePreserveRules()
        {
            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                RendererPreserveRule rule = preservedRendererRules[i];
                if (rule != null && rule.Projectile && !IsIgnoredRendererPreserveRule(rule))
                {
                    return true;
                }
            }

            foreach (string path in preservedProjectileObjectPaths)
            {
                if (!IsIgnoredProjectilePreservePath(path))
                {
                    return true;
                }
            }

            return false;
        }

        private int CountEffectiveProjectilePreserveRules()
        {
            int count = 0;
            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                RendererPreserveRule rule = preservedRendererRules[i];
                if (rule != null && rule.Projectile && !IsIgnoredRendererPreserveRule(rule))
                {
                    count++;
                }
            }

            foreach (string path in preservedProjectileObjectPaths)
            {
                if (!IsIgnoredProjectilePreservePath(path))
                {
                    count++;
                }
            }

            return count;
        }

        private float GetPreservedRendererSyncInterval()
        {
            if (string.Equals(projectilePerformanceMode, ProjectilePerformanceCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                return preservedSyncIntervalSetting;
            }

            int projectileRules = CountEffectiveProjectilePreserveRules();
            if (projectileRules <= 0)
            {
                return PreservedRendererSyncInterval;
            }

            if (projectileRules > HeavyProjectileRuleCount)
            {
                return HeavyProjectileRendererSyncInterval;
            }

            if (projectileRules > MediumProjectileRuleCount)
            {
                return MediumProjectileRendererSyncInterval;
            }

            return ProjectileRendererSyncInterval;
        }

        internal void ReloadPreserveList()
        {
            LoadPreserveList();
            LoadSpriteMasks();
            ReloadVisualRulesFoundation();
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

