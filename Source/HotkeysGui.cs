using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private const int TextureBrowserHotkeysTab = 9;

        private void DrawHotkeysTab(Rect content)
        {
            if (backgroundSettingsLabelStyle == null) backgroundSettingsLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            UiBox(content, "");
            Rect viewport = new Rect(content.x + 10f, content.y + 10f, content.width - 20f, content.height - 20f);
            float width = Mathf.Max(100f, viewport.width - 20f);
            Rect view = new Rect(0, 0, width, Mathf.Max(viewport.height, 620f));
            textureBrowserDetailScroll = GUI.BeginScrollView(viewport, textureBrowserDetailScroll, view);
            float y = 0f;
            DrawHotkeysLabel(ref y, width, "Hotkeys");
            DrawHotkeysLabel(ref y, width, "Every mod action can use one keyboard key. Changes are saved immediately to ShaderStripMod.hotkeys.txt.");
            DrawHotkeysLabel(ref y, width, "Change waits for the next supported key. Esc cancels. Esc, Enter, Tab and arrow keys are reserved.");
            DrawHotkeysLabel(ref y, width, "The mod only detects conflicts between its own actions. Hollow Knight and other mods may already use the same key.");
            DrawHotkeysLabel(ref y, width, "Click Change, then press the new key. Changes are saved immediately.");
            UiSectionLabel(ref y, width, "Hotkey actions");

            string changeRequest = null;
            string clearRequest = null;
            string resetRequest = null;
            bool resetAll = false;
            bool cancelCapture = false;

            if (IsHotkeyCaptureActive())
            {
                Color previous = GUI.backgroundColor;
                try
                {
                    GUI.backgroundColor = new Color(1f, .85f, .35f, 1f);
                    DrawHotkeysLabel(ref y, width, UiText("Press a key for") + " \"" + UiText(HotkeyActions.GetDisplayName(hotkeyCaptureActionId)) + "\" " + UiText("or Esc to cancel."));
                }
                finally
                {
                    GUI.backgroundColor = previous;
                }

                if (UiButton(new Rect(0, y, Mathf.Min(200f, width), 28f), "Cancel capture"))
                {
                    cancelCapture = true;
                }

                y += 36f;
            }

            bool captureActive = IsHotkeyCaptureActive();
            for (int i = 0; i < HotkeyActions.All.Length; i++)
            {
                string action = HotkeyActions.All[i];
                string key = HotkeyLabel(action);
                string line = UiText(HotkeyActions.GetDisplayName(action)) + " - " + (key.Length == 0 ? UiText("Not set") : key);
                DrawHotkeysLabel(ref y, width, line);
                float buttonWidth = Mathf.Max(70f, (width - 12f) / 3f);
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && !captureActive;
                if (UiButton(new Rect(0, y, buttonWidth, 28f), "Change")) changeRequest = action;
                if (UiButton(new Rect(buttonWidth + 6f, y, buttonWidth, 28f), "Clear key")) clearRequest = action;
                if (UiButton(new Rect((buttonWidth + 6f) * 2f, y, buttonWidth, 28f), "Reset default")) resetRequest = action;
                GUI.enabled = previousEnabled;
                y += 34f;
            }

            y += 4f;
            bool previousEnabledAll = GUI.enabled;
            GUI.enabled = previousEnabledAll && !captureActive;
            if (UiButton(new Rect(0, y, Mathf.Min(220f, width), 30f), "Reset all defaults"))
            {
                resetAll = true;
            }

            GUI.enabled = previousEnabledAll;
            y += 40f;
            if (!string.IsNullOrEmpty(hotkeyProfileMessage))
            {
                DrawHotkeysLabel(ref y, width, hotkeyProfileMessage);
            }

            GUI.EndScrollView();

            if (cancelCapture) CancelHotkeyCapture();
            if (changeRequest != null) BeginHotkeyCapture(changeRequest);
            if (clearRequest != null) ClearHotkey(clearRequest);
            if (resetRequest != null) ResetHotkey(resetRequest);
            if (resetAll) ResetAllHotkeys();
        }

        private void DrawHotkeysLabel(ref float y, float width, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = UiText(text);
            float height = backgroundSettingsLabelStyle.CalcHeight(new GUIContent(text), width);
            UiLabel(new Rect(0, y, width, height), text, backgroundSettingsLabelStyle);
            y += height + 8f;
        }
    }
}
