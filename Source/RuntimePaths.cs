using System.Collections.Generic;
using System.IO;
using System.Text;
using Modding;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {

        private string NormalizeExportFolderMode(string mode)
        {
            string cleaned = CleanUnityName(mode);
            if (string.Equals(cleaned, ExportModeMod, System.StringComparison.OrdinalIgnoreCase))
            {
                return ExportModeMod;
            }

            if (string.Equals(cleaned, ExportModeCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                return ExportModeCustom;
            }

            return ExportModeDownloads;
        }

        private string GetExportModeForLegacyDirectory(string configuredDirectory)
        {
            if (string.IsNullOrEmpty(configuredDirectory))
            {
                return ExportModeDownloads;
            }

            if (LooksLikeDownloadsExportDirectory(configuredDirectory))
            {
                return ExportModeDownloads;
            }

            if (PathsEqual(configuredDirectory, GetModExportDirectory()))
            {
                return ExportModeMod;
            }

            return ExportModeCustom;
        }

        private void RefreshExportDirectoryFromMode()
        {
            if (string.Equals(exportFolderMode, ExportModeMod, System.StringComparison.OrdinalIgnoreCase))
            {
                exportDirectory = GetModExportDirectory();
            }
            else if (!string.Equals(exportFolderMode, ExportModeCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                exportFolderMode = ExportModeDownloads;
                exportDirectory = GetDefaultExportDirectory();
            }
        }

        private bool LooksLikeDownloadsExportDirectory(string directory)
        {
            string fullPath = TryGetFullPath(directory);
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            string trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string leaf = Path.GetFileName(trimmed);
            string parent = Path.GetDirectoryName(trimmed);
            string parentLeaf = !string.IsNullOrEmpty(parent) ? Path.GetFileName(parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) : "";
            return string.Equals(leaf, ExportFolderName, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parentLeaf, "Downloads", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool PathsEqual(string left, string right)
        {
            string leftFull = TryGetFullPath(left);
            string rightFull = TryGetFullPath(right);
            return !string.IsNullOrEmpty(leftFull) &&
                !string.IsNullOrEmpty(rightFull) &&
                string.Equals(leftFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    rightFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    System.StringComparison.OrdinalIgnoreCase);
        }

        private string TryGetFullPath(string path)
        {
            try
            {
                string cleaned = CleanPathSetting(path);
                return string.IsNullOrEmpty(cleaned) ? "" : Path.GetFullPath(System.Environment.ExpandEnvironmentVariables(cleaned));
            }
            catch
            {
                return "";
            }
        }

        private string CleanPathSetting(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }

            return path.Trim().Trim('"');
        }

        private string GetDefaultExportDirectory()
        {
            string userProfile = "";
            try
            {
                userProfile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            }
            catch
            {
                userProfile = "";
            }

            if (!string.IsNullOrEmpty(userProfile))
            {
                return Path.Combine(Path.Combine(userProfile, "Downloads"), ExportFolderName);
            }

            return GetModExportDirectory();
        }

        private string GetModExportDirectory()
        {
            string baseDirectory = "";
            try
            {
                if (!string.IsNullOrEmpty(settingsFilePath))
                {
                    baseDirectory = Path.GetDirectoryName(settingsFilePath);
                }
                else if (!string.IsNullOrEmpty(preserveFilePath))
                {
                    baseDirectory = Path.GetDirectoryName(preserveFilePath);
                }
            }
            catch
            {
                baseDirectory = "";
            }

            if (string.IsNullOrEmpty(baseDirectory))
            {
                baseDirectory = Application.persistentDataPath;
            }

            return Path.Combine(baseDirectory, "exports");
        }

        private string BuildDefaultPreserveFile()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# ShaderStripMod preserve list");
            builder.AppendLine("# Press F10 in game to write " + DumpFileName + ".");
            builder.AppendLine("# Preferred: rendererPath= entries preserve exact renderer instances.");
            builder.AppendLine("# Use sprite=, texture=, material=, path=, groupPath=, or projectilePath= entries.");
            builder.AppendLine("# path= is exact object only. groupPath= and projectilePath= include children.");
            builder.AppendLine("# Plain lines are treated as legacy asset rules.");
            builder.AppendLine("# F11 reloads this file without restarting the game.");
            builder.AppendLine("# Lines starting with # or // are ignored.");
            return builder.ToString();
        }

    }
}
