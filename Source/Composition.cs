using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderStripMod
{
    // 1.30.4 media collage: the gameplay camera clears to the background color, a
    // BeforeForwardOpaque command buffer draws the zero-gap media tiles, and the hero
    // plus preserved renderers draw above them on the normal full-screen view.
    public sealed partial class ShaderStripMod
    {
        private bool compositionEnabled;
        private CompositionProfile compositionProfile = CompositionProfile.CreateDefault();
        private string compositionMessage = "Choose a layout, then import PNG, JPG, GIF or video files for its media tiles.";
        private string compositionProfileMessage = "";
        private Camera compositionBufferCamera;
        private CommandBuffer compositionBuffer;
        private readonly Mesh[] compositionPanelMeshes = new Mesh[CompositionPolicy.MaxMediaPanels];
        private readonly Material[] compositionPanelMaterials = new Material[CompositionPolicy.MaxMediaPanels];
        private readonly MediaSurface[] compositionPanelSurfaces = new MediaSurface[CompositionPolicy.MaxMediaPanels];
        private readonly string[] compositionPanelLoaded = new string[CompositionPolicy.MaxMediaPanels];
        private readonly bool[] compositionPanelDrawable = new bool[CompositionPolicy.MaxMediaPanels];
        private readonly float[] compositionPanelTextureAspect = new float[CompositionPolicy.MaxMediaPanels];
        private readonly float[] compositionPanelAspect = new float[CompositionPolicy.MaxMediaPanels];
        private readonly string[] compositionPanelPlacement = new string[CompositionPolicy.MaxMediaPanels];
        private bool compositionActive;
        private CompositionInactiveReason compositionInactiveReason = CompositionInactiveReason.Disabled;
        private CompositionLayout compositionCurrentLayout;
        private int compositionCurrentMediaCount;
        private bool compositionBufferDirty = true;
        private bool compositionProfileDirty;
        private float compositionProfileSaveTime;
        private int compositionProfileReadOnlyVersion;
        private string compositionProfileSaveStatus = "";

        private void SetCompositionEnabled(bool enabled)
        {
            if (compositionEnabled == enabled)
            {
                return;
            }

            compositionEnabled = enabled;
            if (enabled)
            {
                compositionMessage = "The collage starts in a room when gameplay isolation and the hero are available.";
            }

            if (!initialized)
            {
                SaveExportSettings();
                return;
            }

            if (!enabled)
            {
                ReleaseComposition();
            }

            SaveExportSettings();
            UpdateComposition();
            if (!enabled && gameplayIsolationEnabled)
            {
                ApplyStrip();
            }
        }

        private void UpdateComposition()
        {
            if (!initialized)
            {
                return;
            }

            if (compositionProfileDirty && Time.unscaledTime >= compositionProfileSaveTime)
            {
                SaveCompositionProfile();
            }

            if (!compositionEnabled)
            {
                compositionInactiveReason = CompositionInactiveReason.Disabled;
                ReleaseComposition();
                return;
            }

            try
            {
                Camera world = GetGameplayWorldCamera(Camera.allCameras);
                HeroController hero = HeroController.SilentInstance;
                GameManager manager = GameManager.instance;
                bool heroAvailable = hero != null && hero.cState != null && !hero.cState.dead && !hero.cState.hazardDeath;
                bool menuScene = IsMenuScene(GetActiveSceneName());
                bool gameplayScene = manager != null && manager.IsGameplayScene();
                bool transition = manager != null &&
                    (manager.gameState == GlobalEnums.GameState.EXITING_LEVEL || manager.gameState == GlobalEnums.GameState.CUTSCENE);
                CompositionInactiveReason reason = CompositionPolicy.GetInactiveReason(true, gameplayIsolationEnabled,
                    menuScene, heroAvailable, gameplayScene, transition, world != null);

                CompositionLayout layout = default(CompositionLayout);
                if (reason == CompositionInactiveReason.None)
                {
                    layout = CompositionPolicy.CalculateLayout(compositionProfile.Layout,
                        Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
                    if (!layout.IsValid)
                    {
                        reason = CompositionInactiveReason.LayoutUnavailable;
                    }
                }

                if (reason != CompositionInactiveReason.None)
                {
                    compositionInactiveReason = reason;
                    ReleaseComposition();
                    return;
                }

                compositionCurrentLayout = layout;
                compositionCurrentMediaCount = layout.PanelCount;
                if (!EnsureCompositionBuffer(world) || !EnsureCompositionMaterials())
                {
                    compositionInactiveReason = CompositionInactiveReason.SurfaceUnavailable;
                    ReleaseComposition();
                    return;
                }

                EnsureCompositionMedia();
                UpdateCompositionMedia();
                RefreshCompositionPanelMeshes();
                compositionActive = true;
                compositionInactiveReason = CompositionInactiveReason.None;
                if (compositionBufferDirty || compositionBuffer == null)
                {
                    RebuildCompositionBuffer();
                }

                if (compositionBuffer == null)
                {
                    compositionInactiveReason = CompositionInactiveReason.SurfaceUnavailable;
                    ReleaseComposition();
                }
            }
            catch (Exception ex)
            {
                Log("Composition stopped: " + ex.Message);
                compositionMessage = "Composition stopped after a rendering error: " + ex.Message;
                compositionInactiveReason = CompositionInactiveReason.RuntimeError;
                ReleaseComposition();
                compositionEnabled = false;
                SaveExportSettings();
            }
        }

        private bool EnsureCompositionBuffer(Camera camera)
        {
            if (camera == null)
            {
                return false;
            }

            if (compositionBuffer == null)
            {
                compositionBuffer = new CommandBuffer { name = "ShaderStripMod Media Collage" };
                compositionBufferDirty = true;
            }

            if (compositionBufferCamera != camera)
            {
                if (compositionBufferCamera != null)
                {
                    try { compositionBufferCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, compositionBuffer); }
                    catch (Exception) { }
                }

                camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, compositionBuffer);
                compositionBufferCamera = camera;
                compositionBufferDirty = true;
            }

            return true;
        }

        private bool EnsureCompositionMaterials()
        {
            bool complete = true;
            for (int i = 0; i < compositionPanelMaterials.Length; i++)
            {
                if (compositionPanelMaterials[i] == null)
                {
                    complete = false;
                    break;
                }
            }

            if (complete)
            {
                return true;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                compositionMessage = "Sprites/Default shader is unavailable.";
                return false;
            }

            for (int i = 0; i < compositionPanelMaterials.Length; i++)
            {
                if (compositionPanelMaterials[i] == null)
                {
                    compositionPanelMaterials[i] = CreateCompositionMaterial(shader, "Media " + (i + 1));
                }
            }

            compositionBufferDirty = true;
            return true;
        }

        private static Material CreateCompositionMaterial(Shader shader, string side)
        {
            return new Material(shader) { name = "ShaderStripMod Composition " + side, hideFlags = HideFlags.HideAndDontSave };
        }

        private void EnsureCompositionMedia()
        {
            for (int i = 0; i < CompositionPolicy.MaxMediaPanels; i++)
            {
                if (i >= compositionCurrentMediaCount)
                {
                    ReleaseCompositionMedia(i);
                    continue;
                }

                CompositionPanelSettings settings = compositionProfile.Panels[i];
                MediaAudioSettings audio = settings.GetAudioSettings();
                if (compositionPanelSurfaces[i] != null && compositionPanelSurfaces[i].AudioEnabled != audio.Enabled)
                {
                    ReleaseCompositionMedia(i);
                }

                string file = settings.File ?? "";
                if (!string.Equals(file, compositionPanelLoaded[i], StringComparison.Ordinal))
                {
                    ReleaseCompositionMedia(i);
                    compositionPanelLoaded[i] = file;
                    compositionPanelSurfaces[i] = CreateCompositionSurface(file, "Media layer " + (i + 1), settings.Paused, audio);
                }

                if (compositionPanelSurfaces[i] != null)
                {
                    compositionPanelSurfaces[i].TryApplyAudio(audio);
                }
            }
        }

        private MediaSurface CreateCompositionSurface(string fileName, string label, bool paused, MediaAudioSettings audio)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            try
            {
                string extension = Path.GetExtension(fileName);
                if (!CompositionPolicy.IsSupportedPanelExtension(extension))
                {
                    compositionMessage = label + ": unsupported format. Use PNG, JPG, GIF or video.";
                    return null;
                }

                string path = MenuMediaPolicy.Resolve(menuMediaDirectory, fileName);
                if (!File.Exists(path))
                {
                    compositionMessage = label + ": file is missing.";
                    return null;
                }

                bool browserPause = textureBrowserVisible && pauseGameWhileBrowserOpen;
                MediaSurface surface = new MediaSurface(path, paused || browserPause, audio);
                surface.Update(paused || browserPause);
                return surface;
            }
            catch (Exception ex)
            {
                compositionMessage = label + ": " + ex.Message;
                return null;
            }
        }

        private void UpdateCompositionMedia()
        {
            bool browserPause = textureBrowserVisible && pauseGameWhileBrowserOpen;
            for (int i = 0; i < CompositionPolicy.MaxMediaPanels; i++)
            {
                if (i >= compositionCurrentMediaCount)
                {
                    compositionPanelDrawable[i] = false;
                    if (compositionPanelMaterials[i] != null && compositionPanelMaterials[i].mainTexture != null)
                    {
                        compositionPanelMaterials[i].mainTexture = null;
                    }

                    continue;
                }

                CompositionPanelSettings settings = compositionProfile.Panels[i];
                if (compositionPanelSurfaces[i] != null)
                {
                    compositionPanelSurfaces[i].Update(settings.Paused || browserPause);
                }

                Texture texture = compositionPanelSurfaces[i] != null ? compositionPanelSurfaces[i].Texture : null;
                Material material = compositionPanelMaterials[i];
                if (material != null && material.mainTexture != texture)
                {
                    material.mainTexture = texture;
                }

                bool drawable = texture != null;
                if (drawable != compositionPanelDrawable[i])
                {
                    compositionPanelDrawable[i] = drawable;
                    compositionBufferDirty = true;
                }

                if (compositionPanelSurfaces[i] != null && compositionPanelSurfaces[i].Failed)
                {
                    compositionMessage = "Media layer " + (i + 1) + ": " + compositionPanelSurfaces[i].Message;
                }
            }
        }

        private void RefreshCompositionPanelMeshes()
        {
            if (compositionCurrentLayout.PanelCount <= 0)
            {
                return;
            }

            for (int i = 0; i < compositionCurrentMediaCount && i < CompositionPolicy.MaxMediaPanels; i++)
            {
                UpdateCompositionMesh(i,
                    compositionPanelSurfaces[i] != null ? compositionPanelSurfaces[i].Texture : null,
                    compositionCurrentLayout.Panels[i]);
            }
        }

        private void UpdateCompositionMesh(int index, Texture texture, CompositionRect panel)
        {
            Mesh mesh = compositionPanelMeshes[index];
            if (mesh == null)
            {
                mesh = CreateCompositionQuad("Media " + (index + 1));
                compositionPanelMeshes[index] = mesh;
                compositionPanelTextureAspect[index] = -1f;
                compositionPanelAspect[index] = -1f;
                compositionPanelPlacement[index] = "";
            }

            float textureAspect = texture != null && texture.height > 0 ? texture.width / (float)texture.height : 1f;
            float panelAspect = panel.Height > 0 ? panel.Width / (float)Mathf.Max(1, panel.Height) : 1f;
            string placement = CompositionPolicy.NormalizePlacement(compositionProfile.Placement);
            if (Mathf.Abs(textureAspect - compositionPanelTextureAspect[index]) > 0.0001f ||
                Mathf.Abs(panelAspect - compositionPanelAspect[index]) > 0.0001f ||
                !string.Equals(placement, compositionPanelPlacement[index], StringComparison.Ordinal))
            {
                ApplyCompositionMeshGeometry(mesh, textureAspect, panelAspect, placement);
                compositionPanelTextureAspect[index] = textureAspect;
                compositionPanelAspect[index] = panelAspect;
                compositionPanelPlacement[index] = placement;
            }
        }

        private static void ApplyCompositionMeshGeometry(Mesh mesh, float textureAspect, float panelAspect, string placement)
        {
            float halfX = 1f;
            float halfY = 1f;
            float u0 = 0f;
            float v0 = 0f;
            float u1 = 1f;
            float v1 = 1f;

            if (string.Equals(placement, CompositionPolicy.PlacementContain, StringComparison.Ordinal))
            {
                // Fit the whole image inside the tile; the uncovered part shows the background color.
                if (textureAspect > panelAspect)
                {
                    halfY = panelAspect / textureAspect;
                }
                else if (textureAspect < panelAspect)
                {
                    halfX = textureAspect / panelAspect;
                }
            }
            else if (!string.Equals(placement, CompositionPolicy.PlacementStretch, StringComparison.Ordinal))
            {
                // Cover: fill the tile and crop the overflowing axis, like the menu background.
                if (textureAspect > panelAspect)
                {
                    float span = panelAspect / textureAspect;
                    u0 = (1f - span) * 0.5f;
                    u1 = 1f - u0;
                }
                else if (textureAspect < panelAspect)
                {
                    float span = textureAspect / panelAspect;
                    v0 = (1f - span) * 0.5f;
                    v1 = 1f - v0;
                }
            }

            mesh.vertices = new[]
            {
                new Vector3(-halfX, -halfY, 0f),
                new Vector3(-halfX, halfY, 0f),
                new Vector3(halfX, halfY, 0f),
                new Vector3(halfX, -halfY, 0f)
            };
            mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0) };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f, 1f));
        }

        private static Mesh CreateCompositionQuad(string sizeName)
        {
            Mesh mesh = new Mesh { name = "ShaderStripMod Composition " + sizeName, hideFlags = HideFlags.HideAndDontSave };
            ApplyCompositionMeshGeometry(mesh, 1f, 1f, CompositionPolicy.PlacementCover);
            return mesh;
        }

        private void RebuildCompositionBuffer()
        {
            compositionBufferDirty = false;
            if (compositionBuffer == null || compositionBufferCamera == null || compositionCurrentLayout.Panels == null)
            {
                return;
            }

            CompositionLayout layout = compositionCurrentLayout;
            compositionBuffer.Clear();
            for (int i = 0; i < layout.PanelCount && i < CompositionPolicy.MaxMediaPanels; i++)
            {
                DrawCompositionPanel(layout.Panels[i], compositionPanelMeshes[i], compositionPanelMaterials[i], compositionPanelDrawable[i]);
            }

            compositionBuffer.SetViewport(new Rect(0f, 0f, layout.ScreenWidth, layout.ScreenHeight));
            compositionBuffer.SetViewProjectionMatrices(compositionBufferCamera.worldToCameraMatrix, compositionBufferCamera.projectionMatrix);
        }

        private void OnCompositionCameraPreCull(Camera camera)
        {
            if (!compositionActive || camera == null || camera != compositionBufferCamera || compositionBuffer == null)
            {
                return;
            }

            // The gameplay camera moves in LateUpdate; rebuild so the restore commands use this frame's matrices.
            RebuildCompositionBuffer();
        }

        private void DrawCompositionPanel(CompositionRect panel, Mesh mesh, Material material, bool visible)
        {
            if (!visible || mesh == null || material == null || panel.IsEmpty)
            {
                return;
            }

            compositionBuffer.SetViewport(new Rect(panel.X, panel.Y, panel.Width, panel.Height));
            compositionBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            compositionBuffer.DrawMesh(mesh, Matrix4x4.identity, material);
        }

        private void ReleaseComposition()
        {
            compositionActive = false;
            if (compositionBuffer != null)
            {
                if (compositionBufferCamera != null)
                {
                    try { compositionBufferCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, compositionBuffer); }
                    catch (Exception) { }
                }

                compositionBuffer.Release();
                compositionBuffer = null;
            }

            compositionBufferCamera = null;
            for (int i = 0; i < CompositionPolicy.MaxMediaPanels; i++)
            {
                ReleaseCompositionMedia(i);
            }

            for (int i = 0; i < CompositionPolicy.MaxMediaPanels; i++)
            {
                DestroyCompositionObject(compositionPanelMaterials[i]);
                compositionPanelMaterials[i] = null;
                DestroyCompositionObject(compositionPanelMeshes[i]);
                compositionPanelMeshes[i] = null;
                compositionPanelTextureAspect[i] = -1f;
                compositionPanelAspect[i] = -1f;
                compositionPanelPlacement[i] = "";
                compositionPanelDrawable[i] = false;
            }

            compositionCurrentMediaCount = 0;
            compositionBufferDirty = true;
        }

        private void ReleaseCompositionMedia(int index)
        {
            if (index < 0 || index >= CompositionPolicy.MaxMediaPanels)
            {
                return;
            }

            ReleaseCompositionSurface(ref compositionPanelSurfaces[index]);
            compositionPanelLoaded[index] = null;
            compositionPanelDrawable[index] = false;
            if (compositionPanelMaterials[index] != null && compositionPanelMaterials[index].mainTexture != null)
            {
                compositionPanelMaterials[index].mainTexture = null;
            }
        }

        private void ReleaseCompositionSurface(ref MediaSurface surface)
        {
            if (surface != null)
            {
                surface.Dispose();
                surface = null;
            }
        }

        private static void DestroyCompositionObject(UnityEngine.Object target)
        {
            if (target != null)
            {
                UnityEngine.Object.Destroy(target);
            }
        }

        private int GetCompositionMediaCountForLayout()
        {
            CompositionLayout layout = CompositionPolicy.CalculateLayout(compositionProfile.Layout,
                Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            return layout.PanelCount;
        }

        private string GetCompositionPanelLabel(int mediaIndex)
        {
            CompositionLayout layout = CompositionPolicy.CalculateLayout(compositionProfile.Layout,
                Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            return CompositionPolicy.GetPanelName(layout, mediaIndex);
        }

        private bool CompositionProfileFileExists()
        {
            try
            {
                return File.Exists(GetCompositionProfilePath());
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void MigrateLegacyCompositionSettings(string layout, string leftFile, string rightFile, bool leftPaused, bool rightPaused)
        {
            compositionProfile = CompositionProfile.CreateDefault();
            compositionProfile.Layout = CompositionPolicy.NormalizeLayout(layout);
            compositionProfile.Panels[0].File = NormalizeLegacyCompositionFile(leftFile);
            compositionProfile.Panels[0].Paused = leftPaused;
            compositionProfile.Panels[1].File = NormalizeLegacyCompositionFile(rightFile);
            compositionProfile.Panels[1].Paused = rightPaused;
        }

        private static string NormalizeLegacyCompositionFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return "";
            }

            return CompositionPolicy.IsSupportedPanelExtension(Path.GetExtension(file)) &&
                   CompositionProfileCodec.IsSafeMediaName(file)
                ? file
                : "";
        }

        private string GetCompositionProfilePath()
        {
            string directory = string.IsNullOrEmpty(settingsFilePath) ? null : Path.GetDirectoryName(settingsFilePath);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Application.persistentDataPath;
            }

            return Path.Combine(directory, CompositionProfileCodec.FileName);
        }

        private void LoadCompositionProfile()
        {
            compositionProfileReadOnlyVersion = 0;
            compositionProfileSaveStatus = "";
            try
            {
                string path = GetCompositionProfilePath();
                if (!File.Exists(path))
                {
                    return;
                }

                CompositionProfile loaded;
                int formatVersion;
                string error;
                if (!CompositionProfileCodec.TryParse(File.ReadAllText(path, System.Text.Encoding.UTF8), out loaded, out formatVersion, out error))
                {
                    compositionProfileReadOnlyVersion = formatVersion > 0 ? formatVersion : -1;
                    compositionProfileDirty = false;
                    compositionProfileSaveTime = 0f;
                    compositionProfileMessage = BuildCompositionReadOnlyMessage();
                    compositionProfileSaveStatus = UiText("Not saved: the preset file is protected.");
                    Log("ShaderStripMod composition preset was not loaded: " + error);
                    return;
                }

                compositionProfile = loaded;
                compositionBufferDirty = true;
                compositionProfileMessage = "Composition preset loaded.";
                compositionProfileSaveStatus = UiText("Saved.");
            }
            catch (Exception ex)
            {
                compositionProfileReadOnlyVersion = -1;
                compositionProfileDirty = false;
                compositionProfileSaveTime = 0f;
                compositionProfileMessage = "Composition preset error: " + ex.Message;
                compositionProfileSaveStatus = UiText("Not saved: the preset file could not be read.");
            }
        }

        private string BuildCompositionReadOnlyMessage()
        {
            if (compositionProfileReadOnlyVersion > 0)
            {
                return UiText("Composition preset was not loaded: formatVersion {version} is not supported. The file will not be overwritten.")
                    .Replace("{version}", compositionProfileReadOnlyVersion.ToString());
            }

            return UiText("Composition preset was not loaded: the formatVersion value is invalid. The file will not be overwritten.");
        }

        private void SaveCompositionProfile()
        {
            if (compositionProfileReadOnlyVersion != 0)
            {
                compositionProfileDirty = false;
                compositionProfileSaveTime = 0f;
                compositionProfileMessage = BuildCompositionReadOnlyMessage();
                compositionProfileSaveStatus = UiText("Not saved: the preset file is protected.");
                return;
            }

            try
            {
                AtomicTextFile.Write(GetCompositionProfilePath(), CompositionProfileCodec.Serialize(compositionProfile));
                compositionProfileDirty = false;
                compositionProfileSaveTime = 0f;
                compositionProfileMessage = "Composition preset saved.";
                compositionProfileSaveStatus = UiText("Saved.");
            }
            catch (Exception ex)
            {
                compositionProfileDirty = true;
                compositionProfileSaveTime = Time.unscaledTime + 5f;
                compositionProfileMessage = "Composition preset error: " + ex.Message;
                compositionProfileSaveStatus = UiText("Save failed. Changes remain unsaved; retrying.");
                Log("ShaderStripMod composition preset save failed: " + ex.Message);
            }
        }

        private bool TryBackupCompositionProfile(out string backupPath)
        {
            backupPath = "";
            string path = GetCompositionProfilePath();
            if (!File.Exists(path))
            {
                return true;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    directory = ".";
                }

                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                backupPath = GetUniqueFilePath(Path.Combine(directory, CompositionProfileCodec.FileName + ".bak_before_reset_" + stamp));
                File.Copy(path, backupPath);
                return true;
            }
            catch (Exception ex)
            {
                backupPath = "";
                Log("ShaderStripMod composition preset backup failed: " + ex.Message);
                return false;
            }
        }

        private void ReloadCompositionProfile()
        {
            LoadCompositionProfile();
            compositionBufferDirty = true;
            ReleaseComposition();
            UpdateComposition();
        }

        private void MarkCompositionProfileDirty()
        {
            if (compositionProfileReadOnlyVersion != 0)
            {
                compositionProfileDirty = false;
                compositionProfileSaveStatus = UiText("Not saved: the preset file is protected.");
                return;
            }

            compositionProfileDirty = true;
            compositionProfileSaveTime = Time.unscaledTime + 0.5f;
            compositionProfileSaveStatus = UiText("Unsaved changes. Autosave starts in 0.5 seconds.");
        }

        private void FlushCompositionProfile()
        {
            if (compositionProfileDirty)
            {
                SaveCompositionProfile();
            }
        }

        private void SetCompositionLayout(string layout)
        {
            string normalized = CompositionPolicy.NormalizeLayout(layout);
            if (string.Equals(normalized, compositionProfile.Layout, StringComparison.Ordinal))
            {
                return;
            }

            compositionProfile.Layout = normalized;
            MarkCompositionProfileDirty();
            compositionBufferDirty = true;
            UpdateComposition();
        }

        private void SetCompositionPlacement(string placement)
        {
            string normalized = CompositionPolicy.NormalizePlacement(placement);
            if (string.Equals(normalized, compositionProfile.Placement, StringComparison.Ordinal))
            {
                return;
            }

            compositionProfile.Placement = normalized;
            for (int i = 0; i < CompositionPolicy.MaxMediaPanels; i++)
            {
                compositionPanelTextureAspect[i] = -1f;
                compositionPanelAspect[i] = -1f;
                compositionPanelPlacement[i] = "";
            }

            MarkCompositionProfileDirty();
            compositionBufferDirty = true;
            UpdateComposition();
        }

        private void SetCompositionPanelFile(int index, string file)
        {
            if (index < 0 || index >= CompositionPolicy.MaxMediaPanels)
            {
                return;
            }

            if (!string.IsNullOrEmpty(file) &&
                (!CompositionPolicy.IsSupportedPanelExtension(Path.GetExtension(file)) || !CompositionProfileCodec.IsSafeMediaName(file)))
            {
                compositionMessage = "Choose a PNG, JPG, GIF or video from the imported menu-media list.";
                return;
            }

            string nextFile = file ?? "";
            if (string.Equals(compositionProfile.Panels[index].File, nextFile, StringComparison.Ordinal) &&
                compositionPanelSurfaces[index] != null && !compositionPanelSurfaces[index].Failed)
            {
                return;
            }

            compositionProfile.Panels[index].File = nextFile;
            compositionPanelLoaded[index] = null;
            ReleaseCompositionMedia(index);
            MarkCompositionProfileDirty();
            UpdateComposition();
        }

        private void ClearCompositionPanel(int index)
        {
            SetCompositionPanelFile(index, "");
        }

        private void ClearCompositionPanelFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < CompositionPolicy.MaxMediaPanels; i++)
            {
                if (!string.Equals(compositionProfile.Panels[i].File, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                compositionProfile.Panels[i].File = "";
                compositionPanelLoaded[i] = null;
                ReleaseCompositionMedia(i);
                changed = true;
            }

            if (changed)
            {
                MarkCompositionProfileDirty();
            }
        }

        private void SetCompositionPanelPaused(int index, bool paused)
        {
            if (index < 0 || index >= CompositionPolicy.MaxMediaPanels)
            {
                return;
            }

            compositionProfile.Panels[index].Paused = paused;
            MarkCompositionProfileDirty();
        }

        private void SetCompositionPanelSoundEnabled(int index, bool enabled)
        {
            if (index < 0 || index >= CompositionPolicy.MaxMediaPanels)
            {
                return;
            }

            if (compositionProfile.Panels[index].SoundEnabled == enabled)
            {
                return;
            }

            compositionProfile.Panels[index].SoundEnabled = enabled;
            compositionPanelLoaded[index] = null;
            ReleaseCompositionMedia(index);
            MarkCompositionProfileDirty();
            UpdateComposition();
        }

        private void SetCompositionPanelMuted(int index, bool muted)
        {
            if (index < 0 || index >= CompositionPolicy.MaxMediaPanels)
            {
                return;
            }

            compositionProfile.Panels[index].Muted = muted;
            MarkCompositionProfileDirty();
        }

        private void SetCompositionPanelVolume(int index, float volume)
        {
            if (index < 0 || index >= CompositionPolicy.MaxMediaPanels)
            {
                return;
            }

            float clamped = MediaAudioPolicy.ClampVolume(volume, MediaAudioPolicy.DefaultVolume);
            if (Mathf.Abs(clamped - compositionProfile.Panels[index].Volume) < 0.0001f)
            {
                return;
            }

            compositionProfile.Panels[index].Volume = clamped;
            MarkCompositionProfileDirty();
        }

        private void ResetComposition()
        {
            string backup = "";
            if (compositionProfileReadOnlyVersion != 0 && !TryBackupCompositionProfile(out backup))
            {
                compositionMessage = "Composition preset backup failed. Nothing was reset.";
                compositionProfileSaveStatus = UiText("Not saved: the preset file could not be backed up.");
                return;
            }

            compositionProfileReadOnlyVersion = 0;
            compositionEnabled = false;
            compositionProfile = CompositionProfile.CreateDefault();
            compositionProfileDirty = false;
            compositionProfileSaveTime = 0f;
            compositionMessage = "Choose a layout, then import PNG, JPG, GIF or video files for its media tiles.";
            ReleaseComposition();
            SaveExportSettings();
            SaveCompositionProfile();
            if (!string.IsNullOrEmpty(backup))
            {
                Log("ShaderStripMod replaced a protected composition preset after backup: " + backup + ".");
            }

            compositionInactiveReason = CompositionInactiveReason.Disabled;
            if (gameplayIsolationEnabled)
            {
                ApplyStrip();
            }
        }

        private string DescribeCompositionReason(CompositionInactiveReason reason)
        {
            switch (reason)
            {
                case CompositionInactiveReason.Disabled:
                    return "Composition is off.";
                case CompositionInactiveReason.GameplayIsolationOff:
                    return HotkeyText("Enable gameplay isolation{keySuffix} to show the media collage.", HotkeyActions.ToggleGameplayIsolation);
                case CompositionInactiveReason.MenuScene:
                    return "Composition starts in a gameplay room, not in the title menu.";
                case CompositionInactiveReason.HeroUnavailable:
                    return "Waiting for the hero to spawn.";
                case CompositionInactiveReason.NotGameplayScene:
                    return "Waiting for a gameplay room.";
                case CompositionInactiveReason.Transition:
                    return "Waiting for the scene transition or cutscene to finish.";
                case CompositionInactiveReason.WorldCameraMissing:
                    return "Gameplay camera was not found.";
                case CompositionInactiveReason.LayoutUnavailable:
                    return "The window is too small for the selected layout.";
                case CompositionInactiveReason.SurfaceUnavailable:
                    return "Could not create the media collage material.";
                case CompositionInactiveReason.RuntimeError:
                    return "Composition stopped after a rendering error. See the log.";
                default:
                    return "Composition is active.";
            }
        }

        private string DescribeCompositionState()
        {
            return compositionEnabled && compositionActive && compositionInactiveReason == CompositionInactiveReason.None
                ? "Composition is active."
                : DescribeCompositionReason(compositionInactiveReason);
        }
    }
}
