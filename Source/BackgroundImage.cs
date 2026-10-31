using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public IntPtr lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName openFileName);

        [DllImport("comdlg32.dll")]
        private static extern int CommDlgExtendedError();

        private const int OpenFileNameExplorer = 0x00080000;
        private const int OpenFileNameFileMustExist = 0x00001000;
        private const int OpenFileNamePathMustExist = 0x00000800;
        private const int OpenFileNameNoChangeDirectory = 0x00000008;
        private void InitializeBackgroundImagePaths()
        {
            string baseDirectory = !string.IsNullOrEmpty(settingsFilePath) ? Path.GetDirectoryName(settingsFilePath) : Application.persistentDataPath;
            backgroundImagesDirectory = Path.Combine(string.IsNullOrEmpty(baseDirectory) ? Application.persistentDataPath : baseDirectory, BackgroundImageFolderName);
            try { Directory.CreateDirectory(backgroundImagesDirectory); } catch (Exception ex) { backgroundImageMessage = "Background folder error: " + ex.Message; }
            backgroundImageFilesDirty = true;
        }

        private string NormalizeBackgroundMode(string value)
        {
            return string.Equals(value, BackgroundModeImage, StringComparison.OrdinalIgnoreCase) ? BackgroundModeImage : BackgroundModeColor;
        }

        private string NormalizeBackgroundImageLayout(string value)
        {
            if (string.Equals(value, BackgroundImageLayoutContain, StringComparison.OrdinalIgnoreCase)) return BackgroundImageLayoutContain;
            if (string.Equals(value, BackgroundImageLayoutStretch, StringComparison.OrdinalIgnoreCase)) return BackgroundImageLayoutStretch;
            return BackgroundImageLayoutCover;
        }

        private void RefreshBackgroundImageFiles()
        {
            if (!backgroundImageFilesDirty) return;
            backgroundImageFilesDirty = false;
            backgroundImageFiles.Clear();
            try
            {
                if (string.IsNullOrEmpty(backgroundImagesDirectory)) InitializeBackgroundImagePaths();
                if (!Directory.Exists(backgroundImagesDirectory)) return;
                string root = Path.GetFullPath(backgroundImagesDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (string path in Directory.GetFiles(backgroundImagesDirectory, "*", SearchOption.TopDirectoryOnly))
                {
                    string full = Path.GetFullPath(path);
                    string extension = Path.GetExtension(full);
                    if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!IsSupportedBackgroundImageExtension(extension)) continue;
                    backgroundImageFiles.Add(Path.GetFileName(full));
                }
                backgroundImageFiles.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) { backgroundImageMessage = "Image discovery failed: " + ex.Message; }
        }

        private void SetBackgroundImage(string fileName)
        {
            RefreshBackgroundImageFiles();
            if (string.IsNullOrWhiteSpace(fileName) || !backgroundImageFiles.Contains(fileName))
            {
                backgroundImageMessage = "Select an available image.";
                return;
            }
            string previousMode = backgroundMode;
            string previousImage = backgroundImageRelativePath;
            ReleaseImageBackground();
            backgroundMode = BackgroundModeImage;
            backgroundImageRelativePath = fileName;
            if (!TryLoadImageBackground())
            {
                backgroundMode = previousMode;
                backgroundImageRelativePath = previousImage;
                return;
            }
            SaveExportSettings();
            if (visualsActive) ApplyStrip();
            backgroundImageMessage = "Background image selected: " + fileName;
        }
        private void RemoveBackgroundImage(string fileName)
        {
            RefreshBackgroundImageFiles();
            if (string.IsNullOrWhiteSpace(fileName) || !backgroundImageFiles.Contains(fileName))
            {
                backgroundImageMessage = "Image is no longer available.";
                return;
            }

            try
            {
                string root = Path.GetFullPath(backgroundImagesDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string fullPath = Path.GetFullPath(Path.Combine(backgroundImagesDirectory, fileName));
                if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) { backgroundImageMessage = "Image path must stay inside background-images."; return; }
                string trash = Path.Combine(backgroundImagesDirectory, "removed");
                Directory.CreateDirectory(trash);
                File.Move(fullPath, Path.Combine(trash, Guid.NewGuid().ToString("N") + "_" + fileName));
                backgroundTrashDirty = true;
                bool wasSelected = string.Equals(backgroundImageRelativePath, fileName, StringComparison.OrdinalIgnoreCase);
                if (wasSelected)
                {
                    backgroundImageRelativePath = "";
                    backgroundMode = BackgroundModeColor;
                    ReleaseImageBackground();
                }

                backgroundImageFilesDirty = true;
                RefreshBackgroundImageFiles();
                SaveExportSettings();
                if (visualsActive) ApplyStrip();
                backgroundImageMessage = "Removed image: " + fileName;
            }
            catch (Exception ex) { backgroundImageMessage = "Could not remove image: " + ex.Message; }
        }

        private void ChooseBackgroundImage()
        {
            string selectedPath;
            try { selectedPath = ShowBackgroundImageFilePicker(); }
            catch (Exception ex) { backgroundImageMessage = "Could not open image picker: " + ex.Message; return; }
            if (string.IsNullOrEmpty(selectedPath)) { backgroundImageMessage = "Image selection cancelled."; return; }

            try
            {
                string extension = Path.GetExtension(selectedPath);
                if (!IsSupportedBackgroundImageExtension(extension)) { backgroundImageMessage = "Choose a PNG, JPG or JPEG image."; return; }
                FileInfo source = new FileInfo(selectedPath);
                if (!source.Exists || source.Length <= 0 || source.Length > 64L * 1024L * 1024L) { backgroundImageMessage = "Image must be between 1 byte and 64 MiB."; return; }
                MenuMediaPolicy.ValidateImageDimensions(File.ReadAllBytes(source.FullName),extension);
                if (string.IsNullOrEmpty(backgroundImagesDirectory)) InitializeBackgroundImagePaths();
                Directory.CreateDirectory(backgroundImagesDirectory);
                string sourceFullPath = Path.GetFullPath(selectedPath);
                string folderFullPath = Path.GetFullPath(backgroundImagesDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string fileName = Path.GetFileName(sourceFullPath);
                if (!string.Equals(Path.GetDirectoryName(sourceFullPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, folderFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    fileName = GetAvailableBackgroundImageFileName(fileName);
                    File.Copy(sourceFullPath, Path.Combine(backgroundImagesDirectory, fileName), false);
                }
                backgroundImageFilesDirty = true;
                SetBackgroundImage(fileName);
            }
            catch (Exception ex) { backgroundImageMessage = "Could not import image: " + ex.Message; }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        private string ShowBackgroundImageFilePicker(string filter=null,string title=null,string directory=null)
        {
            OpenFileName dialog = new OpenFileName();
            dialog.lStructSize = Marshal.SizeOf(typeof(OpenFileName));
            dialog.hwndOwner = GetActiveWindow();
            dialog.lpstrFilter = filter ?? "Image files\0*.png;*.jpg;*.jpeg\0PNG files\0*.png\0JPEG files\0*.jpg;*.jpeg\0\0";
            dialog.nMaxFile = 32768;
            dialog.lpstrFile = Marshal.AllocHGlobal(dialog.nMaxFile * 2);
            try
            {
                Marshal.WriteInt16(dialog.lpstrFile, 0);
                dialog.lpstrTitle = UiText(title ?? "Choose background image");
                dialog.lpstrInitialDir = Directory.Exists(directory ?? backgroundImagesDirectory) ? (directory ?? backgroundImagesDirectory) : null;
                dialog.Flags = OpenFileNameExplorer | OpenFileNameFileMustExist | OpenFileNamePathMustExist | OpenFileNameNoChangeDirectory;
                if (GetOpenFileName(dialog)) return Marshal.PtrToStringUni(dialog.lpstrFile);
                int error = CommDlgExtendedError();
                if (error != 0) throw new IOException("Windows file picker error: " + error);
                return "";
            }
            finally { Marshal.FreeHGlobal(dialog.lpstrFile); }
        }
        private bool IsSupportedBackgroundImageExtension(string extension)
        {
            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private string GetAvailableBackgroundImageFileName(string originalFileName)
        {
            string name = Path.GetFileNameWithoutExtension(originalFileName);
            string extension = Path.GetExtension(originalFileName);
            string candidate = originalFileName;
            int suffix = 2;
            while (File.Exists(Path.Combine(backgroundImagesDirectory, candidate)))
            {
                candidate = name + " (" + suffix + ")" + extension;
                suffix++;
            }
            return candidate;
        }

        private void SetBackgroundMode(string mode)
        {
            backgroundMode = NormalizeBackgroundMode(mode);
            SaveExportSettings();
            if (string.Equals(backgroundMode, BackgroundModeColor, StringComparison.OrdinalIgnoreCase)) ReleaseImageBackground();
            if (visualsActive) ApplyStrip();
        }

        private void SetBackgroundImageLayout(string layout)
        {
            backgroundImageLayout = NormalizeBackgroundImageLayout(layout);
            SaveExportSettings();
            UpdateImageBackground();
            backgroundImageMessage = "Image layout: " + backgroundImageLayout + ".";
        }

        private bool IsImageBackgroundEnabled()
        {
            return string.Equals(backgroundMode, BackgroundModeImage, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(backgroundImageRelativePath);
        }

        private Camera imageCamera;
        private CommandBuffer imageDrawBuffer;
        private Mesh imageMesh;
        private Material imageMaterial;
        private float imageMeshWidth = -1f, imageMeshHeight = -1f;
        private float nextImageFileCheck;
        private float nextImageLoadAttempt;

        private bool TryLoadImageBackground()
        {
            if (!IsImageBackgroundEnabled()) return false;
            if (backgroundImageTexture != null) return true;
            if (Time.unscaledTime < nextImageLoadAttempt) return false;
            nextImageLoadAttempt = Time.unscaledTime + 2f;
            Texture2D texture = null;
            Material material = null;
            try
            {
                string root = Path.GetFullPath(backgroundImagesDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string path = Path.GetFullPath(Path.Combine(root, backgroundImageRelativePath));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new IOException("Image must be inside background-images.");
                FileInfo info = new FileInfo(path);
                if (!info.Exists || info.Length <= 0 || info.Length > 64L * 1024L * 1024L) throw new IOException("Image missing, empty or larger than 64 MiB.");
                byte[] encoded = File.ReadAllBytes(path);
                MenuMediaPolicy.ValidateImageDimensions(encoded, Path.GetExtension(path));
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(encoded, true)) throw new IOException("Cannot decode image.");
                MenuMediaPolicy.ValidateDecodedImageDimensions(texture.width, texture.height);
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) throw new IOException("Sprites/Default shader is unavailable.");
                material = new Material(shader);
                material.mainTexture = texture;
                texture.name = "ShaderStripMod Background " + backgroundImageRelativePath;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                backgroundImageTexture = texture;
                imageMaterial = material;
                loadedBackgroundImagePath = path;
                loadedBackgroundImageLastWriteUtc = info.LastWriteTimeUtc;
                backgroundImageMessage = "Loaded: " + backgroundImageRelativePath;
                return true;
            }
            catch (Exception ex)
            {
                if (material != null) UnityEngine.Object.Destroy(material);
                if (texture != null) UnityEngine.Object.Destroy(texture);
                backgroundImageMessage = "Image load failed: " + ex.Message;
                return false;
            }
        }

        private Camera GetImageBackgroundCamera()
        {
            Camera[] cameras = Camera.allCameras;
            return IsMenuScene(GetActiveSceneName()) ? GetMenuBackgroundCamera(cameras)
                : GetGameplayWorldCamera(cameras);
        }

        private void ConfigureImageBackgroundForCamera(Camera camera, bool visible)
        {
            if (visualsActive && visible) EnforceBackgroundCameraFrame(camera);
            Texture drawTexture=null;Material drawMaterial=null;string layout=backgroundImageLayout;
            bool hasCustom=TryGetCustomMenuBackground(out drawTexture,out drawMaterial);
            if(hasCustom)layout=customMenuLayout;
            else if(IsImageBackgroundEnabled()&&TryLoadImageBackground()){drawTexture=backgroundImageTexture;drawMaterial=imageMaterial;}
            if (!visualsActive || !visible || camera == null || drawTexture==null || drawMaterial==null)
            {
                HideImageBackground();
                return;
            }
            if (camera != imageCamera || imageDrawBuffer == null)
            {
                HideImageBackground();
                imageCamera = camera;
                imageDrawBuffer = new CommandBuffer { name = "ShaderStripMod Screen Background" };
                camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, imageDrawBuffer);
            }
            if (imageMesh == null)
            {
                imageMesh = new Mesh { name = "ShaderStripMod Background Quad" };
                imageMesh.vertices = new Vector3[4];
                imageMesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            }
            BackgroundImageGeometry.Size size = BackgroundImageGeometry.Calculate(drawTexture.width, drawTexture.height,
                Mathf.Max(1f, Screen.width), Mathf.Max(1f, Screen.height), layout);
            float x = size.Width * (1f + 2f / Mathf.Max(1f, Screen.width));
            float y = size.Height * (1f + 2f / Mathf.Max(1f, Screen.height));
            if (x != imageMeshWidth || y != imageMeshHeight)
            {
                imageMesh.vertices = new[] { new Vector3(-x, -y, 0), new Vector3(-x, y, 0), new Vector3(x, y, 0), new Vector3(x, -y, 0) };
                imageMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
                imageMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                imageMeshWidth = x;
                imageMeshHeight = y;
            }
            imageDrawBuffer.Clear();
            imageDrawBuffer.SetViewport(new Rect(0, 0, Screen.width, Screen.height));
            imageDrawBuffer.ClearRenderTarget(false, true, stripBackground);
            // Clip-space quad is independent of player position, camera motion and scene layers.
            imageDrawBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            imageDrawBuffer.DrawMesh(imageMesh, Matrix4x4.identity, drawMaterial);
            imageDrawBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            imageDrawBuffer.SetViewport(camera.pixelRect);
        }

        private void OnImageBackgroundCameraPreCull(Camera camera)
        {
            if (!visualsActive) return;
            EnforceMenuCameraFrame(camera);
            ReassertGameplayCameraBeforeCull(camera);
            ReassertMenuCameraBeforeCull(camera);
            spriteMaskVisibility.Reassert(r => r!=null,(r,value) => r.forceRenderingOff=value);
            if (camera != GetImageBackgroundCamera()) return;
            EnforceBackgroundCameraFrame(camera);
            ConfigureImageBackgroundForCamera(camera, true);
        }

        private void HideImageBackground()
        {
            if (imageDrawBuffer != null)
            {
                if (imageCamera != null) imageCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, imageDrawBuffer);
                imageDrawBuffer.Release();
                imageDrawBuffer = null;
            }
            imageCamera = null;
        }

        private void UpdateImageBackground()
        {
            UpdateCustomMenu();
            if (!visualsActive || (!IsImageBackgroundEnabled() && !IsCustomMenuActive())) { HideImageBackground(); return; }
            if (Time.unscaledTime >= nextImageFileCheck)
            {
                nextImageFileCheck = Time.unscaledTime + 1f;
                try
                {
                    if (!string.IsNullOrEmpty(loadedBackgroundImagePath) &&
                        (!File.Exists(loadedBackgroundImagePath) || File.GetLastWriteTimeUtc(loadedBackgroundImagePath) != loadedBackgroundImageLastWriteUtc)) ReleaseImageBackground();
                }
                catch (Exception ex) { backgroundImageMessage = "Image check failed: " + ex.Message; }
            }
            ConfigureImageBackgroundForCamera(GetImageBackgroundCamera(), true);
        }

        private void ReleaseImageBackground()
        {
            HideImageBackground();
            if (imageMesh != null) UnityEngine.Object.Destroy(imageMesh);
            if (imageMaterial != null) UnityEngine.Object.Destroy(imageMaterial);
            if (backgroundImageTexture != null) UnityEngine.Object.Destroy(backgroundImageTexture);
            imageMesh = null;
            imageMeshWidth = imageMeshHeight = -1f;
            imageMaterial = null;
            backgroundImageTexture = null;
            loadedBackgroundImagePath = "";
            loadedBackgroundImageLastWriteUtc = DateTime.MinValue;
            nextImageLoadAttempt = 0f;
        }
    }
}







