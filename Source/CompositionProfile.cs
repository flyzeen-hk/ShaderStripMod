using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ShaderStripMod
{
    internal sealed class CompositionPanelSettings
    {
        internal string File = "";
        internal bool Paused;
        internal bool SoundEnabled;
        internal float Volume = MediaAudioPolicy.DefaultVolume;
        internal bool Muted;

        internal CompositionPanelSettings Clone()
        {
            return new CompositionPanelSettings
            {
                File = File,
                Paused = Paused,
                SoundEnabled = SoundEnabled,
                Volume = Volume,
                Muted = Muted
            };
        }

        internal MediaAudioSettings GetAudioSettings()
        {
            return new MediaAudioSettings
            {
                Enabled = SoundEnabled,
                Volume = Volume,
                Muted = Muted,
                MasterVolume = MediaAudioPolicy.DefaultMasterVolume
            };
        }
    }

    internal sealed class CompositionProfile
    {
        internal string Layout = CompositionPolicy.LayoutThreeColumns;
        internal string Placement = CompositionPolicy.PlacementCover;
        internal readonly CompositionPanelSettings[] Panels = new CompositionPanelSettings[CompositionPolicy.MaxMediaPanels];

        internal CompositionProfile()
        {
            for (int i = 0; i < Panels.Length; i++)
            {
                Panels[i] = new CompositionPanelSettings();
            }
        }

        internal CompositionProfile Clone()
        {
            CompositionProfile copy = new CompositionProfile { Layout = Layout, Placement = Placement };
            for (int i = 0; i < Panels.Length; i++)
            {
                copy.Panels[i] = Panels[i].Clone();
            }

            return copy;
        }

        internal static CompositionProfile CreateDefault()
        {
            return new CompositionProfile();
        }
    }

    internal static class CompositionProfileCodec
    {
        internal const int FormatVersion = 2;
        internal const int MinimumSupportedFormatVersion = 1;
        internal const int MaximumSupportedFormatVersion = 2;
        internal const string FileName = "ShaderStripMod.composition.txt";

        internal static string Serialize(CompositionProfile profile)
        {
            CompositionProfile source = profile ?? CompositionProfile.CreateDefault();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# ShaderStripMod media collage preset. Edit only while the game is closed.");
            builder.AppendLine("# Tiles are drawn behind the hero over the full-screen gameplay. Adjacent tiles have no gap.");
            builder.AppendLine("# Layout: " + string.Join(", ", CompositionPolicy.LayoutNames));
            builder.AppendLine("# Placement: Cover (crop edges), Contain (fit inside), Stretch.");
            builder.AppendLine("formatVersion=" + FormatVersion.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("layout=" + CompositionPolicy.NormalizeLayout(source.Layout));
            builder.AppendLine("placement=" + CompositionPolicy.NormalizePlacement(source.Placement));
            for (int i = 0; i < CompositionPolicy.MaxMediaPanels; i++)
            {
                CompositionPanelSettings panel = i < source.Panels.Length && source.Panels[i] != null
                    ? source.Panels[i]
                    : new CompositionPanelSettings();
                builder.AppendLine("panel." + i.ToString(CultureInfo.InvariantCulture) + ".file=" + (panel.File ?? ""));
                builder.AppendLine("panel." + i.ToString(CultureInfo.InvariantCulture) + ".paused=" + (panel.Paused ? "true" : "false"));
                builder.AppendLine("panel." + i.ToString(CultureInfo.InvariantCulture) + ".sound=" + (panel.SoundEnabled ? "true" : "false"));
                builder.AppendLine("panel." + i.ToString(CultureInfo.InvariantCulture) + ".volume=" +
                    MediaAudioPolicy.ClampVolume(panel.Volume, MediaAudioPolicy.DefaultVolume).ToString("0.###", CultureInfo.InvariantCulture));
                builder.AppendLine("panel." + i.ToString(CultureInfo.InvariantCulture) + ".muted=" + (panel.Muted ? "true" : "false"));
            }

            return builder.ToString();
        }

        internal static bool TryParse(string text, out CompositionProfile profile, out int formatVersion, out string error)
        {
            profile = CompositionProfile.CreateDefault();
            formatVersion = MinimumSupportedFormatVersion;
            error = "";
            if (string.IsNullOrEmpty(text)) return true;
            foreach (string rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int separator = line.IndexOf('=');
                if (separator <= 0) continue;
                string key = line.Substring(0, separator).Trim();
                if (!string.Equals(key, "formatVersion", StringComparison.OrdinalIgnoreCase)) continue;
                string value = line.Substring(separator + 1).Trim();
                int parsed;
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    formatVersion = -1;
                    error = "formatVersion is not a number.";
                    return false;
                }

                formatVersion = parsed;
                if (parsed < MinimumSupportedFormatVersion || parsed > MaximumSupportedFormatVersion)
                {
                    error = "formatVersion " + parsed.ToString(CultureInfo.InvariantCulture) +
                        " is not supported. Supported versions are " + MinimumSupportedFormatVersion.ToString(CultureInfo.InvariantCulture) +
                        " and " + MaximumSupportedFormatVersion.ToString(CultureInfo.InvariantCulture) + ".";
                    return false;
                }
            }

            profile = ParseCore(text);
            return true;
        }

        internal static CompositionProfile Parse(string text)
        {
            CompositionProfile profile;
            int formatVersion;
            string error;
            return TryParse(text, out profile, out formatVersion, out error) ? profile : CompositionProfile.CreateDefault();
        }

        private static CompositionProfile ParseCore(string text)
        {
            CompositionProfile profile = CompositionProfile.CreateDefault();
            if (string.IsNullOrEmpty(text)) return profile;
            foreach (string rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int separator = line.IndexOf('=');
                if (separator <= 0) continue;
                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                if (string.Equals(key, "layout", StringComparison.OrdinalIgnoreCase))
                {
                    profile.Layout = CompositionPolicy.NormalizeLayout(value);
                }
                else if (string.Equals(key, "placement", StringComparison.OrdinalIgnoreCase))
                {
                    profile.Placement = CompositionPolicy.NormalizePlacement(value);
                }
                else if (key.StartsWith("panel.", StringComparison.OrdinalIgnoreCase))
                {
                    ParsePanel(profile, key, value);
                }
            }

            return profile;
        }

        internal static bool IsSafeMediaName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name == "." || name == "..") return false;
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            if (name.IndexOfAny(new[] { '/', '\\', ':', '\r', '\n' }) >= 0) return false;
            return true;
        }

        private static void ParsePanel(CompositionProfile profile, string key, string value)
        {
            string[] parts = key.Split('.');
            int index;
            if (parts.Length != 3 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out index)) return;
            if (index < 0 || index >= CompositionPolicy.MaxMediaPanels) return;
            CompositionPanelSettings panel = profile.Panels[index];
            string field = parts[2];
            if (string.Equals(field, "file", StringComparison.OrdinalIgnoreCase))
            {
                string file = value ?? "";
                panel.File = CompositionPolicy.IsSupportedPanelExtension(Path.GetExtension(file)) && IsSafeMediaName(file) ? file : "";
            }
            else if (string.Equals(field, "paused", StringComparison.OrdinalIgnoreCase)) panel.Paused = ParseBool(value);
            else if (string.Equals(field, "sound", StringComparison.OrdinalIgnoreCase)) panel.SoundEnabled = ParseBool(value);
            else if (string.Equals(field, "muted", StringComparison.OrdinalIgnoreCase)) panel.Muted = ParseBool(value);
            else if (string.Equals(field, "volume", StringComparison.OrdinalIgnoreCase))
            {
                float volume;
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out volume))
                {
                    panel.Volume = MediaAudioPolicy.ClampVolume(volume, MediaAudioPolicy.DefaultVolume);
                }
            }
        }

        private static bool ParseBool(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
