using System;
using System.IO;
using System.Text;

namespace ShaderStripMod.VisualRules
{
    internal sealed class VisualRuleSaveResult
    {
        internal string TargetPath = "";
        internal string BackupPath = "";
        internal int RuleCount;
    }

    internal static class VisualRuleFileStore
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        internal static VisualRuleParseResult Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A visual-rules path is required.", "path");
            }

            if (!File.Exists(path))
            {
                VisualRuleParseResult missing = new VisualRuleParseResult();
                missing.Document.FormatVersion = VisualRuleDocument.SupportedFormatVersion;
                return missing;
            }

            byte[] bytes = File.ReadAllBytes(path);
            return VisualRuleCodec.Parse(StrictUtf8.GetString(bytes));
        }

        internal static VisualRuleSaveResult SaveAtomic(string path, VisualRuleDocument document, string backupDirectory)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A visual-rules path is required.", "path");
            }

            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("The visual-rules file must have a parent directory.");
            }

            Directory.CreateDirectory(directory);
            string serialized = VisualRuleCodec.Serialize(document);
            VisualRuleParseResult memoryValidation = VisualRuleCodec.Parse(serialized);
            if (!memoryValidation.Success || memoryValidation.Document.Rules.Count != document.Rules.Count)
            {
                throw new InvalidDataException("Refusing to save visual-rules because writer validation failed.");
            }

            string temporaryPath = fullPath + ".tmp." + Guid.NewGuid().ToString("N");
            string backupPath = "";
            try
            {
                WriteAndFlush(temporaryPath, serialized);
                VisualRuleParseResult diskValidation = Load(temporaryPath);
                if (!diskValidation.Success || diskValidation.Document.Rules.Count != document.Rules.Count)
                {
                    throw new InvalidDataException("Refusing to replace visual-rules because temporary-file validation failed.");
                }

                if (File.Exists(fullPath))
                {
                    backupPath = CreateBackup(fullPath, backupDirectory);
                    File.Replace(temporaryPath, fullPath, null, true);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }

                return new VisualRuleSaveResult
                {
                    TargetPath = fullPath,
                    BackupPath = backupPath,
                    RuleCount = document.Rules.Count
                };
            }
            catch
            {
                TryDelete(temporaryPath);
                throw;
            }
        }

        private static void WriteAndFlush(string path, string text)
        {
            byte[] bytes = StrictUtf8.GetBytes(text);
            using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static string CreateBackup(string sourcePath, string backupDirectory)
        {
            string directory = !string.IsNullOrWhiteSpace(backupDirectory)
                ? Path.GetFullPath(backupDirectory)
                : Path.Combine(Path.GetDirectoryName(sourcePath), "backups");
            Directory.CreateDirectory(directory);

            string fileName = Path.GetFileName(sourcePath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string candidate = Path.Combine(directory, fileName + ".backup_" + timestamp);
            int suffix = 1;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(directory, fileName + ".backup_" + timestamp + "_" + suffix);
                suffix++;
            }

            File.Copy(sourcePath, candidate, false);
            return candidate;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}
