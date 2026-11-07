using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private const string CapturePresetFileName = "ShaderStripMod.capture-presets.txt";

        private string capturePresetFilePath;
        private CapturePresetProfile capturePresetProfile = new CapturePresetProfile();
        private CapturePresetMode captureMode = CapturePresetMode.SelectedObjects;
        private string capturePresetNameInput = "";
        private string lastCapturePresetMessage = "";
        private string pendingDeleteCapturePreset = "";

        private bool CaptureRulesSuspended
        {
            get { return captureMode == CapturePresetMode.HeroAndAttacks || captureMode == CapturePresetMode.HeroOnly; }
        }

        private bool CaptureShowsAttacks
        {
            get { return captureMode != CapturePresetMode.HeroOnly; }
        }

        private CapturePresetMode GetActiveCapturePresetMode()
        {
            return CapturePresetPolicy.Resolve(gameplayIsolationEnabled, CaptureRulesSuspended, CaptureShowsAttacks);
        }

        private void LoadCapturePresets()
        {
            if (string.IsNullOrEmpty(capturePresetFilePath))
            {
                InitializeListFiles();
            }

            if (string.IsNullOrEmpty(capturePresetFilePath))
            {
                return;
            }

            try
            {
                if (!File.Exists(capturePresetFilePath))
                {
                    AtomicTextFile.Write(capturePresetFilePath, CapturePresetCodec.Write(capturePresetProfile));
                    return;
                }

                string text = File.ReadAllText(capturePresetFilePath, Encoding.UTF8);
                CapturePresetProfile loaded = CapturePresetCodec.Parse(text);
                if (loaded == null)
                {
                    lastCapturePresetMessage = UiText("Capture presets were not loaded: unsupported file version.");
                    Log("ShaderStripMod capture presets were not loaded from " + capturePresetFilePath + ": unsupported file version.");
                    return;
                }

                capturePresetProfile = loaded;
                Log("ShaderStripMod loaded " + capturePresetProfile.Presets.Count + " capture preset(s) from " + capturePresetFilePath + ".");
            }
            catch (System.Exception ex)
            {
                lastCapturePresetMessage = UiText("Capture presets were not loaded. Check the log for details.");
                Log("ShaderStripMod capture preset load failed: " + ex.Message);
            }
        }

        private bool SaveCapturePresets()
        {
            if (string.IsNullOrEmpty(capturePresetFilePath))
            {
                InitializeListFiles();
            }

            if (string.IsNullOrEmpty(capturePresetFilePath))
            {
                lastCapturePresetMessage = UiText("Could not save capture presets: no writable mod folder.");
                return false;
            }

            try
            {
                AtomicTextFile.Write(capturePresetFilePath, CapturePresetCodec.Write(capturePresetProfile));
                return true;
            }
            catch (System.Exception ex)
            {
                lastCapturePresetMessage = UiText("Could not save capture presets: ") + ex.Message;
                Log("ShaderStripMod capture preset save failed: " + ex.Message);
                return false;
            }
        }

        private bool TryBackupCapturePresetFile(string reason, out string backupPath)
        {
            backupPath = "";
            if (string.IsNullOrEmpty(capturePresetFilePath) || !File.Exists(capturePresetFilePath))
            {
                return true;
            }

            try
            {
                string directory = Path.GetDirectoryName(capturePresetFilePath);
                if (string.IsNullOrEmpty(directory))
                {
                    directory = ".";
                }

                string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string candidate = Path.Combine(directory, CapturePresetFileName + ".bak_" + reason + "_" + stamp);
                backupPath = GetUniqueFilePath(candidate);
                File.Copy(capturePresetFilePath, backupPath);
                return true;
            }
            catch (System.Exception ex)
            {
                backupPath = "";
                Log("ShaderStripMod capture preset backup failed: " + ex.Message);
                return false;
            }
        }

        private void SaveCurrentCapturePreset()
        {
            string name = CapturePresetPolicy.NormalizeName(capturePresetNameInput);
            if (name.Length == 0)
            {
                lastCapturePresetMessage = UiText("Enter a preset name first.");
                return;
            }

            bool replaced = capturePresetProfile.Find(name) != null;
            if (!replaced && capturePresetProfile.Presets.Count >= CapturePresetProfile.MaxPresets)
            {
                lastCapturePresetMessage = UiText("Capture preset limit reached: {limit}.").Replace("{limit}", CapturePresetProfile.MaxPresets.ToString());
                return;
            }

            CapturePreset preset = new CapturePreset();
            preset.Name = name;
            preset.IsolationEnabled = gameplayIsolationEnabled;
            preset.RuleLines.AddRange(BuildPreserveRuleLines(false));
            if (preset.RuleLines.Count > CapturePresetProfile.MaxRulesPerPreset)
            {
                lastCapturePresetMessage = UiText("This preset would contain {rules} rules; the limit is {limit}.")
                    .Replace("{rules}", preset.RuleLines.Count.ToString())
                    .Replace("{limit}", CapturePresetProfile.MaxRulesPerPreset.ToString());
                return;
            }

            string backup = "";
            if (replaced && !TryBackupCapturePresetFile("before_replace", out backup))
            {
                lastCapturePresetMessage = UiText("Preset was not replaced: the presets file backup failed. No changes were made.");
                return;
            }

            CapturePreset previous = replaced ? capturePresetProfile.Find(name).Clone() : null;
            capturePresetProfile.Upsert(preset);
            if (!SaveCapturePresets())
            {
                if (previous != null)
                {
                    capturePresetProfile.Upsert(previous);
                }
                else
                {
                    capturePresetProfile.Remove(name);
                }

                return;
            }

            capturePresetNameInput = name;
            pendingDeleteCapturePreset = "";
            lastCapturePresetMessage = replaced
                ? FormatCapturePresetText("Preset replaced: {name} ({rules} rules, isolation {isolation}). Backup: {backup}.", name, preset.RuleLines.Count, preset.IsolationEnabled, backup)
                : FormatCapturePresetText("Preset saved: {name} ({rules} rules, isolation {isolation}).", name, preset.RuleLines.Count, preset.IsolationEnabled, "");
            Log("ShaderStripMod capture preset " + (replaced ? "replaced" : "saved") + ": " + name + ", rules=" + preset.RuleLines.Count + ", isolation=" + preset.IsolationEnabled + ", backup=" + backup + ".");
        }

        private void ApplyCapturePreset(CapturePreset preset)
        {
            if (preset == null)
            {
                lastCapturePresetMessage = UiText("Capture preset was not found.");
                return;
            }

            string backup;
            if (!TryBackupPreserveFile("before_capture_preset", out backup))
            {
                lastCapturePresetMessage = UiText("Capture preset was not applied: the preserve file backup failed. No changes were made.");
                return;
            }

            List<string> previousRules = SnapshotPreserveRuleLines();
            bool previousIsolation = gameplayIsolationEnabled;
            CapturePresetMode previousMode = captureMode;
            int previousHeroLayer = heroLayer;
            int previousVisibleMask = visibleMask;

            ClearAllPreserveRuleCollections();
            for (int i = 0; i < preset.RuleLines.Count; i++)
            {
                AddPreserveEntriesFromLine(preset.RuleLines[i]);
            }

            if (!SavePreserveList())
            {
                RestorePreserveRuleLines(previousRules);
                gameplayIsolationEnabled = previousIsolation;
                captureMode = previousMode;
                heroLayer = previousHeroLayer;
                visibleMask = previousVisibleMask;
                pendingDeleteCapturePreset = "";
                lastCapturePresetMessage = UiText("Capture preset was not applied: the preserve file could not be saved. No changes were made.");
                RefreshAfterPreserveManagerChange();
                return;
            }

            if (gameplayIsolationEnabled != preset.IsolationEnabled)
            {
                SetGameplayIsolationEnabled(preset.IsolationEnabled);
            }

            captureMode = preset.IsolationEnabled ? CapturePresetMode.SelectedObjects : CapturePresetMode.FullScene;
            heroLayer = -1;
            visibleMask = 0;
            pendingDeleteCapturePreset = "";
            lastCapturePresetMessage = FormatCapturePresetText("Preset applied: {name} ({rules} rules, isolation {isolation}). Preserve rules were backed up: {backup}.", preset.Name, preset.RuleLines.Count, preset.IsolationEnabled, backup);
            Log("ShaderStripMod applied capture preset " + preset.Name + ": rules=" + preset.RuleLines.Count + ", isolation=" + preset.IsolationEnabled + ", backup=" + backup + ".");
            RefreshAfterPreserveManagerChange();
        }

        private void DeleteCapturePreset(string name)
        {
            CapturePreset preset = capturePresetProfile.Find(name);
            if (preset == null)
            {
                pendingDeleteCapturePreset = "";
                lastCapturePresetMessage = UiText("Capture preset was not found.");
                return;
            }

            string backup;
            if (!TryBackupCapturePresetFile("before_delete", out backup))
            {
                pendingDeleteCapturePreset = "";
                lastCapturePresetMessage = UiText("Preset was not deleted: the presets file backup failed. No changes were made.");
                return;
            }

            capturePresetProfile.Remove(preset.Name);
            pendingDeleteCapturePreset = "";
            if (!SaveCapturePresets())
            {
                capturePresetProfile.Upsert(preset);
                return;
            }

            lastCapturePresetMessage = FormatCapturePresetText("Preset deleted: {name}. Backup: {backup}.", preset.Name, preset.RuleLines.Count, preset.IsolationEnabled, backup);
            Log("ShaderStripMod deleted capture preset " + preset.Name + ", backup=" + backup + ".");
        }

        private void SetCapturePresetMode(CapturePresetMode mode)
        {
            CapturePresetState state = CapturePresetPolicy.Evaluate(mode);
            if (GetActiveCapturePresetMode() == state.Mode && captureMode == state.Mode)
            {
                lastCapturePresetMessage = "";
                return;
            }

            if (gameplayIsolationEnabled != state.IsolationEnabled)
            {
                SetGameplayIsolationEnabled(state.IsolationEnabled);
            }

            captureMode = state.Mode;
            heroLayer = -1;
            visibleMask = 0;
            pendingDeleteCapturePreset = "";
            lastCapturePresetMessage = "";
            RefreshAfterPreserveManagerChange();
            Log("ShaderStripMod capture mode = " + state.Mode + ", isolation=" + state.IsolationEnabled + ", rulesSuspended=" + state.SuspendPreserveRules + ", attacks=" + state.ShowAttacks + ".");
        }

        private string FormatCapturePresetText(string template, string name, int rules, bool isolation, string backup)
        {
            return UiText(template)
                .Replace("{name}", name ?? "")
                .Replace("{rules}", rules.ToString())
                .Replace("{isolation}", UiText(isolation ? "On" : "Off"))
                .Replace("{backup}", backup ?? "");
        }

        private string BuildCaptureModeStatusText()
        {
            return UiText("Capture mode: {mode}. Gameplay isolation: {isolation}.")
                .Replace("{mode}", UiText(CapturePresetPolicy.GetEnglishName(GetActiveCapturePresetMode())))
                .Replace("{isolation}", UiText(gameplayIsolationEnabled ? "On" : "Off"));
        }

        private string BuildCaptureModeHint()
        {
            switch (GetActiveCapturePresetMode())
            {
                case CapturePresetMode.HeroAndAttacks:
                    return "Hero + attacks temporarily ignores preserve rules. They stay saved in ShaderStripMod.preserve.txt.";
                case CapturePresetMode.HeroOnly:
                    return "Hero only hides attack effects and temporarily ignores preserve rules. Your rules stay saved.";
                case CapturePresetMode.FullScene:
                    return "Full scene shows the original room and disables gameplay isolation.";
                default:
                    return "Selected objects uses your preserve rules. Switch to another mode to check the room without changing them.";
            }
        }

        private void DrawCapturePresetPanel(ref float y, float width)
        {
            UiSectionLabel(ref y, width, "Capture mode");
            DrawBackgroundSettingsLabel(ref y, width, "Choose what stays visible in gameplay. Changing the mode never edits or deletes your preserve rules.");
            DrawCapturePresetModeButtons(ref y, width, GetActiveCapturePresetMode());
            DrawBackgroundSettingsLabel(ref y, width, BuildCaptureModeStatusText());
            DrawBackgroundSettingsLabel(ref y, width, BuildCaptureModeHint());

            UiSectionLabel(ref y, width, "Saved capture presets");
            DrawBackgroundSettingsLabel(ref y, width, "A preset stores the current preserve rules and the gameplay isolation flag. Saved masks, images and media are not included.");
            bool savePreset;
            if (width < 240f)
            {
                capturePresetNameInput = GUI.TextField(new Rect(0f, y, width, 26f), capturePresetNameInput, CapturePresetPolicy.MaxNameLength);
                y += 32f;
                savePreset = UiButton(new Rect(0f, y, width, 26f), "Save preset");
                y += 34f;
            }
            else
            {
                float saveWidth = Mathf.Clamp(width * 0.24f, 96f, 150f);
                float fieldWidth = Mathf.Max(1f, width - saveWidth - 6f);
                capturePresetNameInput = GUI.TextField(new Rect(0f, y, fieldWidth, 26f), capturePresetNameInput, CapturePresetPolicy.MaxNameLength);
                savePreset = UiButton(new Rect(fieldWidth + 6f, y, saveWidth, 26f), "Save preset");
                y += 34f;
            }

            if (!string.IsNullOrEmpty(lastCapturePresetMessage))
            {
                DrawBackgroundSettingsLabel(ref y, width, lastCapturePresetMessage);
            }

            if (capturePresetProfile.Presets.Count == 0)
            {
                DrawBackgroundSettingsLabel(ref y, width, "No saved presets yet. Set up preserve rules, give them a name and press Save preset.");
            }

            string applyName = null;
            string requestDeleteName = null;
            string confirmDeleteName = null;
            bool cancelDelete = false;
            for (int i = 0; i < capturePresetProfile.Presets.Count; i++)
            {
                CapturePreset preset = capturePresetProfile.Presets[i];
                if (preset == null)
                {
                    continue;
                }

                DrawBackgroundSettingsLabel(ref y, width, FormatCapturePresetText("Preset: {name} ({rules} rules, isolation {isolation}).", preset.Name, preset.RuleLines.Count, preset.IsolationEnabled, ""));
                if (string.Equals(pendingDeleteCapturePreset, preset.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    DrawBackgroundSettingsLabel(ref y, width, "Click Delete preset again to confirm. The presets file is backed up first.");
                    int choice = DrawCapturePresetButtonPair(ref y, width, "Delete preset", true, "Cancel", false);
                    if (choice == 1) confirmDeleteName = preset.Name;
                    else if (choice == 2) cancelDelete = true;
                    continue;
                }

                int action = DrawCapturePresetButtonPair(ref y, width, "Apply preset", false, "Delete preset", true);
                if (action == 1) applyName = preset.Name;
                else if (action == 2) requestDeleteName = preset.Name;
            }

            if (savePreset) SaveCurrentCapturePreset();
            if (cancelDelete) pendingDeleteCapturePreset = "";
            else if (requestDeleteName != null)
            {
                pendingDeleteCapturePreset = requestDeleteName;
                lastCapturePresetMessage = UiText("Click Delete preset again to confirm.");
            }
            else if (confirmDeleteName != null) DeleteCapturePreset(confirmDeleteName);
            if (applyName != null) ApplyCapturePreset(capturePresetProfile.Find(applyName));
        }

        private void DrawCapturePresetModeButtons(ref float y, float width, CapturePresetMode active)
        {
            int columns = Mathf.Clamp(Mathf.FloorToInt((width + 6f) / 96f), 1, 2);
            float buttonWidth = Mathf.Max(1f, (width - 6f * (columns - 1)) / columns);
            int rows = (4 + columns - 1) / columns;
            for (int i = 0; i < 4; i++)
            {
                CapturePresetMode mode = (CapturePresetMode)i;
                Rect button = new Rect((i % columns) * (buttonWidth + 6f), y + (i / columns) * 30f, buttonWidth, 28f);
                if (DrawModeButton(button, CapturePresetPolicy.GetEnglishName(mode), active == mode))
                {
                    SetCapturePresetMode(mode);
                }
            }

            y += rows * 34f;
        }

        private int DrawCapturePresetButtonPair(ref float y, float width, string first, bool firstDanger, string second, bool secondDanger)
        {
            int choice = 0;
            if (width < 200f)
            {
                if (firstDanger ? UiDangerButton(new Rect(0f, y, width, 26f), first) : UiButton(new Rect(0f, y, width, 26f), first))
                {
                    choice = 1;
                }

                y += 34f;
                if (secondDanger ? UiDangerButton(new Rect(0f, y, width, 26f), second) : UiButton(new Rect(0f, y, width, 26f), second))
                {
                    choice = 2;
                }

                y += 34f;
                return choice;
            }

            float half = (width - 6f) / 2f;
            if (firstDanger ? UiDangerButton(new Rect(0f, y, half, 26f), first) : UiButton(new Rect(0f, y, half, 26f), first))
            {
                choice = 1;
            }

            if (secondDanger ? UiDangerButton(new Rect(half + 6f, y, half, 26f), second) : UiButton(new Rect(half + 6f, y, half, 26f), second))
            {
                choice = choice == 1 ? 1 : 2;
            }

            y += 34f;
            return choice;
        }
    }
}
