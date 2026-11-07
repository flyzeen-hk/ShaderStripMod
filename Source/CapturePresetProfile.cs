using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ShaderStripMod
{
    internal enum CapturePresetMode
    {
        SelectedObjects,
        HeroAndAttacks,
        HeroOnly,
        FullScene
    }

    internal struct CapturePresetState
    {
        internal CapturePresetMode Mode;
        internal bool IsolationEnabled;
        internal bool SuspendPreserveRules;
        internal bool ShowAttacks;
    }

    internal static class CapturePresetPolicy
    {
        internal const int MaxNameLength = 40;

        internal static CapturePresetState Evaluate(CapturePresetMode mode)
        {
            CapturePresetState state = new CapturePresetState();
            state.Mode = mode;
            switch (mode)
            {
                case CapturePresetMode.FullScene:
                    state.IsolationEnabled = false;
                    state.SuspendPreserveRules = false;
                    state.ShowAttacks = true;
                    break;
                case CapturePresetMode.HeroOnly:
                    state.IsolationEnabled = true;
                    state.SuspendPreserveRules = true;
                    state.ShowAttacks = false;
                    break;
                case CapturePresetMode.HeroAndAttacks:
                    state.IsolationEnabled = true;
                    state.SuspendPreserveRules = true;
                    state.ShowAttacks = true;
                    break;
                default:
                    state.Mode = CapturePresetMode.SelectedObjects;
                    state.IsolationEnabled = true;
                    state.SuspendPreserveRules = false;
                    state.ShowAttacks = true;
                    break;
            }

            return state;
        }

        internal static CapturePresetMode Resolve(bool isolationEnabled, bool suspendPreserveRules, bool showAttacks)
        {
            if (!isolationEnabled)
            {
                return CapturePresetMode.FullScene;
            }

            if (suspendPreserveRules)
            {
                return showAttacks ? CapturePresetMode.HeroAndAttacks : CapturePresetMode.HeroOnly;
            }

            return CapturePresetMode.SelectedObjects;
        }

        internal static string GetEnglishName(CapturePresetMode mode)
        {
            switch (mode)
            {
                case CapturePresetMode.HeroAndAttacks:
                    return "Hero + attacks";
                case CapturePresetMode.HeroOnly:
                    return "Hero only";
                case CapturePresetMode.FullScene:
                    return "Full scene";
                default:
                    return "Selected objects";
            }
        }

        internal static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "";
            }

            StringBuilder builder = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char character = name[i];
                builder.Append(character < ' ' || character == '\u007f' ? ' ' : character);
            }

            string result = builder.ToString().Trim();
            if (result.Length > MaxNameLength)
            {
                result = result.Substring(0, MaxNameLength).TrimEnd();
            }

            return result;
        }

        internal static bool IsSupportedRuleLine(string line)
        {
            if (string.IsNullOrEmpty(line) || line.IndexOf('\r') >= 0 || line.IndexOf('\n') >= 0)
            {
                return false;
            }

            string trimmed = line.Trim();
            int separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                return false;
            }

            string key = trimmed.Substring(0, separator).Trim();
            string value = trimmed.Substring(separator + 1).Trim();
            return value.Length > 0 && IsSupportedRuleKey(key);
        }

        internal static bool IsSupportedRuleKey(string key)
        {
            return string.Equals(key, "rendererPath", StringComparison.Ordinal) ||
                string.Equals(key, "projectileRendererPath", StringComparison.Ordinal) ||
                string.Equals(key, "path", StringComparison.Ordinal) ||
                string.Equals(key, "groupPath", StringComparison.Ordinal) ||
                string.Equals(key, "projectilePath", StringComparison.Ordinal) ||
                string.Equals(key, "sprite", StringComparison.Ordinal) ||
                string.Equals(key, "texture", StringComparison.Ordinal) ||
                string.Equals(key, "material", StringComparison.Ordinal) ||
                string.Equals(key, "asset", StringComparison.Ordinal);
        }
    }

    internal sealed class CapturePreset
    {
        internal string Name = "";
        internal bool IsolationEnabled = true;
        internal readonly List<string> RuleLines = new List<string>();

        internal CapturePreset Clone()
        {
            CapturePreset copy = new CapturePreset();
            copy.Name = Name;
            copy.IsolationEnabled = IsolationEnabled;
            copy.RuleLines.AddRange(RuleLines);
            return copy;
        }
    }

    internal sealed class CapturePresetProfile
    {
        internal const int FormatVersion = 1;
        internal const int MaxPresets = 64;
        internal const int MaxRulesPerPreset = 4096;

        internal readonly List<CapturePreset> Presets = new List<CapturePreset>();

        internal CapturePreset Find(string name)
        {
            string normalized = CapturePresetPolicy.NormalizeName(name);
            if (normalized.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < Presets.Count; i++)
            {
                if (string.Equals(Presets[i].Name, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return Presets[i];
                }
            }

            return null;
        }

        internal bool Upsert(CapturePreset preset)
        {
            if (preset == null)
            {
                return false;
            }

            string normalized = CapturePresetPolicy.NormalizeName(preset.Name);
            if (normalized.Length == 0)
            {
                return false;
            }

            CapturePreset existing = Find(normalized);
            if (existing == null)
            {
                if (Presets.Count >= MaxPresets)
                {
                    return false;
                }

                CapturePreset added = new CapturePreset();
                CopyInto(added, preset, normalized);
                Presets.Add(added);
                return false;
            }

            CopyInto(existing, preset, normalized);
            return true;
        }

        internal bool Remove(string name)
        {
            CapturePreset existing = Find(name);
            if (existing == null)
            {
                return false;
            }

            Presets.Remove(existing);
            return true;
        }

        private static void CopyInto(CapturePreset target, CapturePreset source, string name)
        {
            target.Name = name;
            target.IsolationEnabled = source.IsolationEnabled;
            target.RuleLines.Clear();
            for (int i = 0; i < source.RuleLines.Count && target.RuleLines.Count < MaxRulesPerPreset; i++)
            {
                string line = source.RuleLines[i];
                if (CapturePresetPolicy.IsSupportedRuleLine(line))
                {
                    target.RuleLines.Add(line.Trim());
                }
            }
        }
    }

    internal static class CapturePresetCodec
    {
        internal static string Write(CapturePresetProfile profile)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# ShaderStripMod capture presets");
            builder.AppendLine("# Saved preserve-rule snapshots for the F9 Settings capture panel.");
            builder.AppendLine("# Apply loads the rules into ShaderStripMod.preserve.txt after making a backup.");
            builder.AppendLine("formatVersion=" + CapturePresetProfile.FormatVersion.ToString(CultureInfo.InvariantCulture));
            if (profile != null)
            {
                for (int i = 0; i < profile.Presets.Count; i++)
                {
                    CapturePreset preset = profile.Presets[i];
                    if (preset == null || preset.Name.Length == 0)
                    {
                        continue;
                    }

                    builder.AppendLine();
                    builder.AppendLine("preset=" + preset.Name);
                    builder.AppendLine("isolation=" + (preset.IsolationEnabled ? "On" : "Off"));
                    for (int j = 0; j < preset.RuleLines.Count; j++)
                    {
                        if (CapturePresetPolicy.IsSupportedRuleLine(preset.RuleLines[j]))
                        {
                            builder.AppendLine("rule=" + preset.RuleLines[j].Trim());
                        }
                    }
                }
            }

            return builder.ToString();
        }

        internal static CapturePresetProfile Parse(string text)
        {
            CapturePresetProfile profile = new CapturePresetProfile();
            if (string.IsNullOrEmpty(text))
            {
                return profile;
            }

            CapturePreset current = null;
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                if (string.Equals(key, "formatVersion", StringComparison.OrdinalIgnoreCase))
                {
                    int version;
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out version) ||
                        version < 1 ||
                        version > CapturePresetProfile.FormatVersion)
                    {
                        return null;
                    }

                    continue;
                }

                if (string.Equals(key, "preset", StringComparison.OrdinalIgnoreCase))
                {
                    FlushPreset(profile, current);
                    current = CreatePreset(value);
                    continue;
                }

                if (current == null)
                {
                    continue;
                }

                if (string.Equals(key, "isolation", StringComparison.OrdinalIgnoreCase))
                {
                    bool isolation;
                    if (TryParseToggle(value, out isolation))
                    {
                        current.IsolationEnabled = isolation;
                    }

                    continue;
                }

                if (string.Equals(key, "rule", StringComparison.OrdinalIgnoreCase) &&
                    current.RuleLines.Count < CapturePresetProfile.MaxRulesPerPreset &&
                    CapturePresetPolicy.IsSupportedRuleLine(value))
                {
                    current.RuleLines.Add(value);
                }
            }

            FlushPreset(profile, current);
            return profile;
        }

        private static CapturePreset CreatePreset(string name)
        {
            string normalized = CapturePresetPolicy.NormalizeName(name);
            if (normalized.Length == 0)
            {
                return null;
            }

            CapturePreset preset = new CapturePreset();
            preset.Name = normalized;
            return preset;
        }

        private static void FlushPreset(CapturePresetProfile profile, CapturePreset preset)
        {
            if (preset != null && preset.Name.Length > 0)
            {
                profile.Upsert(preset);
            }
        }

        private static bool TryParseToggle(string value, out bool result)
        {
            result = false;
            if (string.Equals(value, "On", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "True", StringComparison.OrdinalIgnoreCase) ||
                value == "1")
            {
                result = true;
                return true;
            }

            if (string.Equals(value, "Off", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "False", StringComparison.OrdinalIgnoreCase) ||
                value == "0")
            {
                return true;
            }

            return false;
        }
    }
}
