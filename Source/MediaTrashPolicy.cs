using System;
using System.Globalization;
using System.IO;

namespace ShaderStripMod
{
    internal static class MediaTrashPolicy
    {
        internal static string GetOriginalName(string trashFileName)
        {
            if (string.IsNullOrEmpty(trashFileName))
            {
                return "";
            }

            string name = Path.GetFileName(trashFileName);
            if (name.Length > 33 && name[32] == '_')
            {
                bool guidPrefix = true;
                for (int i = 0; i < 32; i++)
                {
                    if (!Uri.IsHexDigit(name[i]))
                    {
                        guidPrefix = false;
                        break;
                    }
                }

                if (guidPrefix)
                {
                    return name.Substring(33);
                }
            }

            return name;
        }

        internal static string FormatSize(long bytes)
        {
            if (bytes < 0)
            {
                bytes = 0;
            }

            if (bytes < 1024)
            {
                return bytes + " B";
            }

            double value = bytes / 1024.0;
            if (value < 1024.0)
            {
                return value.ToString("0.#", CultureInfo.InvariantCulture) + " KB";
            }

            value /= 1024.0;
            if (value < 1024.0)
            {
                return value.ToString("0.#", CultureInfo.InvariantCulture) + " MB";
            }

            return (value / 1024.0).ToString("0.##", CultureInfo.InvariantCulture) + " GB";
        }

        internal static bool IsPathInside(string filePath, string directory)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(directory))
            {
                return false;
            }

            try
            {
                string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string full = Path.GetFullPath(filePath);
                return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        internal static string GetUniqueFilePath(string directory, string fileName, Func<string, bool> exists)
        {
            string name = Path.GetFileName(fileName);
            string stem = string.IsNullOrEmpty(name) ? "restored" : Path.GetFileNameWithoutExtension(name);
            if (string.IsNullOrEmpty(stem))
            {
                stem = "restored";
            }

            string extension = string.IsNullOrEmpty(name) ? "" : Path.GetExtension(name);
            string candidate = Path.Combine(directory, string.IsNullOrEmpty(name) ? stem + extension : name);
            int suffix = 2;
            while (exists != null && exists(candidate))
            {
                candidate = Path.Combine(directory, stem + " (" + suffix + ")" + extension);
                suffix++;
            }

            return candidate;
        }
    }
}
