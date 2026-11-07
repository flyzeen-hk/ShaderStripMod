using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private HotkeyProfile hotkeyProfile = HotkeyProfile.CreateDefault();
        private string hotkeyCaptureActionId = "";
        private string hotkeyProfileMessage = "";
        private int hotkeyCaptureEscapeFrame = -1;
        private KeyCode[] hotkeyCaptureKeys;

        private string GetHotkeyProfilePath()
        {
            string directory = string.IsNullOrEmpty(settingsFilePath) ? null : Path.GetDirectoryName(settingsFilePath);
            if (string.IsNullOrEmpty(directory)) directory = Application.persistentDataPath;
            return Path.Combine(directory, HotkeyProfileCodec.FileName);
        }

        private void LoadHotkeyProfile()
        {
            hotkeyProfile = HotkeyProfile.CreateDefault();
            try
            {
                string path = GetHotkeyProfilePath();
                if (File.Exists(path))
                {
                    hotkeyProfile = HotkeyProfileCodec.Parse(File.ReadAllText(path, Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                Log("ShaderStripMod hotkeys load failed: " + ex.Message);
                hotkeyProfile = HotkeyProfile.CreateDefault();
            }
        }

        private bool SaveHotkeyProfile()
        {
            try
            {
                AtomicTextFile.Write(GetHotkeyProfilePath(), HotkeyProfileCodec.Serialize(hotkeyProfile));
                preserveManagerRuntimeReportDirty = true;
                return true;
            }
            catch (Exception ex)
            {
                Log("ShaderStripMod hotkeys save failed: " + ex.Message);
                hotkeyProfileMessage = "Hotkey settings could not be saved.";
                return false;
            }
        }

        private string GetHotkeyKeyName(string actionId)
        {
            return hotkeyProfile != null ? hotkeyProfile.GetBinding(actionId) : "";
        }

        private KeyCode GetHotkeyKey(string actionId)
        {
            string name = GetHotkeyKeyName(actionId);
            KeyCode key;
            return !string.IsNullOrEmpty(name) && Enum.TryParse(name, out key) ? key : KeyCode.None;
        }

        private bool IsHotkeyDown(string actionId)
        {
            KeyCode key = GetHotkeyKey(actionId);
            return key != KeyCode.None && Input.GetKeyDown(key);
        }

        private string HotkeyLabel(string actionId)
        {
            return HotkeyKeyPolicy.GetDisplayName(GetHotkeyKeyName(actionId));
        }

        private string HotkeyLogLabel(string actionId)
        {
            string key = HotkeyLabel(actionId);
            return key.Length == 0 ? "unassigned" : key;
        }

        private string HotkeySuffix(string actionId)
        {
            string key = HotkeyLabel(actionId);
            return key.Length == 0 ? "" : " (" + key + ")";
        }

        private string HotkeyText(string template, string actionId)
        {
            string key = HotkeyLabel(actionId);
            return UiText(template)
                .Replace("{key}", key.Length == 0 ? UiText("the configured hotkey") : key)
                .Replace("{keySuffix}", key.Length == 0 ? "" : " (" + key + ")");
        }

        private bool IsHotkeyCaptureActive()
        {
            return !string.IsNullOrEmpty(hotkeyCaptureActionId);
        }

        private void BeginHotkeyCapture(string actionId)
        {
            string action = HotkeyActions.Normalize(actionId);
            if (action.Length == 0) return;
            hotkeyCaptureActionId = action;
            hotkeyProfileMessage = "";
        }

        private void CancelHotkeyCapture()
        {
            if (!IsHotkeyCaptureActive()) return;
            hotkeyCaptureActionId = "";
            hotkeyProfileMessage = "Hotkey change cancelled.";
        }

        private bool UpdateHotkeyCapture()
        {
            if (!IsHotkeyCaptureActive()) return false;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                hotkeyCaptureEscapeFrame = Time.frameCount;
                CancelHotkeyCapture();
                return true;
            }

            if (hotkeyCaptureKeys == null || hotkeyCaptureKeys.Length == 0)
            {
                List<KeyCode> keys = new List<KeyCode>();
                for (int i = 0; i < HotkeyKeyPolicy.AllowedKeyNames.Length; i++)
                {
                    KeyCode key;
                    if (Enum.TryParse(HotkeyKeyPolicy.AllowedKeyNames[i], out key)) keys.Add(key);
                }

                hotkeyCaptureKeys = keys.ToArray();
            }

            for (int i = 0; i < hotkeyCaptureKeys.Length; i++)
            {
                KeyCode key = hotkeyCaptureKeys[i];
                if (!Input.GetKeyDown(key)) continue;
                string action = hotkeyCaptureActionId;
                string error;
                if (HotkeyValidation.TryAssign(hotkeyProfile, action, key.ToString(), out error))
                {
                    hotkeyCaptureActionId = "";
                    if (SaveHotkeyProfile()) hotkeyProfileMessage = "Hotkey saved.";
                }
                else
                {
                    hotkeyProfileMessage = error;
                }

                return true;
            }

            return true;
        }

        private void ClearHotkey(string actionId)
        {
            string action = HotkeyActions.Normalize(actionId);
            if (action.Length == 0) return;
            CancelHotkeyCapture();
            hotkeyProfile.SetBinding(action, "");
            if (SaveHotkeyProfile()) hotkeyProfileMessage = "Hotkey cleared.";
        }

        private void ResetHotkey(string actionId)
        {
            string action = HotkeyActions.Normalize(actionId);
            if (action.Length == 0) return;
            CancelHotkeyCapture();
            string fallback = HotkeyActions.GetDefaultKey(action);
            string error;
            if (HotkeyValidation.TryAssign(hotkeyProfile, action, fallback, out error))
            {
                if (SaveHotkeyProfile()) hotkeyProfileMessage = "Hotkey reset to default.";
                return;
            }

            hotkeyProfile.SetBinding(action, "");
            if (SaveHotkeyProfile()) hotkeyProfileMessage = error;
        }

        private void ResetAllHotkeys()
        {
            CancelHotkeyCapture();
            hotkeyProfile.ResetAll();
            hotkeyProfile.RepairConflicts();
            if (SaveHotkeyProfile()) hotkeyProfileMessage = "All hotkeys reset to defaults.";
        }
    }
}
