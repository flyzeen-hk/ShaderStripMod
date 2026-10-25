using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ShaderStripMod.VisualRules;

namespace ShaderStripMod.Runtime
{
    internal sealed class VisualRuleLoadState
    {
        internal bool CreatedFile;
        internal bool UsedPartialDocument;
        internal bool KeptPreviousDocument;
        internal int ActiveRuleCount;
        internal int ErrorCount;
        internal int WarningCount;
        internal string Message = "";
    }

    internal sealed class VisualRuleRuntimeManager
    {
        private const int MaxResolutionCacheEntries = 4096;

        private sealed class RuleIndex
        {
            private readonly Dictionary<string, List<VisualOverrideRule>> renderers = NewIndex();
            private readonly Dictionary<string, List<VisualOverrideRule>> objects = NewIndex();
            private readonly Dictionary<string, List<VisualOverrideRule>> projectiles = NewIndex();
            private readonly Dictionary<string, List<VisualOverrideRule>> sprites = NewIndex();
            private readonly Dictionary<string, List<VisualOverrideRule>> textures = NewIndex();
            private readonly Dictionary<string, List<VisualOverrideRule>> materials = NewIndex();
            private readonly List<VisualOverrideRule> groups = new List<VisualOverrideRule>();
            private readonly List<VisualOverrideRule> globals = new List<VisualOverrideRule>();
            private readonly List<VisualOverrideRule> fallback = new List<VisualOverrideRule>();

            internal void Build(IList<VisualOverrideRule> rules)
            {
                Clear();
                if (rules == null) return;
                for (int i = 0; i < rules.Count; i++)
                {
                    VisualOverrideRule rule = rules[i];
                    if (rule == null) continue;
                    switch (rule.Scope)
                    {
                        case VisualRuleScope.Renderer:
                            AddExact(renderers, BuildScenePathKey(rule.SceneName, rule.ObjectPath), rule);
                            break;
                        case VisualRuleScope.Object:
                            AddExact(objects, BuildScenePathKey(rule.SceneName, rule.ObjectPath), rule);
                            break;
                        case VisualRuleScope.ProjectileFamily:
                            AddNamed(projectiles, rule.ProjectileFamily, rule);
                            break;
                        case VisualRuleScope.Group:
                            groups.Add(rule);
                            break;
                        case VisualRuleScope.Sprite:
                            AddNamed(sprites, rule.SpriteName, rule);
                            break;
                        case VisualRuleScope.Texture:
                            AddNamed(textures, rule.TextureName, rule);
                            break;
                        case VisualRuleScope.Material:
                            AddNamed(materials, rule.MaterialName, rule);
                            break;
                        case VisualRuleScope.Global:
                            globals.Add(rule);
                            break;
                        default:
                            fallback.Add(rule);
                            break;
                    }
                }
            }

            internal List<VisualOverrideRule> GetCandidates(RendererIdentity identity)
            {
                List<VisualOverrideRule> result = new List<VisualOverrideRule>();
                HashSet<VisualOverrideRule> seen = new HashSet<VisualOverrideRule>();
                Add(result, seen, globals);
                Add(result, seen, fallback);
                Add(result, seen, groups);
                Add(result, seen, renderers, BuildScenePathKey(identity.SceneName, identity.ObjectPath));
                Add(result, seen, objects, BuildScenePathKey(identity.SceneName, identity.ObjectPath));
                Add(result, seen, projectiles, identity.ProjectileFamily);
                Add(result, seen, sprites, identity.SpriteName);
                Add(result, seen, textures, identity.TextureName);
                Add(result, seen, materials, identity.MaterialName);
                return result;
            }

            private void AddExact(Dictionary<string, List<VisualOverrideRule>> index, string key, VisualOverrideRule rule)
            {
                if (string.IsNullOrEmpty(rule.SceneName) || string.IsNullOrEmpty(rule.ObjectPath))
                {
                    fallback.Add(rule);
                    return;
                }

                AddToIndex(index, key, rule);
            }

            private void AddNamed(Dictionary<string, List<VisualOverrideRule>> index, string key, VisualOverrideRule rule)
            {
                if (string.IsNullOrEmpty(key))
                {
                    fallback.Add(rule);
                    return;
                }

                AddToIndex(index, key, rule);
            }

            private static void AddToIndex(Dictionary<string, List<VisualOverrideRule>> index, string key, VisualOverrideRule rule)
            {
                List<VisualOverrideRule> bucket;
                if (!index.TryGetValue(key ?? "", out bucket))
                {
                    bucket = new List<VisualOverrideRule>();
                    index.Add(key ?? "", bucket);
                }

                bucket.Add(rule);
            }

            private static void Add(List<VisualOverrideRule> target, HashSet<VisualOverrideRule> seen, Dictionary<string, List<VisualOverrideRule>> index, string key)
            {
                List<VisualOverrideRule> bucket;
                if (index.TryGetValue(key ?? "", out bucket)) Add(target, seen, bucket);
            }

            private static void Add(List<VisualOverrideRule> target, HashSet<VisualOverrideRule> seen, IList<VisualOverrideRule> source)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    VisualOverrideRule rule = source[i];
                    if (rule != null && seen.Add(rule)) target.Add(rule);
                }
            }

            private void Clear()
            {
                renderers.Clear();
                objects.Clear();
                projectiles.Clear();
                sprites.Clear();
                textures.Clear();
                materials.Clear();
                groups.Clear();
                globals.Clear();
                fallback.Clear();
            }

            private static Dictionary<string, List<VisualOverrideRule>> NewIndex()
            {
                return new Dictionary<string, List<VisualOverrideRule>>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private readonly string rulesPath;
        private readonly string backupDirectory;
        private readonly RuleIndex index = new RuleIndex();
        private readonly Dictionary<string, ResolvedVisualOverride> resolutionCache =
            new Dictionary<string, ResolvedVisualOverride>(StringComparer.Ordinal);
        private readonly List<VisualOverrideRule> activeRules = new List<VisualOverrideRule>();
        private readonly List<VisualRuleDiagnostic> diagnostics = new List<VisualRuleDiagnostic>();

        internal VisualRuleRuntimeManager(string rulesPath, string backupDirectory)
        {
            if (string.IsNullOrWhiteSpace(rulesPath)) throw new ArgumentException("A visual-rules path is required.", nameof(rulesPath));
            this.rulesPath = Path.GetFullPath(rulesPath);
            this.backupDirectory = string.IsNullOrWhiteSpace(backupDirectory) ? "" : Path.GetFullPath(backupDirectory);
        }

        internal string RulesPath => rulesPath;
        internal string BackupDirectory => backupDirectory;
        internal int ActiveRuleCount => activeRules.Count;
        internal int DiagnosticCount => diagnostics.Count;
        internal int ResolutionCacheCount => resolutionCache.Count;
        internal int ResolutionCacheHits { get; private set; }
        internal int ResolutionCacheMisses { get; private set; }
        internal int ResolutionCacheEvictions { get; private set; }
        internal string LastMessage { get; private set; } = "Not initialized.";
        internal IList<VisualRuleDiagnostic> Diagnostics => diagnostics.AsReadOnly();

        internal VisualRuleLoadState Initialize()
        {
            bool created = false;
            if (!File.Exists(rulesPath))
            {
                VisualRuleFileStore.SaveAtomic(rulesPath, new VisualRuleDocument(), backupDirectory);
                created = true;
            }

            VisualRuleLoadState state = Reload();
            state.CreatedFile = created;
            return state;
        }

        internal VisualRuleLoadState Reload()
        {
            VisualRuleLoadState state = new VisualRuleLoadState();
            VisualRuleParseResult parsed;
            try
            {
                parsed = VisualRuleFileStore.Load(rulesPath);
            }
            catch (Exception ex)
            {
                diagnostics.Clear();
                diagnostics.Add(new VisualRuleDiagnostic
                {
                    Severity = VisualRuleDiagnosticSeverity.Error,
                    Message = "Visual-rules read failed: " + ex.Message
                });
                state.ErrorCount = 1;
                state.KeptPreviousDocument = activeRules.Count > 0;
                state.ActiveRuleCount = activeRules.Count;
                state.Message = state.KeptPreviousDocument
                    ? "Read failed; previous active rules were kept."
                    : "Read failed; no visual rules are active.";
                LastMessage = state.Message;
                return state;
            }

            diagnostics.Clear();
            for (int i = 0; i < parsed.Diagnostics.Count; i++)
            {
                VisualRuleDiagnostic diagnostic = parsed.Diagnostics[i];
                diagnostics.Add(diagnostic);
                if (diagnostic.Severity == VisualRuleDiagnosticSeverity.Error) state.ErrorCount++;
                if (diagnostic.Severity == VisualRuleDiagnosticSeverity.Warning) state.WarningCount++;
            }

            bool mayReplace = parsed.Success || parsed.Document.Rules.Count > 0 || activeRules.Count == 0;
            if (mayReplace)
            {
                activeRules.Clear();
                activeRules.AddRange(parsed.Document.Rules);
                index.Build(activeRules);
                state.UsedPartialDocument = !parsed.Success && activeRules.Count > 0;
                InvalidateResolutionCache();
            }
            else
            {
                state.KeptPreviousDocument = true;
            }

            state.ActiveRuleCount = activeRules.Count;
            state.Message = BuildLoadMessage(state);
            LastMessage = state.Message;
            return state;
        }

        internal ResolvedVisualOverride Resolve(RendererIdentity identity)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            string key = BuildResolutionCacheKey(identity);
            ResolvedVisualOverride cached;
            if (resolutionCache.TryGetValue(key, out cached))
            {
                ResolutionCacheHits++;
                return cached;
            }

            ResolutionCacheMisses++;
            List<VisualOverrideRule> candidates = index.GetCandidates(identity);
            ResolvedVisualOverride resolved = VisualRuleResolver.Resolve(candidates, identity);
            if (resolutionCache.Count >= MaxResolutionCacheEntries)
            {
                resolutionCache.Clear();
                ResolutionCacheEvictions++;
            }

            resolutionCache[key] = resolved;
            return resolved;
        }

        internal void InvalidateResolutionCache()
        {
            resolutionCache.Clear();
            ResolutionCacheHits = 0;
            ResolutionCacheMisses = 0;
        }

        private static string BuildLoadMessage(VisualRuleLoadState state)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Visual rules active=").Append(state.ActiveRuleCount);
            builder.Append(", errors=").Append(state.ErrorCount);
            builder.Append(", warnings=").Append(state.WarningCount);
            if (state.UsedPartialDocument) builder.Append("; valid lines loaded, invalid lines isolated");
            if (state.KeptPreviousDocument) builder.Append("; previous active document kept");
            return builder.Append('.').ToString();
        }

        private static string BuildResolutionCacheKey(RendererIdentity identity)
        {
            StringBuilder builder = new StringBuilder(identity.BuildStableRendererKey());
            AppendCachePart(builder, identity.SpriteName);
            AppendCachePart(builder, identity.TextureName);
            AppendCachePart(builder, identity.TextureWidth.ToString());
            AppendCachePart(builder, identity.TextureHeight.ToString());
            AppendCachePart(builder, identity.MaterialName);
            AppendCachePart(builder, identity.ProjectileFamily);
            AppendCachePart(builder, identity.IsPreserved ? "1" : "0");
            return builder.ToString();
        }

        private static void AppendCachePart(StringBuilder builder, string value)
        {
            string safe = value ?? "";
            builder.Append('|').Append(safe.Length).Append(':').Append(safe);
        }

        private static string BuildScenePathKey(string scene, string path)
        {
            return (scene ?? "") + "\n" + (path ?? "");
        }
    }
}
