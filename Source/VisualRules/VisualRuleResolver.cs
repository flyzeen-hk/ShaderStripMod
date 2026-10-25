using System;
using System.Collections.Generic;

namespace ShaderStripMod.VisualRules
{
    internal sealed class VisualRuleMatchDecision
    {
        internal VisualOverrideRule Rule;
        internal bool Matched;
        internal bool Eligible;
        internal bool LegacyIdentity;
        internal int ScopeRank;
        internal int PredicateCount;
        internal string Reason = "";
    }

    internal sealed class ResolvedVisualChannel
    {
        internal VisualRuleChannel Channel;
        internal VisualOverrideRule Winner;
        internal VisualOverrideRule BlockingRule;
        internal readonly List<VisualRuleMatchDecision> Decisions = new List<VisualRuleMatchDecision>();

        internal bool IsBlocked
        {
            get { return BlockingRule != null; }
        }
    }

    internal sealed class ResolvedVisualOverride
    {
        internal readonly ResolvedVisualChannel Color = new ResolvedVisualChannel { Channel = VisualRuleChannel.Color };
        internal readonly ResolvedVisualChannel Replacement = new ResolvedVisualChannel { Channel = VisualRuleChannel.Replacement };
        internal readonly ResolvedVisualChannel StaticDisplay = new ResolvedVisualChannel { Channel = VisualRuleChannel.StaticDisplay };
    }

    internal static class VisualRuleResolver
    {
        internal static ResolvedVisualOverride Resolve(IEnumerable<VisualOverrideRule> rules, RendererIdentity identity)
        {
            if (rules == null) throw new ArgumentNullException("rules");
            if (identity == null) throw new ArgumentNullException("identity");

            List<VisualOverrideRule> snapshot = new List<VisualOverrideRule>(rules);
            ResolvedVisualOverride resolved = new ResolvedVisualOverride();
            ResolveChannel(snapshot, identity, resolved.Color);
            ResolveChannel(snapshot, identity, resolved.Replacement);
            ResolveChannel(snapshot, identity, resolved.StaticDisplay);
            return resolved;
        }

        private static void ResolveChannel(List<VisualOverrideRule> rules, RendererIdentity identity, ResolvedVisualChannel channel)
        {
            List<VisualRuleMatchDecision> eligible = new List<VisualRuleMatchDecision>();
            for (int i = 0; i < rules.Count; i++)
            {
                VisualOverrideRule rule = rules[i];
                if (rule == null || !TargetsChannel(rule, channel.Channel))
                {
                    continue;
                }

                VisualRuleMatchDecision decision = BuildDecision(rule, identity);
                channel.Decisions.Add(decision);
                if (decision.Eligible)
                {
                    eligible.Add(decision);
                }
            }

            if (eligible.Count == 0)
            {
                return;
            }

            eligible.Sort(CompareCandidates);
            VisualOverrideRule selected = eligible[0].Rule;
            if (selected.RuleCategory == VisualRuleCategory.OriginalBlock)
            {
                channel.BlockingRule = selected;
            }
            else
            {
                channel.Winner = selected;
            }

            for (int i = 0; i < eligible.Count; i++)
            {
                VisualRuleMatchDecision decision = eligible[i];
                decision.Reason = ReferenceEquals(decision.Rule, selected)
                    ? (selected.RuleCategory == VisualRuleCategory.OriginalBlock ? "Selected channel-specific OriginalBlock." : "Selected channel winner.")
                    : "Matched but lost deterministic priority comparison.";
            }
        }

        private static VisualRuleMatchDecision BuildDecision(VisualOverrideRule rule, RendererIdentity identity)
        {
            VisualRuleMatchDecision decision = new VisualRuleMatchDecision();
            decision.Rule = rule;
            decision.ScopeRank = GetScopeRank(rule.Scope);
            decision.PredicateCount = CountIdentityPredicates(rule);
            decision.LegacyIdentity = IsLegacyExactIdentity(rule);

            if (!rule.Enabled)
            {
                decision.Reason = "Rule is disabled.";
                return decision;
            }

            string mismatch;
            if (!MatchesScope(rule, identity, out mismatch))
            {
                decision.Reason = mismatch;
                return decision;
            }

            decision.Matched = true;
            bool exact = rule.Scope == VisualRuleScope.Renderer || rule.Scope == VisualRuleScope.Object;
            if (identity.IsPreserved && !exact && !rule.IncludePreserved)
            {
                decision.Reason = "Matched broad rule, but Preserve protection requires includePreserved=true.";
                return decision;
            }

            decision.Eligible = true;
            decision.Reason = decision.LegacyIdentity
                ? "Matched legacy exact identity; caller must check ambiguity across the candidate set."
                : "Matched and eligible.";
            return decision;
        }

        private static bool TargetsChannel(VisualOverrideRule rule, VisualRuleChannel channel)
        {
            return (rule.GetCategoryChannel() & channel) != 0;
        }

        private static int CompareCandidates(VisualRuleMatchDecision left, VisualRuleMatchDecision right)
        {
            bool leftBlock = left.Rule.RuleCategory == VisualRuleCategory.OriginalBlock;
            bool rightBlock = right.Rule.RuleCategory == VisualRuleCategory.OriginalBlock;
            if (leftBlock != rightBlock) return leftBlock ? -1 : 1;

            int result = right.ScopeRank.CompareTo(left.ScopeRank);
            if (result != 0) return result;
            result = right.Rule.Priority.CompareTo(left.Rule.Priority);
            if (result != 0) return result;
            result = right.PredicateCount.CompareTo(left.PredicateCount);
            if (result != 0) return result;
            result = left.Rule.StableOrder.CompareTo(right.Rule.StableOrder);
            if (result != 0) return result;
            return StringComparer.Ordinal.Compare(left.Rule.Id ?? "", right.Rule.Id ?? "");
        }

        private static bool MatchesScope(VisualOverrideRule rule, RendererIdentity identity, out string reason)
        {
            switch (rule.Scope)
            {
                case VisualRuleScope.Renderer:
                    if (string.IsNullOrEmpty(rule.SceneName) || string.IsNullOrEmpty(rule.ObjectPath) || string.IsNullOrEmpty(rule.RendererType))
                    {
                        reason = "Renderer scope requires sceneName, objectPath, and rendererType.";
                        return false;
                    }

                    if (!EqualsIgnoreCase(rule.SceneName, identity.SceneName) ||
                        !EqualsIgnoreCase(rule.ObjectPath, identity.ObjectPath) ||
                        !OptionalEquals(rule.HierarchyDiscriminator, identity.HierarchyDiscriminator) ||
                        !EqualsIgnoreCase(rule.RendererType, identity.RendererType) ||
                        (rule.RendererComponentIndex >= 0 && rule.RendererComponentIndex != identity.RendererComponentIndex) ||
                        !MatchesMaterialTarget(rule, identity))
                    {
                        reason = "Renderer identity does not match.";
                        return false;
                    }
                    break;

                case VisualRuleScope.Object:
                    if (string.IsNullOrEmpty(rule.SceneName) || string.IsNullOrEmpty(rule.ObjectPath))
                    {
                        reason = "Object scope requires sceneName and objectPath.";
                        return false;
                    }

                    if (!EqualsIgnoreCase(rule.SceneName, identity.SceneName) ||
                        !EqualsIgnoreCase(rule.ObjectPath, identity.ObjectPath) ||
                        !OptionalEquals(rule.HierarchyDiscriminator, identity.HierarchyDiscriminator))
                    {
                        reason = "Object identity does not match.";
                        return false;
                    }
                    break;

                case VisualRuleScope.ProjectileFamily:
                    if (string.IsNullOrEmpty(rule.ProjectileFamily) || !EqualsIgnoreCase(rule.ProjectileFamily, identity.ProjectileFamily))
                    {
                        reason = "Projectile family does not match.";
                        return false;
                    }
                    break;

                case VisualRuleScope.Group:
                    if (string.IsNullOrEmpty(rule.ObjectPath) ||
                        (!string.IsNullOrEmpty(rule.SceneName) && !EqualsIgnoreCase(rule.SceneName, identity.SceneName)) ||
                        !PathMatchesGroup(identity.ObjectPath, rule.ObjectPath))
                    {
                        reason = "Group path does not match on a segment boundary.";
                        return false;
                    }
                    break;

                case VisualRuleScope.Sprite:
                    if (string.IsNullOrEmpty(rule.SpriteName) || !EqualsIgnoreCase(rule.SpriteName, identity.SpriteName))
                    {
                        reason = "Sprite name does not match.";
                        return false;
                    }
                    break;

                case VisualRuleScope.Texture:
                    if (string.IsNullOrEmpty(rule.TextureName) ||
                        !EqualsIgnoreCase(rule.TextureName, identity.TextureName) ||
                        (rule.TextureWidth >= 0 && rule.TextureWidth != identity.TextureWidth) ||
                        (rule.TextureHeight >= 0 && rule.TextureHeight != identity.TextureHeight) ||
                        !OptionalEquals(rule.TexturePropertyName, identity.TexturePropertyName))
                    {
                        reason = "Texture identity does not match.";
                        return false;
                    }
                    break;

                case VisualRuleScope.Material:
                    if (string.IsNullOrEmpty(rule.MaterialName) || !EqualsIgnoreCase(rule.MaterialName, identity.MaterialName))
                    {
                        reason = "Material name does not match.";
                        return false;
                    }
                    break;

                case VisualRuleScope.Global:
                    break;

                default:
                    reason = "Unsupported rule scope.";
                    return false;
            }

            reason = "Matched.";
            return true;
        }

        private static bool MatchesMaterialTarget(VisualOverrideRule rule, RendererIdentity identity)
        {
            return (rule.MaterialIndex < 0 || rule.MaterialIndex == identity.MaterialIndex) &&
                OptionalEquals(rule.TexturePropertyName, identity.TexturePropertyName) &&
                OptionalEquals(rule.SpriteName, identity.SpriteName) &&
                OptionalEquals(rule.TextureName, identity.TextureName) &&
                OptionalEquals(rule.MaterialName, identity.MaterialName);
        }

        private static bool PathMatchesGroup(string path, string group)
        {
            if (EqualsIgnoreCase(path, group)) return true;
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(group) || path.Length <= group.Length) return false;
            return path.StartsWith(group, StringComparison.OrdinalIgnoreCase) && path[group.Length] == '/';
        }

        private static int GetScopeRank(VisualRuleScope scope)
        {
            switch (scope)
            {
                case VisualRuleScope.Renderer: return 900;
                case VisualRuleScope.Object: return 800;
                case VisualRuleScope.ProjectileFamily: return 700;
                case VisualRuleScope.Group: return 600;
                case VisualRuleScope.Sprite: return 500;
                case VisualRuleScope.Texture: return 400;
                case VisualRuleScope.Material: return 300;
                case VisualRuleScope.Global: return 200;
                default: return 0;
            }
        }

        private static int CountIdentityPredicates(VisualOverrideRule rule)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(rule.SceneName)) count++;
            if (!string.IsNullOrEmpty(rule.ObjectPath)) count++;
            if (!string.IsNullOrEmpty(rule.HierarchyDiscriminator)) count++;
            if (!string.IsNullOrEmpty(rule.RendererType)) count++;
            if (rule.RendererComponentIndex >= 0) count++;
            if (rule.MaterialIndex >= 0) count++;
            if (!string.IsNullOrEmpty(rule.TexturePropertyName)) count++;
            if (!string.IsNullOrEmpty(rule.SpriteName)) count++;
            if (!string.IsNullOrEmpty(rule.TextureName)) count++;
            if (rule.TextureWidth >= 0) count++;
            if (rule.TextureHeight >= 0) count++;
            if (!string.IsNullOrEmpty(rule.MaterialName)) count++;
            if (!string.IsNullOrEmpty(rule.ProjectileFamily)) count++;
            return count;
        }

        private static bool IsLegacyExactIdentity(VisualOverrideRule rule)
        {
            return (rule.Scope == VisualRuleScope.Renderer || rule.Scope == VisualRuleScope.Object) &&
                (rule.IdentityVersion < RendererIdentity.CurrentIdentityVersion || string.IsNullOrEmpty(rule.HierarchyDiscriminator));
        }

        private static bool OptionalEquals(string expected, string actual)
        {
            return string.IsNullOrEmpty(expected) || EqualsIgnoreCase(expected, actual);
        }

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return string.Equals(left ?? "", right ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }
}
