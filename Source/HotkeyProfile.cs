using System;
using System.Collections.Generic;
using System.Text;

namespace ShaderStripMod
{
    internal static class HotkeyActions
    {
        internal const string ToggleBrowser = "toggleBrowser";
        internal const string ToggleCameraPin = "toggleCameraPin";
        internal const string DumpLoadedAssets = "dumpLoadedAssets";
        internal const string ReloadPreserveList = "reloadPreserveList";
        internal const string ToggleGameplayIsolation = "toggleGameplayIsolation";

        internal static readonly string[] All =
        {
            ToggleBrowser,
            ToggleCameraPin,
            DumpLoadedAssets,
            ReloadPreserveList,
            ToggleGameplayIsolation
        };

        private static readonly string[] DefaultKeys = { "F9", "F8", "F10", "F11", "F12" };

        private static readonly string[] DisplayNames =
        {
            "Open or close the texture browser",
            "Pin or release the camera",
            "Dump loaded assets",
            "Reload preserve rules",
            "Toggle gameplay isolation"
        };

        internal static int IndexOf(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return -1;
            for (int i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i], actionId, StringComparison.OrdinalIgnoreCase)) return i;
            }

            return -1;
        }

        internal static bool IsKnown(string actionId)
        {
            return IndexOf(actionId) >= 0;
        }

        internal static string Normalize(string actionId)
        {
            int index = IndexOf(actionId);
            return index >= 0 ? All[index] : "";
        }

        internal static string GetDefaultKey(string actionId)
        {
            int index = IndexOf(actionId);
            return index >= 0 ? DefaultKeys[index] : "";
        }

        internal static string GetDisplayName(string actionId)
        {
            int index = IndexOf(actionId);
            return index >= 0 ? DisplayNames[index] : "";
        }
    }

    internal static class HotkeyKeyPolicy
    {
        internal static readonly string[] AllowedKeyNames = BuildAllowedKeyNames();

        private static readonly string[] ReservedKeyNames =
        {
            "Escape", "Return", "KeypadEnter", "Tab", "UpArrow", "DownArrow", "LeftArrow", "RightArrow"
        };

        internal static string Normalize(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return "";
            string cleaned = keyName.Trim();
            for (int i = 0; i < AllowedKeyNames.Length; i++)
            {
                if (string.Equals(AllowedKeyNames[i], cleaned, StringComparison.OrdinalIgnoreCase)) return AllowedKeyNames[i];
            }

            return "";
        }

        internal static bool IsAssignable(string keyName)
        {
            return Normalize(keyName).Length > 0;
        }

        internal static bool IsReserved(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return false;
            string cleaned = keyName.Trim();
            for (int i = 0; i < ReservedKeyNames.Length; i++)
            {
                if (string.Equals(ReservedKeyNames[i], cleaned, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        internal static string GetDisplayName(string keyName)
        {
            string key = Normalize(keyName);
            if (key.Length == 0) return "";
            if (key.StartsWith("Alpha", StringComparison.Ordinal)) return key.Substring(5);
            if (key.StartsWith("Keypad", StringComparison.Ordinal))
            {
                string suffix = key.Substring(6);
                if (suffix == "Plus") return "Num +";
                if (suffix == "Minus") return "Num -";
                if (suffix == "Multiply") return "Num *";
                if (suffix == "Divide") return "Num /";
                if (suffix == "Period") return "Num .";
                return "Num " + suffix;
            }

            if (key == "PageUp") return "Page Up";
            if (key == "PageDown") return "Page Down";
            return key;
        }

        private static string[] BuildAllowedKeyNames()
        {
            List<string> keys = new List<string>();
            for (int i = 1; i <= 12; i++) keys.Add("F" + i);
            for (char c = 'A'; c <= 'Z'; c++) keys.Add(c.ToString());
            for (int i = 0; i <= 9; i++) keys.Add("Alpha" + i);
            for (int i = 0; i <= 9; i++) keys.Add("Keypad" + i);
            keys.Add("Insert");
            keys.Add("Home");
            keys.Add("End");
            keys.Add("PageUp");
            keys.Add("PageDown");
            keys.Add("Delete");
            keys.Add("Backspace");
            keys.Add("Space");
            keys.Add("KeypadPlus");
            keys.Add("KeypadMinus");
            keys.Add("KeypadMultiply");
            keys.Add("KeypadDivide");
            keys.Add("KeypadPeriod");
            return keys.ToArray();
        }
    }

    internal sealed class HotkeyProfile
    {
        private readonly Dictionary<string, string> bindings = new Dictionary<string, string>(StringComparer.Ordinal);

        internal HotkeyProfile()
        {
            for (int i = 0; i < HotkeyActions.All.Length; i++)
            {
                bindings[HotkeyActions.All[i]] = HotkeyActions.GetDefaultKey(HotkeyActions.All[i]);
            }
        }

        internal bool IsAction(string actionId)
        {
            return HotkeyActions.IsKnown(actionId);
        }

        internal string GetBinding(string actionId)
        {
            string action = HotkeyActions.Normalize(actionId);
            if (action.Length == 0) return "";
            string key;
            return bindings.TryGetValue(action, out key) ? key ?? "" : "";
        }

        internal void SetBinding(string actionId, string keyName)
        {
            string action = HotkeyActions.Normalize(actionId);
            if (action.Length == 0) return;
            bindings[action] = HotkeyKeyPolicy.Normalize(keyName);
        }

        internal void ResetAction(string actionId)
        {
            string action = HotkeyActions.Normalize(actionId);
            if (action.Length == 0) return;
            bindings[action] = HotkeyActions.GetDefaultKey(action);
        }

        internal void ResetAll()
        {
            for (int i = 0; i < HotkeyActions.All.Length; i++)
            {
                bindings[HotkeyActions.All[i]] = HotkeyActions.GetDefaultKey(HotkeyActions.All[i]);
            }
        }

        internal void RepairConflicts()
        {
            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < HotkeyActions.All.Length; i++)
            {
                string action = HotkeyActions.All[i];
                string key = HotkeyKeyPolicy.Normalize(bindings[action]);
                if (key.Length == 0)
                {
                    bindings[action] = "";
                    continue;
                }

                if (!used.Contains(key))
                {
                    bindings[action] = key;
                    used.Add(key);
                    continue;
                }

                string fallback = HotkeyActions.GetDefaultKey(action);
                if (fallback.Length > 0 && !used.Contains(fallback))
                {
                    bindings[action] = fallback;
                    used.Add(fallback);
                }
                else
                {
                    bindings[action] = "";
                }
            }
        }

        internal HotkeyProfile Clone()
        {
            HotkeyProfile copy = new HotkeyProfile();
            for (int i = 0; i < HotkeyActions.All.Length; i++)
            {
                string action = HotkeyActions.All[i];
                copy.bindings[action] = GetBinding(action);
            }

            return copy;
        }

        internal static HotkeyProfile CreateDefault()
        {
            return new HotkeyProfile();
        }
    }

    internal static class HotkeyValidation
    {
        internal static bool TryAssign(HotkeyProfile profile, string actionId, string keyName, out string error)
        {
            error = "";
            if (profile == null)
            {
                error = "Hotkey settings are unavailable.";
                return false;
            }

            string action = HotkeyActions.Normalize(actionId);
            if (action.Length == 0)
            {
                error = "Unknown hotkey action.";
                return false;
            }

            if (HotkeyKeyPolicy.IsReserved(keyName))
            {
                error = "Esc, Enter, Tab and arrow keys are reserved.";
                return false;
            }

            string key = HotkeyKeyPolicy.Normalize(keyName);
            if (key.Length == 0)
            {
                error = "This key is not supported. Choose a letter, number, F-key or keypad key.";
                return false;
            }

            for (int i = 0; i < HotkeyActions.All.Length; i++)
            {
                string other = HotkeyActions.All[i];
                if (other == action) continue;
                if (!string.Equals(profile.GetBinding(other), key, StringComparison.Ordinal)) continue;
                error = "This key is already assigned to another action.";
                return false;
            }

            profile.SetBinding(action, key);
            return true;
        }
    }

    internal static class HotkeyProfileCodec
    {
        internal const int FormatVersion = 1;
        internal const string FileName = "ShaderStripMod.hotkeys.txt";

        internal static string Serialize(HotkeyProfile profile)
        {
            HotkeyProfile source = (profile ?? HotkeyProfile.CreateDefault()).Clone();
            source.RepairConflicts();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# ShaderStripMod hotkeys. Edit only while the game is closed.");
            builder.AppendLine("# Use key names: F1-F12, A-Z, Alpha0-Alpha9, Keypad0-Keypad9, Insert, Home, End, PageUp, PageDown, Delete, Backspace, Space.");
            builder.AppendLine("# Esc, Enter, Tab and arrow keys are reserved and cannot be assigned.");
            builder.AppendLine("formatVersion=" + FormatVersion);
            for (int i = 0; i < HotkeyActions.All.Length; i++)
            {
                string action = HotkeyActions.All[i];
                builder.AppendLine(action + "=" + source.GetBinding(action));
            }

            return builder.ToString();
        }

        internal static HotkeyProfile Parse(string text)
        {
            HotkeyProfile profile = HotkeyProfile.CreateDefault();
            if (string.IsNullOrEmpty(text)) return profile;
            foreach (string rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int separator = line.IndexOf('=');
                if (separator <= 0) continue;
                string action = HotkeyActions.Normalize(line.Substring(0, separator).Trim());
                if (action.Length == 0) continue;
                string value = line.Substring(separator + 1).Trim();
                if (value.Length == 0)
                {
                    profile.SetBinding(action, "");
                    continue;
                }

                string key = HotkeyKeyPolicy.Normalize(value);
                if (key.Length == 0) continue;
                profile.SetBinding(action, key);
            }

            profile.RepairConflicts();
            return profile;
        }
    }
}
