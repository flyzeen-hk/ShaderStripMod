using System;
using System.Collections.Generic;
using System.IO;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private sealed class RemovedMediaFile
        {
            internal string Path;
            internal string DisplayName;
            internal long Size;
        }

        private readonly List<RemovedMediaFile> backgroundTrashFiles = new List<RemovedMediaFile>();
        private readonly List<RemovedMediaFile> menuTrashFiles = new List<RemovedMediaFile>();
        private bool backgroundTrashDirty = true;
        private long backgroundTrashBytes;
        private long menuTrashBytes;
        private bool confirmEmptyBackgroundTrash;
        private bool confirmEmptyMenuTrash;
        private string pendingDeleteBackgroundTrash;
        private string pendingDeleteMenuTrash;

        private string GetBackgroundTrashDirectory()
        {
            if (string.IsNullOrEmpty(backgroundImagesDirectory))
            {
                InitializeBackgroundImagePaths();
            }

            return Path.Combine(backgroundImagesDirectory, "removed");
        }

        private string GetMenuTrashDirectory()
        {
            if (string.IsNullOrEmpty(menuMediaDirectory))
            {
                InitializeCustomMenuPaths();
            }

            return Path.Combine(menuMediaDirectory, "removed");
        }

        private void RefreshBackgroundTrashFiles()
        {
            if (!backgroundTrashDirty)
            {
                return;
            }

            backgroundTrashDirty = false;
            backgroundTrashFiles.Clear();
            backgroundTrashBytes = 0;
            try
            {
                string trash = GetBackgroundTrashDirectory();
                if (!Directory.Exists(trash))
                {
                    pendingDeleteBackgroundTrash = null;
                    return;
                }

                string root = Path.GetFullPath(trash).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (string path in Directory.GetFiles(trash, "*", SearchOption.TopDirectoryOnly))
                {
                    string full = Path.GetFullPath(path);
                    if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !IsSupportedBackgroundImageExtension(Path.GetExtension(full)))
                    {
                        continue;
                    }

                    try
                    {
                        FileInfo info = new FileInfo(full);
                        if (!info.Exists)
                        {
                            continue;
                        }

                        RemovedMediaFile item = new RemovedMediaFile();
                        item.Path = full;
                        item.DisplayName = MediaTrashPolicy.GetOriginalName(Path.GetFileName(full));
                        item.Size = info.Length;
                        backgroundTrashFiles.Add(item);
                        backgroundTrashBytes += item.Size;
                    }
                    catch
                    {
                    }
                }

                backgroundTrashFiles.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
                pendingDeleteBackgroundTrash = FindTrashPath(backgroundTrashFiles, pendingDeleteBackgroundTrash);
            }
            catch (Exception ex)
            {
                pendingDeleteBackgroundTrash = null;
                backgroundImageMessage = "Trash discovery failed: " + ex.Message;
            }
        }

        private void RefreshMenuTrashFiles()
        {
            if (!MenuTrashDirty)
            {
                return;
            }

            MenuTrashDirty = false;
            menuTrashFiles.Clear();
            menuTrashBytes = 0;
            try
            {
                string trash = GetMenuTrashDirectory();
                if (!Directory.Exists(trash))
                {
                    pendingDeleteMenuTrash = null;
                    return;
                }

                string root = Path.GetFullPath(trash).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (string path in Directory.GetFiles(trash, "*", SearchOption.TopDirectoryOnly))
                {
                    string full = Path.GetFullPath(path);
                    if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !MenuMediaPolicy.IsSupported(Path.GetExtension(full)))
                    {
                        continue;
                    }

                    try
                    {
                        FileInfo info = new FileInfo(full);
                        if (!info.Exists)
                        {
                            continue;
                        }

                        RemovedMediaFile item = new RemovedMediaFile();
                        item.Path = full;
                        item.DisplayName = MediaTrashPolicy.GetOriginalName(Path.GetFileName(full));
                        item.Size = info.Length;
                        menuTrashFiles.Add(item);
                        menuTrashBytes += item.Size;
                    }
                    catch
                    {
                    }
                }

                menuTrashFiles.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
                pendingDeleteMenuTrash = FindTrashPath(menuTrashFiles, pendingDeleteMenuTrash);
            }
            catch (Exception ex)
            {
                pendingDeleteMenuTrash = null;
                customMenuMessage = "Menu trash error: " + ex.Message;
            }
        }

        private void RestoreBackgroundTrashFile(string path)
        {
            if (string.Equals(pendingDeleteBackgroundTrash, path, StringComparison.OrdinalIgnoreCase))
            {
                pendingDeleteBackgroundTrash = null;
            }

            try
            {
                string trash = GetBackgroundTrashDirectory();
                if (!MediaTrashPolicy.IsPathInside(path, trash))
                {
                    backgroundImageMessage = "Trash file path is invalid.";
                    return;
                }

                string name = MediaTrashPolicy.GetOriginalName(Path.GetFileName(path));
                if (string.IsNullOrEmpty(name) || !IsSupportedBackgroundImageExtension(Path.GetExtension(name)))
                {
                    backgroundImageMessage = "Trash file has an unsupported image type.";
                    return;
                }

                Directory.CreateDirectory(backgroundImagesDirectory);
                string target = MediaTrashPolicy.GetUniqueFilePath(backgroundImagesDirectory, name, File.Exists);
                File.Move(path, target);
                backgroundTrashDirty = true;
                backgroundImageFilesDirty = true;
                RefreshBackgroundTrashFiles();
                RefreshBackgroundImageFiles();
                backgroundImageMessage = "Restored image: " + Path.GetFileName(target) + ".";
            }
            catch (Exception ex)
            {
                backgroundImageMessage = "Could not restore image: " + ex.Message;
            }
        }

        private void DeleteBackgroundTrashFile(string path)
        {
            pendingDeleteBackgroundTrash = null;
            try
            {
                string trash = GetBackgroundTrashDirectory();
                if (!MediaTrashPolicy.IsPathInside(path, trash))
                {
                    backgroundImageMessage = "Trash file path is invalid.";
                    return;
                }

                File.Delete(path);
                backgroundTrashDirty = true;
                RefreshBackgroundTrashFiles();
                backgroundImageMessage = "Permanently deleted image from trash.";
            }
            catch (Exception ex)
            {
                backgroundImageMessage = "Could not delete image: " + ex.Message;
            }
        }

        private void EmptyBackgroundTrash()
        {
            confirmEmptyBackgroundTrash = false;
            pendingDeleteBackgroundTrash = null;
            try
            {
                RefreshBackgroundTrashFiles();
                string trash = GetBackgroundTrashDirectory();
                int deleted = 0;
                int failed = 0;
                long bytes = 0;
                for (int i = 0; i < backgroundTrashFiles.Count; i++)
                {
                    RemovedMediaFile item = backgroundTrashFiles[i];
                    if (!MediaTrashPolicy.IsPathInside(item.Path, trash))
                    {
                        failed++;
                        continue;
                    }

                    try
                    {
                        File.Delete(item.Path);
                        deleted++;
                        bytes += item.Size;
                    }
                    catch
                    {
                        failed++;
                    }
                }

                backgroundTrashDirty = true;
                RefreshBackgroundTrashFiles();
                backgroundImageMessage = deleted == 0 && failed == 0
                    ? "Image trash is already empty."
                    : "Permanently deleted " + deleted + " image(s), freed " + MediaTrashPolicy.FormatSize(bytes) +
                        (failed > 0 ? "; " + failed + " file(s) could not be deleted." : ".");
            }
            catch (Exception ex)
            {
                backgroundImageMessage = "Could not empty image trash: " + ex.Message;
            }
        }

        private void RestoreMenuTrashFile(string path)
        {
            if (string.Equals(pendingDeleteMenuTrash, path, StringComparison.OrdinalIgnoreCase))
            {
                pendingDeleteMenuTrash = null;
            }

            try
            {
                string trash = GetMenuTrashDirectory();
                if (!MediaTrashPolicy.IsPathInside(path, trash))
                {
                    customMenuMessage = "Trash file path is invalid.";
                    return;
                }

                string name = MediaTrashPolicy.GetOriginalName(Path.GetFileName(path));
                if (string.IsNullOrEmpty(name) || !MenuMediaPolicy.IsSupported(Path.GetExtension(name)))
                {
                    customMenuMessage = "Trash file has an unsupported media type.";
                    return;
                }

                Directory.CreateDirectory(menuMediaDirectory);
                string target = MediaTrashPolicy.GetUniqueFilePath(menuMediaDirectory, name, File.Exists);
                File.Move(path, target);
                MenuTrashDirty = true;
                menuMediaFilesDirty = true;
                RefreshMenuTrashFiles();
                RefreshMenuMediaFiles();
                customMenuMessage = "Restored menu media: " + Path.GetFileName(target) + ".";
            }
            catch (Exception ex)
            {
                customMenuMessage = "Menu media error: " + ex.Message;
            }
        }

        private void DeleteMenuTrashFile(string path)
        {
            pendingDeleteMenuTrash = null;
            try
            {
                string trash = GetMenuTrashDirectory();
                if (!MediaTrashPolicy.IsPathInside(path, trash))
                {
                    customMenuMessage = "Trash file path is invalid.";
                    return;
                }

                File.Delete(path);
                MenuTrashDirty = true;
                RefreshMenuTrashFiles();
                customMenuMessage = "Permanently deleted menu media from trash.";
            }
            catch (Exception ex)
            {
                customMenuMessage = "Menu media error: " + ex.Message;
            }
        }

        private void EmptyMenuTrash()
        {
            confirmEmptyMenuTrash = false;
            pendingDeleteMenuTrash = null;
            try
            {
                RefreshMenuTrashFiles();
                string trash = GetMenuTrashDirectory();
                int deleted = 0;
                int failed = 0;
                long bytes = 0;
                for (int i = 0; i < menuTrashFiles.Count; i++)
                {
                    RemovedMediaFile item = menuTrashFiles[i];
                    if (!MediaTrashPolicy.IsPathInside(item.Path, trash))
                    {
                        failed++;
                        continue;
                    }

                    try
                    {
                        File.Delete(item.Path);
                        deleted++;
                        bytes += item.Size;
                    }
                    catch
                    {
                        failed++;
                    }
                }

                MenuTrashDirty = true;
                RefreshMenuTrashFiles();
                customMenuMessage = deleted == 0 && failed == 0
                    ? "Menu media trash is already empty."
                    : "Permanently deleted " + deleted + " menu media file(s), freed " + MediaTrashPolicy.FormatSize(bytes) +
                        (failed > 0 ? "; " + failed + " file(s) could not be deleted." : ".");
            }
            catch (Exception ex)
            {
                customMenuMessage = "Menu media error: " + ex.Message;
            }
        }

        private static string FindTrashPath(List<RemovedMediaFile> files, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            for (int i = 0; i < files.Count; i++)
            {
                if (string.Equals(files[i].Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    return files[i].Path;
                }
            }

            return null;
        }
    }
}
