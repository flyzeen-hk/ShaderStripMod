using System;
using System.Collections.Generic;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private static void CheckDefaultHotkeys()
        {
            HotkeyProfile profile = HotkeyProfile.CreateDefault();
            Check(profile.GetBinding(HotkeyActions.ToggleBrowser) == "F9", "Browser hotkey defaults to F9");
            Check(profile.GetBinding(HotkeyActions.ToggleCameraPin) == "F8", "Camera pin hotkey defaults to F8");
            Check(profile.GetBinding(HotkeyActions.DumpLoadedAssets) == "F10", "Dump hotkey defaults to F10");
            Check(profile.GetBinding(HotkeyActions.ReloadPreserveList) == "F11", "Reload hotkey defaults to F11");
            Check(profile.GetBinding(HotkeyActions.ToggleGameplayIsolation) == "F12", "Isolation hotkey defaults to F12");
            Check(profile.GetBinding("unknownAction") == "", "Unknown hotkey action has no binding");
            Check(profile.IsAction("TOGGLEBROWSER"), "Action lookup is case-insensitive");
            Check(!profile.IsAction("unknownAction"), "Unknown action is rejected by lookup");
            Check(HotkeyActions.Normalize("togglegameplayisolation") == HotkeyActions.ToggleGameplayIsolation, "Action names normalize to their canonical id");
            Check(HotkeyActions.GetDisplayName(HotkeyActions.ToggleBrowser).Length > 0, "Every action has a display name");

            HashSet<string> defaults = new HashSet<string>(StringComparer.Ordinal);
            foreach (string action in HotkeyActions.All)
            {
                string key = profile.GetBinding(action);
                Check(HotkeyKeyPolicy.IsAssignable(key), "Default key is assignable for " + action);
                Check(!HotkeyKeyPolicy.IsReserved(key), "Default key is not reserved for " + action);
                Check(defaults.Add(key), "Default keys are unique for " + action);
                Check(HotkeyActions.GetDefaultKey(action) == key, "Default key helper agrees for " + action);
            }
        }

        private static void CheckHotkeyKeyPolicy()
        {
            Check(HotkeyKeyPolicy.Normalize("f9") == "F9", "Key names normalize to canonical case");
            Check(HotkeyKeyPolicy.Normalize(" alpha1 ") == "Alpha1", "Key names trim surrounding spaces");
            Check(HotkeyKeyPolicy.Normalize("F13") == "", "Out-of-range F-key is rejected");
            Check(HotkeyKeyPolicy.Normalize("Mouse0") == "", "Unsupported key is rejected");
            Check(HotkeyKeyPolicy.Normalize("") == "", "Empty key normalizes to empty");
            Check(HotkeyKeyPolicy.IsAssignable("Space"), "Space is assignable");
            Check(!HotkeyKeyPolicy.IsAssignable("Bogus"), "Unknown key is not assignable");
            Check(HotkeyKeyPolicy.IsReserved("escape"), "Escape is reserved regardless of case");
            Check(HotkeyKeyPolicy.IsReserved("Return"), "Return is reserved");
            Check(HotkeyKeyPolicy.IsReserved("KeypadEnter"), "Keypad Enter is reserved");
            Check(HotkeyKeyPolicy.IsReserved("Tab"), "Tab is reserved");
            Check(HotkeyKeyPolicy.IsReserved("LeftArrow"), "Arrow keys are reserved");
            Check(!HotkeyKeyPolicy.IsReserved("F9"), "Function keys are not reserved");
            Check(HotkeyKeyPolicy.GetDisplayName("F9") == "F9", "Function key display name is unchanged");
            Check(HotkeyKeyPolicy.GetDisplayName("Alpha1") == "1", "Top-row number display name is short");
            Check(HotkeyKeyPolicy.GetDisplayName("Keypad1") == "Num 1", "Keypad number display name is readable");
            Check(HotkeyKeyPolicy.GetDisplayName("KeypadPlus") == "Num +", "Keypad plus display name is readable");
            Check(HotkeyKeyPolicy.GetDisplayName("PageUp") == "Page Up", "Page Up display name is readable");
            Check(HotkeyKeyPolicy.GetDisplayName("A") == "A", "Letter display name is unchanged");
            Check(HotkeyKeyPolicy.GetDisplayName("Bogus") == "", "Unsupported key has no display name");

            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in HotkeyKeyPolicy.AllowedKeyNames)
            {
                Check(keys.Add(key), "Allowed key list has no duplicates: " + key);
                Check(HotkeyKeyPolicy.Normalize(key) == key, "Allowed key round-trips: " + key);
                Check(HotkeyKeyPolicy.GetDisplayName(key).Length > 0, "Allowed key has a display name: " + key);
                Check(!HotkeyKeyPolicy.IsReserved(key), "Allowed key is not reserved: " + key);
            }
        }

        private static void CheckHotkeyCodec()
        {
            string serialized = HotkeyProfileCodec.Serialize(null);
            Check(serialized.Contains("formatVersion=" + HotkeyProfileCodec.FormatVersion), "Serialized hotkeys record the format version");
            Check(serialized.Contains("toggleBrowser=F9"), "Serialized hotkeys include the browser binding");
            Check(serialized.Contains("toggleGameplayIsolation=F12"), "Serialized hotkeys include the isolation binding");
            Check(serialized.IndexOf("toggleBrowser=", StringComparison.Ordinal) <
                  serialized.IndexOf("toggleCameraPin=", StringComparison.Ordinal), "Serialized hotkeys keep action order");

            HotkeyProfile parsedDefaults = HotkeyProfileCodec.Parse(serialized);
            foreach (string action in HotkeyActions.All)
            {
                Check(parsedDefaults.GetBinding(action) == HotkeyActions.GetDefaultKey(action), "Serialized defaults round-trip for " + action);
            }

            HotkeyProfile empty = HotkeyProfileCodec.Parse(null);
            Check(empty.GetBinding(HotkeyActions.ToggleBrowser) == "F9", "Null hotkey text falls back to defaults");
            empty = HotkeyProfileCodec.Parse("");
            Check(empty.GetBinding(HotkeyActions.ToggleBrowser) == "F9", "Empty hotkey text falls back to defaults");

            HotkeyProfile parsed = HotkeyProfileCodec.Parse(
                "# comment\r\nformatVersion=1\r\n\r\ntoggleBrowser= f7 \r\ntoggleCameraPin=\r\n" +
                "dumpLoadedAssets=bogus\r\nreloadPreserveList=Z\r\nunknownAction=F6\r\n");
            Check(parsed.GetBinding(HotkeyActions.ToggleBrowser) == "F7", "Parsed hotkeys normalize key case");
            Check(parsed.GetBinding(HotkeyActions.ToggleCameraPin) == "", "Parsed empty hotkey clears the binding");
            Check(parsed.GetBinding(HotkeyActions.DumpLoadedAssets) == "F10", "Parsed invalid hotkey keeps the default");
            Check(parsed.GetBinding(HotkeyActions.ReloadPreserveList) == "Z", "Parsed letters are accepted");
            Check(parsed.GetBinding(HotkeyActions.ToggleGameplayIsolation) == "F12", "Unlisted actions keep defaults");

            HotkeyProfile duplicates = HotkeyProfileCodec.Parse("toggleBrowser=F1\ntoggleCameraPin=F1\n");
            Check(duplicates.GetBinding(HotkeyActions.ToggleBrowser) == "F1", "First duplicate hotkey keeps its key");
            Check(duplicates.GetBinding(HotkeyActions.ToggleCameraPin) == "F8", "Second duplicate falls back to its free default");

            HotkeyProfile exhausted = HotkeyProfileCodec.Parse("toggleBrowser=F8\ntoggleCameraPin=F8\n");
            Check(exhausted.GetBinding(HotkeyActions.ToggleBrowser) == "F8", "First exhausted duplicate keeps its key");
            Check(exhausted.GetBinding(HotkeyActions.ToggleCameraPin) == "", "Duplicate is cleared when the default is already used");

            HotkeyProfile custom = HotkeyProfile.CreateDefault();
            custom.SetBinding(HotkeyActions.ToggleBrowser, "A");
            custom.SetBinding(HotkeyActions.ToggleCameraPin, "");
            custom.SetBinding(HotkeyActions.DumpLoadedAssets, "KeypadPlus");
            HotkeyProfile customRoundTrip = HotkeyProfileCodec.Parse(HotkeyProfileCodec.Serialize(custom));
            Check(customRoundTrip.GetBinding(HotkeyActions.ToggleBrowser) == "A", "Custom letter binding round-trips");
            Check(customRoundTrip.GetBinding(HotkeyActions.ToggleCameraPin) == "", "Cleared binding round-trips");
            Check(customRoundTrip.GetBinding(HotkeyActions.DumpLoadedAssets) == "KeypadPlus", "Keypad binding round-trips");

            HotkeyProfile dirty = HotkeyProfile.CreateDefault();
            dirty.SetBinding(HotkeyActions.ToggleBrowser, "F1");
            dirty.SetBinding(HotkeyActions.ToggleCameraPin, "F1");
            HotkeyProfile repaired = HotkeyProfileCodec.Parse(HotkeyProfileCodec.Serialize(dirty));
            Check(repaired.GetBinding(HotkeyActions.ToggleBrowser) == "F1", "Serialization repairs the first duplicate");
            Check(repaired.GetBinding(HotkeyActions.ToggleCameraPin) == "F8", "Serialization repairs the second duplicate");
        }

        private static void CheckHotkeyValidation()
        {
            HotkeyProfile profile = HotkeyProfile.CreateDefault();
            string error;
            Check(HotkeyValidation.TryAssign(profile, HotkeyActions.ToggleBrowser, "f1", out error), "Valid hotkey assignment succeeds");
            Check(error == "", "Successful assignment reports no error");
            Check(profile.GetBinding(HotkeyActions.ToggleBrowser) == "F1", "Valid assignment normalizes the key");

            Check(!HotkeyValidation.TryAssign(profile, HotkeyActions.ToggleBrowser, "Escape", out error), "Reserved key is rejected");
            Check(error.Length > 0, "Reserved key reports an error");
            Check(!HotkeyValidation.TryAssign(profile, HotkeyActions.ToggleBrowser, "F13", out error), "Unsupported key is rejected");
            Check(error.Length > 0, "Unsupported key reports an error");
            Check(!HotkeyValidation.TryAssign(profile, "unknownAction", "F2", out error), "Unknown action is rejected");
            Check(error.Length > 0, "Unknown action reports an error");
            Check(!HotkeyValidation.TryAssign(profile, HotkeyActions.ToggleCameraPin, "F1", out error), "Duplicate key is rejected");
            Check(error.Length > 0, "Duplicate key reports an error");
            Check(profile.GetBinding(HotkeyActions.ToggleCameraPin) == "F8", "Rejected assignment leaves the profile unchanged");
            Check(HotkeyValidation.TryAssign(profile, HotkeyActions.ToggleBrowser, "F1", out error), "Re-assigning the current key is allowed");
            Check(!HotkeyValidation.TryAssign(null, HotkeyActions.ToggleBrowser, "F2", out error), "Null profile is rejected");
            Check(error.Length > 0, "Null profile reports an error");

            profile.ResetAction(HotkeyActions.ToggleBrowser);
            Check(profile.GetBinding(HotkeyActions.ToggleBrowser) == "F9", "Reset action restores its default");
            profile.SetBinding(HotkeyActions.ToggleCameraPin, "A");
            profile.ResetAll();
            foreach (string action in HotkeyActions.All)
            {
                Check(profile.GetBinding(action) == HotkeyActions.GetDefaultKey(action), "Reset all restores defaults for " + action);
            }

            profile.SetBinding(HotkeyActions.ToggleCameraPin, "");
            Check(profile.GetBinding(HotkeyActions.ToggleCameraPin) == "", "Bindings can be cleared");

            HotkeyProfile original = HotkeyProfile.CreateDefault();
            HotkeyProfile clone = original.Clone();
            clone.SetBinding(HotkeyActions.ToggleBrowser, "A");
            Check(original.GetBinding(HotkeyActions.ToggleBrowser) == "F9", "Clone changes do not affect the original profile");
            Check(clone.GetBinding(HotkeyActions.ToggleBrowser) == "A", "Clone keeps its own binding");
        }

        private static void CheckHotkeyLocalization()
        {
            Check(UiLocalization.Text("Hotkeys", "Russian") == "Горячие клавиши", "Hotkeys tab title is localized");
            Check(UiLocalization.Text("Горячие клавиши", "English") == "Hotkeys", "Hotkeys title round-trips to English");
            Check(UiLocalization.Text("Clear key", "Russian") == "Очистить клавишу", "Clear key label is localized");
            Check(UiLocalization.Text("Reset all defaults", "Russian") == "Сбросить все по умолчанию", "Reset all label is localized");
            Check(UiLocalization.Text("Hotkey saved.", "Russian") == "Клавиша сохранена.", "Saved status is localized");
            Check(UiLocalization.Text("This key is already assigned to another action.", "Russian").Length > 0, "Duplicate key error is translated");
            Check(UiLocalization.Text("Эта клавиша уже назначена другому действию.", "English") == "This key is already assigned to another action.", "Duplicate key error round-trips to English");
        }

        private static void RunHotkeyChecks()
        {
            CheckDefaultHotkeys();
            CheckHotkeyKeyPolicy();
            CheckHotkeyCodec();
            CheckHotkeyValidation();
            CheckHotkeyLocalization();
        }
    }
}
