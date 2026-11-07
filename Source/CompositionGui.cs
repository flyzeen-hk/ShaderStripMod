using System;
using System.IO;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private const int TextureBrowserCompositionTab = 8;
        private GUIStyle compositionLabelStyle;
        private GUIStyle compositionButtonStyle;
        private readonly System.Collections.Generic.Dictionary<string,long> compositionMediaSizeCache=new System.Collections.Generic.Dictionary<string,long>(StringComparer.OrdinalIgnoreCase);
        private float compositionContentHeight = 1750f;
        private bool confirmResetComposition;

        private void CompositionLabel(ref float y, float width, string text)
        {
            text = UiText(text);
            float height = Mathf.Max(20f, compositionLabelStyle.CalcHeight(new GUIContent(text), width));
            UiLabel(new Rect(0f, y, width, height), text, compositionLabelStyle);
            y += height + 6f;
        }

        private void DrawCompositionTab(Rect content)
        {
            if (compositionLabelStyle == null)
            {
                compositionLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            }

            if (compositionButtonStyle == null)
            {
                compositionButtonStyle = new GUIStyle(GUI.skin.button) { wordWrap = true };
            }

            if (backgroundSettingsLabelStyle == null)
            {
                backgroundSettingsLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            }

            RefreshMenuMediaFiles();
            UiBox(content, "");
            Rect view = new Rect(content.x + 8f, content.y + 8f, content.width - 16f, content.height - 16f);
            float width = Mathf.Max(1f, view.width - 20f);
            float y = 0f;
            textureBrowserDetailScroll = GUI.BeginScrollView(view, textureBrowserDetailScroll,
                new Rect(0f, 0f, width, Mathf.Max(view.height, compositionContentHeight)));
            CompositionLabel(ref y, width, "Full-screen media collage");
            CompositionLabel(ref y, width, "Tiles are fixed parts of one full-screen collage, not movable windows.");
            CompositionLabel(ref y, width, HotkeyText("PNG, JPG, GIF and video tiles are drawn behind the hero over the full-screen gameplay. Gameplay isolation{keySuffix} is required. Empty tiles keep the background color visible.", HotkeyActions.ToggleGameplayIsolation));
            UiSectionLabel(ref y, width, "How to use");
            CompositionLabel(ref y, width, "1. Choose a layout.");
            CompositionLabel(ref y, width, "2. Import image, GIF or video files.");
            CompositionLabel(ref y, width, "3. Assign files to panels.");
            CompositionLabel(ref y, width, "4. Enable the collage and gameplay isolation.");
            int mediaCount = GetCompositionMediaCountForLayout();
            int filledPanels = CountFilledCompositionPanels(mediaCount);
            UiSectionLabel(ref y, width, "Collage summary");
            CompositionLabel(ref y, width, BuildCompositionSummary(mediaCount, filledPanels));
            CompositionLabel(ref y, width, compositionMessage);
            string loadAdvisory = BuildCompositionLoadAdvisory(mediaCount);
            if (!string.IsNullOrEmpty(loadAdvisory))
            {
                CompositionLabel(ref y, width, loadAdvisory);
            }

            CompositionLabel(ref y, width, DescribeCompositionState());
            bool enabled = DrawWrappedToggle(ref y, width, compositionEnabled, "Enable media collage");
            bool isolation = DrawWrappedToggle(ref y, width, gameplayIsolationEnabled, HotkeyText("Gameplay isolation{keySuffix}", HotkeyActions.ToggleGameplayIsolation));
            CompositionLabel(ref y, width, HotkeyText("Camera pin{keySuffix} stays available. The collage pauses during scene transitions and resumes in the next room.", HotkeyActions.ToggleCameraPin));

            UiSectionLabel(ref y, width, "Layout");
            string layout = compositionProfile.Layout;
            string chosenLayout = null;
            int layoutColumns = Mathf.Clamp(Mathf.FloorToInt((width + 6f) / 76f), 1, 2);
            float layoutWidth = (width - 6f * (layoutColumns - 1)) / layoutColumns;
            int layoutRows = (CompositionPolicy.LayoutNames.Length + layoutColumns - 1) / layoutColumns;
            for (int i = 0; i < CompositionPolicy.LayoutNames.Length; i++)
            {
                string name = CompositionPolicy.LayoutNames[i];
                Rect button = new Rect((i % layoutColumns) * (layoutWidth + 6f), y + (i / layoutColumns) * 30f, layoutWidth, 26f);
                if (DrawModeButton(button, CompositionPolicy.GetLayoutDisplayName(name), layout == name))
                {
                    chosenLayout = name;
                }
            }

            y += layoutRows * 30f + 8f;
            CompositionLabel(ref y, width, "Placement");
            string chosenPlacement = null;
            int placementColumns = Mathf.Clamp(Mathf.FloorToInt((width + 6f) / 76f), 1, 3);
            float placementWidth = (width - 6f * (placementColumns - 1)) / placementColumns;
            int placementRows = (CompositionPolicy.PlacementNames.Length + placementColumns - 1) / placementColumns;
            for (int i = 0; i < CompositionPolicy.PlacementNames.Length; i++)
            {
                string name = CompositionPolicy.PlacementNames[i];
                Rect button = new Rect((i % placementColumns) * (placementWidth + 6f), y + (i / placementColumns) * 30f, placementWidth, 26f);
                if (DrawModeButton(button, name, compositionProfile.Placement == name))
                {
                    chosenPlacement = name;
                }
            }

            y += placementRows * 30f + 4f;
            CompositionLabel(ref y, width, "Media fills each tile like the menu background: the aspect ratio is preserved and Cover crops overflow.");
            CompositionLabel(ref y, width, "Media tiles: " + mediaCount);
            CompositionLabel(ref y, width, "Adjacent tiles touch with no gap. The hero and preserved objects always render above the collage.");

            if (mediaCount == 0)
            {
                CompositionLabel(ref y, width, "The window is too small for this layout. Resize the window or choose another layout.");
            }

            UiSectionLabel(ref y, width, "Panels");
            int importPanel = -1;
            int clearPanel = -1;
            for (int i = 0; i < mediaCount; i++)
            {
                CompositionPanelSettings panel = compositionProfile.Panels[i];
                CompositionLabel(ref y, width, "Panel " + (i + 1) + " - " + UiText(GetCompositionPanelLabel(i)));
                if (string.IsNullOrEmpty(panel.File))
                {
                    CompositionLabel(ref y, width, "Empty panel: import a file or assign an existing one below.");
                }
                else
                {
                    CompositionLabel(ref y, width, "File: " + panel.File);
                }

                if (panel.SoundEnabled && compositionPanelSurfaces[i] != null && compositionPanelSurfaces[i].VideoHasNoAudioTrack)
                {
                    CompositionLabel(ref y, width, "This video has no audio track.");
                }

                if (width < 170f)
                {
                    if (UiButton(new Rect(0f, y, width, 30f), "Import media", compositionButtonStyle))
                    {
                        importPanel = i;
                    }

                    y += 36f;
                    if (UiDangerButton(new Rect(0f, y, width, 30f), "Clear this panel", compositionButtonStyle))
                    {
                        clearPanel = i;
                    }

                    y += 36f;
                }
                else
                {
                    float buttonHalf = (width - 6f) / 2f;
                    if (UiButton(new Rect(0f, y, buttonHalf, 30f), "Import media", compositionButtonStyle))
                    {
                        importPanel = i;
                    }

                    if (UiDangerButton(new Rect(buttonHalf + 6f, y, buttonHalf, 30f), "Clear this panel", compositionButtonStyle))
                    {
                        clearPanel = i;
                    }

                    y += 36f;
                }
                bool paused = DrawWrappedToggle(ref y, width, panel.Paused, "Pause this panel");
                bool sound = DrawWrappedToggle(ref y, width, panel.SoundEnabled, "Panel sound");
                bool muted = DrawWrappedToggle(ref y, width, panel.Muted, "Mute this panel");
                CompositionLabel(ref y, width, "Panel volume");
                float volume = GUI.HorizontalSlider(new Rect(0f, y, Mathf.Max(60f, width - 70f), 24f), panel.Volume, 0f, 1f);
                UiLabel(new Rect(width - 64f, y, 64f, 24f), Mathf.RoundToInt(volume * 100f) + "%");
                y += 34f;
                if (paused != panel.Paused)
                {
                    SetCompositionPanelPaused(i, paused);
                }

                if (sound != panel.SoundEnabled)
                {
                    SetCompositionPanelSoundEnabled(i, sound);
                }

                if (muted != panel.Muted)
                {
                    SetCompositionPanelMuted(i, muted);
                }

                if (Mathf.Abs(volume - panel.Volume) > 0.005f)
                {
                    SetCompositionPanelVolume(i, volume);
                }
            }

            int assignPanel = -1;
            string assignFile = null;
            string removeFile = null;
            if (mediaCount > 0)
            {
                UiSectionLabel(ref y, width, "Imported media");
                CompositionLabel(ref y, width, "Panel assignment uses the buttons next to each file.");
                int available = 0;
                foreach (string name in menuMediaFiles)
                {
                    if (!CompositionPolicy.IsSupportedPanelExtension(Path.GetExtension(name)))
                    {
                        continue;
                    }

                    available++;
                    CompositionLabel(ref y, width, name);
                    DrawCompositionAssignmentButtons(ref y, width, mediaCount, name, ref assignPanel, ref assignFile, ref removeFile);
                }

                if (available == 0)
                {
                    CompositionLabel(ref y, width, "No media yet. Import a PNG, JPG, GIF or video above; files are stored in menu-media.");
                }

                CompositionLabel(ref y, width, "Removing a file keeps a recoverable copy in menu-media/removed.");
            }

            UiSectionLabel(ref y, width, "Preset");
            CompositionLabel(ref y, width, "Preset file: " + Path.GetFileName(GetCompositionProfilePath()));
            if (!string.IsNullOrEmpty(compositionProfileSaveStatus))
            {
                CompositionLabel(ref y, width, compositionProfileSaveStatus);
            }

            CompositionLabel(ref y, width, compositionProfileMessage);
            CompositionLabel(ref y, width, "Preset changes save automatically. Use Save preset now to write immediately.");
            float presetHalf = (width - 6f) / 2f;
            bool savePreset = UiButton(new Rect(0f, y, presetHalf, 30f), "Save preset now", compositionButtonStyle);
            bool reloadPreset = UiButton(new Rect(presetHalf + 6f, y, presetHalf, 30f), "Reload preset", compositionButtonStyle);
            y += 36f;
            bool reset = false;
            if (!confirmResetComposition)
            {
                if (UiDangerButton(new Rect(0f, y, width, 30f), "Reset composition", compositionButtonStyle))
                {
                    confirmResetComposition = true;
                    compositionMessage = "Click Reset composition again to restore defaults. Files stay in menu-media.";
                }

                y += 38f;
            }
            else
            {
                CompositionLabel(ref y, width, "Reset layout and panel assignments? Files stay in menu-media.");
                float resetHalf = (width - 6f) / 2f;
                if (UiDangerButton(new Rect(0f, y, resetHalf, 30f), "Reset composition", compositionButtonStyle))
                {
                    reset = true;
                    confirmResetComposition = false;
                }

                if (UiButton(new Rect(resetHalf + 6f, y, resetHalf, 30f), "Cancel", compositionButtonStyle))
                {
                    confirmResetComposition = false;
                }

                y += 38f;
            }

            GUI.EndScrollView();
            compositionContentHeight = y + 16f;

            if (isolation != gameplayIsolationEnabled)
            {
                SetGameplayIsolationEnabled(isolation);
            }

            if (enabled != compositionEnabled)
            {
                SetCompositionEnabled(enabled);
            }

            if (chosenLayout != null && chosenLayout != compositionProfile.Layout)
            {
                SetCompositionLayout(chosenLayout);
            }

            if (chosenPlacement != null && chosenPlacement != compositionProfile.Placement)
            {
                SetCompositionPlacement(chosenPlacement);
            }

            if (importPanel >= 0)
            {
                ChooseCompositionMedia(importPanel);
            }

            if (clearPanel >= 0)
            {
                ClearCompositionPanel(clearPanel);
            }

            if (assignFile != null)
            {
                SetCompositionPanelFile(assignPanel, assignFile);
            }

            if (removeFile != null)
            {
                RemoveCustomMenuMedia(removeFile);
                UpdateComposition();
            }

            if (savePreset)
            {
                SaveCompositionProfile();
            }

            if (reloadPreset)
            {
                ReloadCompositionProfile();
            }

            if (reset)
            {
                ResetComposition();
            }
        }

        private void DrawCompositionAssignmentButtons(ref float y, float width, int mediaCount, string name, ref int assignPanel, ref string assignFile, ref string removeFile)
        {
            int buttonCount = mediaCount + 1;
            int columns = Mathf.Clamp(Mathf.FloorToInt((width + 6f) / 70f), 1, buttonCount);
            float buttonWidth = Mathf.Max(1f, (width - 6f * (columns - 1)) / columns);
            int rows = (buttonCount + columns - 1) / columns;
            float startY = y;
            for (int p = 0; p < mediaCount; p++)
            {
                bool assigned = string.Equals(compositionProfile.Panels[p].File, name, StringComparison.OrdinalIgnoreCase);
                Rect button = new Rect((p % columns) * (buttonWidth + 6f), startY + (p / columns) * 34f, buttonWidth, 26f);
                if (DrawModeButton(button, "Panel " + (p + 1), assigned))
                {
                    assignPanel = p;
                    assignFile = name;
                }
            }

            Rect removeButton = new Rect((mediaCount % columns) * (buttonWidth + 6f), startY + (mediaCount / columns) * 34f, buttonWidth, 26f);
            if (UiDangerButton(removeButton, "Remove", compositionButtonStyle))
            {
                removeFile = name;
            }

            y = startY + rows * 34f;
        }

        private string BuildCompositionLoadAdvisory(int mediaCount)
        {
            int animated = 0;
            bool largeVideo = false;
            for (int i = 0; i < mediaCount && i < compositionProfile.Panels.Length; i++)
            {
                string file = compositionProfile.Panels[i].File;
                if (string.IsNullOrEmpty(file))
                {
                    continue;
                }

                string extension = Path.GetExtension(file);
                if (MenuMediaPolicy.IsVideo(extension))
                {
                    animated++;
                    long length;
                    if (!compositionMediaSizeCache.TryGetValue(file, out length))
                    {
                        try
                        {
                            length = new FileInfo(MenuMediaPolicy.Resolve(menuMediaDirectory, file)).Length;
                        }
                        catch (Exception)
                        {
                            length = -1;
                        }

                        compositionMediaSizeCache[file] = length;
                    }

                    if (length > 256L * 1024L * 1024L)
                    {
                        largeVideo = true;
                    }
                }
                else if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
                {
                    animated++;
                }
            }

            if (animated >= 3)
            {
                return "Several animated tiles are playing at once. Pause or remove one if the frame rate drops.";
            }

            if (largeVideo)
            {
                return "A selected video is larger than 256 MiB. Preparing it may take a while.";
            }

            if (animated >= 2)
            {
                return "More than one animated tile is playing. If the frame rate drops, pause one of them.";
            }

            return "";
        }

        private int CountFilledCompositionPanels(int mediaCount)
        {
            int filled = 0;
            for (int i = 0; i < mediaCount && i < compositionProfile.Panels.Length; i++)
            {
                if (!string.IsNullOrEmpty(compositionProfile.Panels[i].File))
                {
                    filled++;
                }
            }

            return filled;
        }

        private string BuildCompositionSummary(int mediaCount, int filledPanels)
        {
            return UiText("Layout: ") + UiText(CompositionPolicy.GetLayoutDisplayName(compositionProfile.Layout)) +
                "  |  " + UiText("Placement: ") + UiText(compositionProfile.Placement) +
                "  |  " + UiText("Filled: ") + filledPanels + "/" + mediaCount +
                "  |  " + UiText(gameplayIsolationEnabled ? "Isolation on" : "Isolation off");
        }

        private void ChooseCompositionMedia(int index)
        {
            try
            {
                string name = ImportMenuMedia(false, true);
                if (name == null)
                {
                    return;
                }

                if (!compositionEnabled)
                {
                    SetCompositionEnabled(true);
                }

                SetCompositionPanelFile(index, name);
                compositionMessage = "Panel " + (index + 1) + " selected: " + name;
                SaveExportSettings();
                UpdateComposition();
            }
            catch (Exception ex)
            {
                compositionMessage = "Composition media error: " + ex.Message;
            }
        }
    }
}
