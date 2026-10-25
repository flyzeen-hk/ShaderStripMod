using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ShaderStripMod.VisualRules
{
    internal static class VisualRuleCodec
    {
        private static readonly HashSet<string> KnownFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id", "enabled", "ruleCategory", "channelMask", "scope", "priority", "stableOrder",
            "identityVersion", "sceneName", "objectPath", "hierarchyDiscriminator", "rendererType",
            "rendererComponentIndex", "materialIndex", "texturePropertyName", "spriteName", "textureName",
            "textureWidth", "textureHeight", "materialName", "projectileFamily", "colorMode", "color",
            "intensity", "preserveAlpha", "replacementRelativePath", "placementMode", "offsetX", "offsetY",
            "scaleX", "scaleY", "rotation", "anchorX", "anchorY", "clipToOriginalBounds", "animationMode",
            "frameIdentity", "includePreserved", "createdVersion", "comment", "displayName"
        };

        internal static VisualRuleParseResult Parse(string text)
        {
            VisualRuleParseResult result = new VisualRuleParseResult();
            result.Document.Rules.Clear();
            result.Document.HeaderComments.Clear();

            string normalized = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split('\n');
            bool formatSeen = false;
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNumber = i + 1;
                string raw = lines[i];
                string trimmed = raw.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith("//", StringComparison.Ordinal))
                {
                    if (!formatSeen)
                    {
                        result.Document.HeaderComments.Add(raw);
                    }

                    continue;
                }

                if (!formatSeen)
                {
                    int separator = trimmed.IndexOf('=');
                    int formatVersion;
                    if (separator <= 0 ||
                        !string.Equals(trimmed.Substring(0, separator).Trim(), "formatVersion", StringComparison.OrdinalIgnoreCase) ||
                        !int.TryParse(trimmed.Substring(separator + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out formatVersion))
                    {
                        AddDiagnostic(result, lineNumber, VisualRuleDiagnosticSeverity.Error, "The first data line must be formatVersion=1.");
                        return result;
                    }

                    result.Document.FormatVersion = formatVersion;
                    formatSeen = true;
                    if (formatVersion != VisualRuleDocument.SupportedFormatVersion)
                    {
                        AddDiagnostic(result, lineNumber, VisualRuleDiagnosticSeverity.Error,
                            "Unsupported visual-rules formatVersion " + formatVersion + ". Supported version is " + VisualRuleDocument.SupportedFormatVersion + ".");
                        return result;
                    }

                    continue;
                }

                VisualOverrideRule rule;
                string error;
                if (!TryParseRuleLine(raw, lineNumber, out rule, out error))
                {
                    AddDiagnostic(result, lineNumber, VisualRuleDiagnosticSeverity.Error, error);
                    continue;
                }

                if (!ids.Add(rule.Id))
                {
                    AddDiagnostic(result, lineNumber, VisualRuleDiagnosticSeverity.Error, "Duplicate rule id '" + rule.Id + "'.");
                    continue;
                }

                result.Document.Rules.Add(rule);
            }

            if (!formatSeen)
            {
                AddDiagnostic(result, 0, VisualRuleDiagnosticSeverity.Error, "Missing formatVersion=1 header.");
            }

            return result;
        }

        internal static string Serialize(VisualRuleDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            if (document.FormatVersion != VisualRuleDocument.SupportedFormatVersion)
            {
                throw new InvalidOperationException("Unsupported visual-rules formatVersion " + document.FormatVersion + ".");
            }

            List<VisualOverrideRule> sorted = new List<VisualOverrideRule>(document.Rules);
            sorted.Sort(CompareRulesForStorage);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# ShaderStripMod visual rules");
            builder.AppendLine("formatVersion=" + document.FormatVersion.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < document.HeaderComments.Count; i++)
            {
                string comment = document.HeaderComments[i] ?? "";
                if (!comment.StartsWith("#", StringComparison.Ordinal) && !comment.StartsWith("//", StringComparison.Ordinal))
                {
                    comment = "# " + comment;
                }

                if (!string.Equals(comment, "# ShaderStripMod visual rules", StringComparison.Ordinal))
                {
                    builder.AppendLine(comment);
                }
            }

            builder.AppendLine();
            for (int i = 0; i < sorted.Count; i++)
            {
                builder.AppendLine(SerializeRule(sorted[i]));
            }

            return builder.ToString();
        }

        internal static string EscapeValue(string value)
        {
            StringBuilder builder = new StringBuilder((value ?? "").Length + 8);
            string source = value ?? "";
            for (int i = 0; i < source.Length; i++)
            {
                switch (source[i])
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '=': builder.Append("\\="); break;
                    default: builder.Append(source[i]); break;
                }
            }

            return builder.ToString();
        }

        internal static bool TryUnescapeValue(string value, out string result, out string error)
        {
            StringBuilder builder = new StringBuilder((value ?? "").Length);
            string source = value ?? "";
            for (int i = 0; i < source.Length; i++)
            {
                char current = source[i];
                if (current != '\\')
                {
                    builder.Append(current);
                    continue;
                }

                if (++i >= source.Length)
                {
                    result = "";
                    error = "Dangling escape character at the end of a value.";
                    return false;
                }

                switch (source[i])
                {
                    case '\\': builder.Append('\\'); break;
                    case 't': builder.Append('\t'); break;
                    case 'r': builder.Append('\r'); break;
                    case 'n': builder.Append('\n'); break;
                    case '=': builder.Append('='); break;
                    default:
                        result = "";
                        error = "Unknown escape sequence \\" + source[i] + ".";
                        return false;
                }
            }

            result = builder.ToString();
            error = "";
            return true;
        }

        private static bool TryParseRuleLine(string raw, int lineNumber, out VisualOverrideRule rule, out string error)
        {
            rule = null;
            error = "";
            string[] parts = raw.Split('\t');
            if (parts.Length < 2 || !string.Equals(parts[0].Trim(), "rule", StringComparison.OrdinalIgnoreCase))
            {
                error = "Expected a tab-separated rule line beginning with 'rule'.";
                return false;
            }

            VisualOverrideRule parsed = new VisualOverrideRule();
            parsed.StableOrder = lineNumber;
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < parts.Length; i++)
            {
                string token = parts[i];
                int separator = FindUnescapedEquals(token);
                if (separator <= 0)
                {
                    error = "Invalid field token '" + token + "'. Expected key=value.";
                    return false;
                }

                string key = token.Substring(0, separator).Trim();
                if (!seen.Add(key))
                {
                    error = "Duplicate field '" + key + "'.";
                    return false;
                }

                string value;
                string unescapeError;
                if (!TryUnescapeValue(token.Substring(separator + 1), out value, out unescapeError))
                {
                    error = "Invalid value for '" + key + "': " + unescapeError;
                    return false;
                }

                if (!TryApplyField(parsed, key, value, out error))
                {
                    return false;
                }
            }

            parsed.Id = (parsed.Id ?? "").Trim();
            if (parsed.Id.Length == 0)
            {
                error = "Rule id is required.";
                return false;
            }

            if (!seen.Contains("ruleCategory") || !seen.Contains("scope"))
            {
                error = "ruleCategory and scope are required.";
                return false;
            }

            if (parsed.IdentityVersion <= 0)
            {
                error = "identityVersion must be positive.";
                return false;
            }

            if (parsed.RuleCategory == VisualRuleCategory.OriginalBlock)
            {
                if (parsed.ChannelMask == VisualRuleChannel.None || (parsed.ChannelMask & ~VisualRuleChannel.All) != 0)
                {
                    error = "OriginalBlock requires channelMask=Color, Replacement, StaticDisplay, or All.";
                    return false;
                }

                if (parsed.Scope != VisualRuleScope.Renderer && parsed.Scope != VisualRuleScope.Object)
                {
                    error = "OriginalBlock is allowed only for Renderer or Object scope.";
                    return false;
                }
            }
            else
            {
                parsed.ChannelMask = parsed.GetCategoryChannel();
            }

            NormalizeRuleIdentity(parsed);
            rule = parsed;
            return true;
        }

        private static int FindUnescapedEquals(string token)
        {
            bool escaped = false;
            for (int i = 0; i < token.Length; i++)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (token[i] == '\\')
                {
                    escaped = true;
                }
                else if (token[i] == '=')
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryApplyField(VisualOverrideRule rule, string key, string value, out string error)
        {
            error = "";
            bool boolValue;
            int intValue;
            long longValue;
            float floatValue;
            VisualRuleCategory category;
            VisualRuleChannel channel;
            VisualRuleScope scope;

            switch (key.ToLowerInvariant())
            {
                case "id": rule.Id = value; return true;
                case "enabled":
                    if (!TryParseBool(value, out boolValue)) return Invalid(key, value, out error);
                    rule.Enabled = boolValue; return true;
                case "rulecategory":
                    if (!Enum.TryParse(value, true, out category)) return Invalid(key, value, out error);
                    rule.RuleCategory = category; return true;
                case "channelmask":
                    if (!Enum.TryParse(value, true, out channel)) return Invalid(key, value, out error);
                    rule.ChannelMask = channel; return true;
                case "scope":
                    if (!Enum.TryParse(value, true, out scope)) return Invalid(key, value, out error);
                    rule.Scope = scope; return true;
                case "priority":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) return Invalid(key, value, out error);
                    rule.Priority = intValue; return true;
                case "stableorder":
                    if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue)) return Invalid(key, value, out error);
                    rule.StableOrder = longValue; return true;
                case "identityversion":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) return Invalid(key, value, out error);
                    rule.IdentityVersion = intValue; return true;
            }

            switch (key.ToLowerInvariant())
            {
                case "scenename": rule.SceneName = value; return true;
                case "objectpath": rule.ObjectPath = value; return true;
                case "hierarchydiscriminator": rule.HierarchyDiscriminator = value; return true;
                case "renderertype": rule.RendererType = value; return true;
                case "renderercomponentindex":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) return Invalid(key, value, out error);
                    rule.RendererComponentIndex = intValue; return true;
                case "materialindex":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) return Invalid(key, value, out error);
                    rule.MaterialIndex = intValue; return true;
                case "texturepropertyname": rule.TexturePropertyName = value; return true;
                case "spritename": rule.SpriteName = value; return true;
                case "texturename": rule.TextureName = value; return true;
                case "texturewidth":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) return Invalid(key, value, out error);
                    rule.TextureWidth = intValue; return true;
                case "textureheight":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) return Invalid(key, value, out error);
                    rule.TextureHeight = intValue; return true;
                case "materialname": rule.MaterialName = value; return true;
                case "projectilefamily": rule.ProjectileFamily = value; return true;
                case "colormode": rule.ColorMode = value; return true;
                case "color": rule.Color = value; return true;
                case "intensity":
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue)) return Invalid(key, value, out error);
                    rule.Intensity = floatValue; return true;
                case "preservealpha":
                    if (!TryParseBool(value, out boolValue)) return Invalid(key, value, out error);
                    rule.PreserveAlpha = boolValue; return true;
                case "replacementrelativepath": rule.ReplacementRelativePath = value; return true;
                case "placementmode": rule.PlacementMode = value; return true;
                case "offsetx": return SetFloat(key, value, out rule.OffsetX, out error);
                case "offsety": return SetFloat(key, value, out rule.OffsetY, out error);
                case "scalex": return SetFloat(key, value, out rule.ScaleX, out error);
                case "scaley": return SetFloat(key, value, out rule.ScaleY, out error);
                case "rotation": return SetFloat(key, value, out rule.Rotation, out error);
                case "anchorx": return SetFloat(key, value, out rule.AnchorX, out error);
                case "anchory": return SetFloat(key, value, out rule.AnchorY, out error);
                case "cliptooriginalbounds":
                    if (!TryParseBool(value, out boolValue)) return Invalid(key, value, out error);
                    rule.ClipToOriginalBounds = boolValue; return true;
                case "animationmode": rule.AnimationMode = value; return true;
                case "frameidentity": rule.FrameIdentity = value; return true;
                case "includepreserved":
                    if (!TryParseBool(value, out boolValue)) return Invalid(key, value, out error);
                    rule.IncludePreserved = boolValue; return true;
                case "createdversion": rule.CreatedVersion = value; return true;
                case "comment": rule.Comment = value; return true;
                case "displayname": rule.DisplayName = value; return true;
                default:
                    rule.ExtraFields[key] = value;
                    return true;
            }
        }

        private static string SerializeRule(VisualOverrideRule rule)
        {
            if (rule == null)
            {
                throw new InvalidOperationException("visual-rules document contains a null rule.");
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("rule");
            AppendField(builder, "id", rule.Id);
            AppendField(builder, "enabled", FormatBool(rule.Enabled));
            AppendField(builder, "ruleCategory", rule.RuleCategory.ToString());
            AppendField(builder, "channelMask", (rule.RuleCategory == VisualRuleCategory.OriginalBlock ? rule.ChannelMask : rule.GetCategoryChannel()).ToString());
            AppendField(builder, "scope", rule.Scope.ToString());
            AppendField(builder, "priority", rule.Priority.ToString(CultureInfo.InvariantCulture));
            AppendField(builder, "stableOrder", rule.StableOrder.ToString(CultureInfo.InvariantCulture));
            AppendField(builder, "identityVersion", rule.IdentityVersion.ToString(CultureInfo.InvariantCulture));
            AppendOptional(builder, "sceneName", RendererIdentityBuilder.NormalizeName(rule.SceneName));
            AppendOptional(builder, "objectPath", RendererIdentityBuilder.NormalizePath(rule.ObjectPath));
            AppendOptional(builder, "hierarchyDiscriminator", RendererIdentityBuilder.NormalizePath(rule.HierarchyDiscriminator));
            AppendOptional(builder, "rendererType", RendererIdentityBuilder.NormalizeName(rule.RendererType));
            if (rule.RendererComponentIndex >= 0) AppendField(builder, "rendererComponentIndex", rule.RendererComponentIndex.ToString(CultureInfo.InvariantCulture));
            if (rule.MaterialIndex >= 0) AppendField(builder, "materialIndex", rule.MaterialIndex.ToString(CultureInfo.InvariantCulture));
            AppendOptional(builder, "texturePropertyName", RendererIdentityBuilder.NormalizeName(rule.TexturePropertyName));
            AppendOptional(builder, "spriteName", RendererIdentityBuilder.NormalizeUnityName(rule.SpriteName));
            AppendOptional(builder, "textureName", RendererIdentityBuilder.NormalizeUnityName(rule.TextureName));
            if (rule.TextureWidth >= 0) AppendField(builder, "textureWidth", rule.TextureWidth.ToString(CultureInfo.InvariantCulture));
            if (rule.TextureHeight >= 0) AppendField(builder, "textureHeight", rule.TextureHeight.ToString(CultureInfo.InvariantCulture));
            AppendOptional(builder, "materialName", RendererIdentityBuilder.NormalizeUnityName(rule.MaterialName));
            AppendOptional(builder, "projectileFamily", RendererIdentityBuilder.NormalizePath(rule.ProjectileFamily));
            AppendOptional(builder, "colorMode", rule.ColorMode);
            AppendOptional(builder, "color", rule.Color);
            AppendField(builder, "intensity", rule.Intensity.ToString("R", CultureInfo.InvariantCulture));
            AppendField(builder, "preserveAlpha", FormatBool(rule.PreserveAlpha));
            AppendOptional(builder, "replacementRelativePath", rule.ReplacementRelativePath);
            AppendOptional(builder, "placementMode", rule.PlacementMode);
            AppendField(builder, "offsetX", rule.OffsetX.ToString("R", CultureInfo.InvariantCulture));
            AppendField(builder, "offsetY", rule.OffsetY.ToString("R", CultureInfo.InvariantCulture));
            AppendField(builder, "scaleX", rule.ScaleX.ToString("R", CultureInfo.InvariantCulture));
            AppendField(builder, "scaleY", rule.ScaleY.ToString("R", CultureInfo.InvariantCulture));
            AppendField(builder, "rotation", rule.Rotation.ToString("R", CultureInfo.InvariantCulture));
            AppendField(builder, "anchorX", rule.AnchorX.ToString("R", CultureInfo.InvariantCulture));
            AppendField(builder, "anchorY", rule.AnchorY.ToString("R", CultureInfo.InvariantCulture));
            AppendField(builder, "clipToOriginalBounds", FormatBool(rule.ClipToOriginalBounds));
            AppendOptional(builder, "animationMode", rule.AnimationMode);
            AppendOptional(builder, "frameIdentity", rule.FrameIdentity);
            AppendField(builder, "includePreserved", FormatBool(rule.IncludePreserved));
            AppendOptional(builder, "createdVersion", rule.CreatedVersion);
            AppendOptional(builder, "comment", rule.Comment);
            AppendOptional(builder, "displayName", rule.DisplayName);
            foreach (KeyValuePair<string, string> pair in rule.ExtraFields)
            {
                if (!KnownFields.Contains(pair.Key))
                {
                    AppendField(builder, pair.Key, pair.Value);
                }
            }

            return builder.ToString();
        }

        private static void NormalizeRuleIdentity(VisualOverrideRule rule)
        {
            rule.SceneName = RendererIdentityBuilder.NormalizeName(rule.SceneName);
            rule.ObjectPath = RendererIdentityBuilder.NormalizePath(rule.ObjectPath);
            rule.HierarchyDiscriminator = RendererIdentityBuilder.NormalizePath(rule.HierarchyDiscriminator);
            rule.RendererType = RendererIdentityBuilder.NormalizeName(rule.RendererType);
            rule.TexturePropertyName = RendererIdentityBuilder.NormalizeName(rule.TexturePropertyName);
            rule.SpriteName = RendererIdentityBuilder.NormalizeUnityName(rule.SpriteName);
            rule.TextureName = RendererIdentityBuilder.NormalizeUnityName(rule.TextureName);
            rule.MaterialName = RendererIdentityBuilder.NormalizeUnityName(rule.MaterialName);
            rule.ProjectileFamily = RendererIdentityBuilder.NormalizePath(rule.ProjectileFamily);
        }

        private static int CompareRulesForStorage(VisualOverrideRule left, VisualOverrideRule right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            int order = left.StableOrder.CompareTo(right.StableOrder);
            return order != 0 ? order : StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id);
        }

        private static void AppendOptional(StringBuilder builder, string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) AppendField(builder, key, value);
        }

        private static void AppendField(StringBuilder builder, string key, string value)
        {
            builder.Append('\t');
            builder.Append(key);
            builder.Append('=');
            builder.Append(EscapeValue(value));
        }

        private static bool TryParseBool(string value, out bool result)
        {
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) { result = true; return true; }
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) { result = false; return true; }
            result = false;
            return false;
        }

        private static bool SetFloat(string key, string value, out float target, out string error)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out target))
            {
                return Invalid(key, value, out error);
            }

            error = "";
            return true;
        }

        private static bool Invalid(string key, string value, out string error)
        {
            error = "Invalid value '" + value + "' for field '" + key + "'.";
            return false;
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static void AddDiagnostic(VisualRuleParseResult result, int line, VisualRuleDiagnosticSeverity severity, string message)
        {
            result.Diagnostics.Add(new VisualRuleDiagnostic
            {
                LineNumber = line,
                Severity = severity,
                Message = message
            });
        }
    }
}
