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
        private void DrawTextureExportControls(Rect rect)
        {
            if (string.IsNullOrEmpty(exportDirectoryInput))
            {
                exportDirectoryInput = !string.IsNullOrEmpty(exportDirectory) ? exportDirectory : GetDefaultExportDirectory();
            }

            UiLabel(new Rect(rect.x, rect.y + 2f, 52f, rect.height), "Export");
            exportDirectoryInput = GUI.TextField(new Rect(rect.x + 56f, rect.y, Mathf.Max(80f, rect.width - 254f), rect.height), exportDirectoryInput);

            if (UiButton(new Rect(rect.xMax - 194f, rect.y, 56f, rect.height), "Save"))
            {
                SetExportDirectoryFromInput();
            }

            if (UiButton(new Rect(rect.xMax - 134f, rect.y, 80f, rect.height), "Downloads"))
            {
                exportFolderMode = ExportModeDownloads;
                RefreshExportDirectoryFromMode();
                SaveExportSettings();
                lastTextureExportMessage = "Export folder set to Downloads.";
            }

            if (UiButton(new Rect(rect.xMax - 50f, rect.y, 50f, rect.height), "Mod"))
            {
                exportFolderMode = ExportModeMod;
                RefreshExportDirectoryFromMode();
                SaveExportSettings();
                lastTextureExportMessage = "Export folder set to mod exports.";
            }
        }

        private void SetExportDirectoryFromInput()
        {
            string cleaned = CleanPathSetting(exportDirectoryInput);
            if (string.IsNullOrEmpty(cleaned))
            {
                cleaned = GetDefaultExportDirectory();
            }

            try
            {
                string expanded = System.Environment.ExpandEnvironmentVariables(cleaned);
                string fullPath = Path.GetFullPath(expanded);
                Directory.CreateDirectory(fullPath);
                exportFolderMode = ExportModeCustom;
                exportDirectory = fullPath;
                SaveExportSettings();
                lastTextureExportMessage = "Export folder saved.";
            }
            catch (System.Exception ex)
            {
                lastTextureExportMessage = "Export folder save failed: " + ex.Message;
                Log("ShaderStripMod export folder save failed: " + ex.Message);
            }
        }

        private void ExportTextureBrowserEntry(TextureBrowserEntry entry, bool atlasMode)
        {
            if (entry == null)
            {
                lastTextureExportMessage = "Export failed: no selected entry.";
                return;
            }

            TextureExportSource source = GetExportSource(entry, atlasMode);
            if(entry.MenuResource&&!atlasMode)
            {
                var artwork=GetMenuObjectPreview(entry);
                if(artwork!=null)source=new TextureExportSource {Texture=artwork,SourceDescription="rendered menu object geometry through its atlas UVs"};
            }
            if (source == null || source.Texture == null || source.Texture.width <= 0 || source.Texture.height <= 0)
            {
                lastTextureExportMessage = "Export failed: selected entry has no exportable texture.";
                return;
            }

            string directory = EnsureExportDirectory();
            if (string.IsNullOrEmpty(directory))
            {
                lastTextureExportMessage = "Export failed: no writable export folder.";
                return;
            }

            Texture2D exportTexture = null;
            string regionNote = "";
            try
            {
                exportTexture = source.SpriteRegion ? CopySpriteToTexture2D(source.Sprite, out regionNote) : CopyTextureToTexture2D(source.Texture, out regionNote);
                if (exportTexture == null)
                {
                    lastTextureExportMessage = "Export failed: texture copy returned empty data.";
                    return;
                }

                byte[] png = exportTexture.EncodeToPNG();
                if (png == null || png.Length == 0)
                {
                    lastTextureExportMessage = "Export failed: PNG encoder returned empty data.";
                    return;
                }

                string sourceName = source.SpriteRegion ? GetObjectName(source.Sprite) : GetObjectName(source.Texture);
                string stem = BuildExportFileStem(entry, sourceName, atlasMode);
                string pngPath = GetUniqueFilePath(Path.Combine(directory, stem + ".png"));
                File.WriteAllBytes(pngPath, png);

                string metadataPath = Path.ChangeExtension(pngPath, ".txt");
                File.WriteAllText(metadataPath, BuildTextureExportMetadata(entry, source, exportTexture, regionNote, pngPath, atlasMode), Encoding.UTF8);

                lastTextureExportMessage = "Exported PNG: " + pngPath;
                Log("ShaderStripMod exported texture to " + pngPath + ".");
            }
            catch (System.Exception ex)
            {
                lastTextureExportMessage = "Export failed: " + ex.Message;
                Log("ShaderStripMod texture export failed: " + ex.Message);
            }
            finally
            {
                if (exportTexture != null)
                {
                    Object.Destroy(exportTexture);
                }
            }
        }

        private TextureExportSource GetExportSource(TextureBrowserEntry entry, bool atlasMode)
        {
            if (entry == null)
            {
                return null;
            }

            if (atlasMode)
            {
                if (entry.AtlasSource && entry.Texture != null)
                {
                    return new TextureExportSource
                    {
                        Texture = entry.Texture,
                        Material = entry.Material,
                        SourcePath = entry.ObjectPath,
                        SourceDescription = "selected atlas browser source",
                        Score = entry.AtlasScore
                    };
                }

                TextureExportSource atlasSource = FindBestAtlasExportSource(entry);
                if (atlasSource != null)
                {
                    return atlasSource;
                }
            }

            if (!atlasMode && entry.Sprite != null && entry.Sprite.texture != null)
            {
                return new TextureExportSource
                {
                    Texture = entry.Sprite.texture,
                    Sprite = entry.Sprite,
                    SpriteRegion = true,
                    SourceDescription = "selected sprite region"
                };
            }

            if (entry.Texture != null)
            {
                return new TextureExportSource
                {
                    Texture = entry.Texture,
                    SourceDescription = atlasMode ? "selected texture full atlas" : "selected texture"
                };
            }

            Texture materialTexture = entry.Material != null ? GetMaterialMainTexture(entry.Material) : null;
            if (materialTexture != null)
            {
                return new TextureExportSource
                {
                    Texture = materialTexture,
                    Material = entry.Material,
                    SourceDescription = "selected material main texture"
                };
            }

            if (entry.Sprite != null && entry.Sprite.texture != null)
            {
                return new TextureExportSource
                {
                    Texture = entry.Sprite.texture,
                    Sprite = entry.Sprite,
                    SourceDescription = "selected sprite atlas"
                };
            }

            return null;
        }

        private TextureExportSource FindBestAtlasExportSource(TextureBrowserEntry entry)
        {
            string preservePath = entry != null ? CleanUnityPath(entry.PreservePath) : "";
            if (string.IsNullOrEmpty(preservePath))
            {
                return null;
            }

            string sceneName = GetActiveSceneName();
            TextureExportSource best = null;
            int bestScore = int.MinValue;
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.gameObject == null || IsProxyRenderer(renderer) || !IsRoomRenderer(renderer, sceneName))
                {
                    continue;
                }

                string objectPath = GetObjectPath(renderer.gameObject);
                string cleanedPath = CleanUnityPath(objectPath);
                if (!string.Equals(cleanedPath, preservePath, System.StringComparison.OrdinalIgnoreCase) &&
                    !cleanedPath.StartsWith(preservePath + "/", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsExpensivePreservedRenderer(renderer))
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null)
                {
                    continue;
                }

                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    Texture texture = GetMaterialMainTexture(material);
                    if (texture == null || texture.width <= 0 || texture.height <= 0)
                    {
                        continue;
                    }

                    int score = ScoreAtlasExportSource(renderer, material, texture, cleanedPath, preservePath);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new TextureExportSource
                        {
                            Texture = texture,
                            Material = material,
                            Renderer = renderer,
                            SourcePath = cleanedPath,
                            SourceDescription = "best material atlas under selected path",
                            Score = score
                        };
                    }
                }
            }

            return best;
        }

        private int ScoreAtlasExportSource(Renderer renderer, Material material, Texture texture, string objectPath, string preservePath)
        {
            int score = 0;
            if (string.Equals(objectPath, preservePath, System.StringComparison.OrdinalIgnoreCase))
            {
                score += 1000;
            }

            if (renderer != null && string.Equals(renderer.GetType().Name, "MeshRenderer", System.StringComparison.Ordinal))
            {
                score += 200;
            }

            if (renderer != null && renderer.gameObject != null)
            {
                string layerName = LayerMask.LayerToName(renderer.gameObject.layer);
                if (string.Equals(layerName, "Enemies", System.StringComparison.OrdinalIgnoreCase))
                {
                    score += 80;
                }
            }

            if (ContainsAtlasHint(GetObjectName(texture)))
            {
                score += 60;
            }

            if (ContainsAtlasHint(GetObjectName(material)))
            {
                score += 40;
            }

            if (ContainsAny(objectPath, BossNameHints))
            {
                score += 20;
            }

            return score;
        }

        private bool ContainsAtlasHint(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf("atlas", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string EnsureExportDirectory()
        {
            if (!string.Equals(exportFolderMode, ExportModeCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                RefreshExportDirectoryFromMode();
            }

            string target = CleanPathSetting(exportDirectory);
            if (string.IsNullOrEmpty(target))
            {
                target = GetDefaultExportDirectory();
            }

            try
            {
                target = Path.GetFullPath(System.Environment.ExpandEnvironmentVariables(target));
                Directory.CreateDirectory(target);
                exportDirectory = target;
                exportDirectoryInput = target;
                if (string.Equals(exportFolderMode, ExportModeCustom, System.StringComparison.OrdinalIgnoreCase))
                {
                    SaveExportSettings();
                }

                return target;
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod export folder unavailable: " + ex.Message);
            }

            string fallback = GetModExportDirectory();
            try
            {
                fallback = Path.GetFullPath(fallback);
                Directory.CreateDirectory(fallback);
                exportFolderMode = ExportModeMod;
                exportDirectory = fallback;
                exportDirectoryInput = fallback;
                SaveExportSettings();
                return fallback;
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod fallback export folder unavailable: " + ex.Message);
                return "";
            }
        }

        private Texture2D CopySpriteToTexture2D(Sprite sprite, out string note)
        {
            note = "";
            if (sprite == null || sprite.texture == null)
            {
                note = "No sprite texture.";
                return null;
            }

            Rect rect;
            string spriteNote = "";
            try
            {
                rect = sprite.textureRect;
            }
            catch
            {
                rect = new Rect(0f, 0f, sprite.texture.width, sprite.texture.height);
                spriteNote = "Packed/tight sprite fallback: exported full atlas.";
            }

            Texture2D result = CopyTextureRegionToTexture2D(sprite.texture, rect, out note);
            if (!string.IsNullOrEmpty(spriteNote))
            {
                note = spriteNote + " " + note;
            }

            return result;
        }

        private Texture2D CopyTextureToTexture2D(Texture texture, out string note)
        {
            note = "Full texture export.";
            if (texture == null)
            {
                note = "No texture.";
                return null;
            }

            return CopyTextureRegionToTexture2D(texture, new Rect(0f, 0f, texture.width, texture.height), out note);
        }

        private Texture2D CopyTextureRegionToTexture2D(Texture source, Rect region, out string note)
        {
            note = "Region export.";
            if (source == null || source.width <= 0 || source.height <= 0)
            {
                note = "Invalid source texture.";
                return null;
            }

            int x = Mathf.Clamp(Mathf.RoundToInt(region.x), 0, Mathf.Max(0, source.width - 1));
            int y = Mathf.Clamp(Mathf.RoundToInt(region.y), 0, Mathf.Max(0, source.height - 1));
            int width = Mathf.Clamp(Mathf.RoundToInt(region.width), 1, source.width - x);
            int height = Mathf.Clamp(Mathf.RoundToInt(region.height), 1, source.height - y);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture temporary = null;
            Texture2D result = null;
            try
            {
                temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;

                result = new Texture2D(width, height, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(x, y, width, height), 0, 0);
                result.Apply(false, false);
                note = "Source region: x=" + x + ", y=" + y + ", width=" + width + ", height=" + height + ".";
                return result;
            }
            catch
            {
                if (result != null)
                {
                    Object.Destroy(result);
                }

                throw;
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (temporary != null)
                {
                    RenderTexture.ReleaseTemporary(temporary);
                }
            }
        }

        private string BuildExportFileStem(TextureBrowserEntry entry, string sourceName, bool atlasMode)
        {
            string kind = atlasMode ? "Atlas" : (entry != null ? entry.Kind : "Texture");
            string name = entry != null ? entry.Name : sourceName;
            if (string.IsNullOrEmpty(name))
            {
                name = sourceName;
            }

            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return SanitizeFileName(stamp + "_" + kind + "_" + name);
        }

        private string SanitizeFileName(string name)
        {
            string cleaned = CleanUnityName(name);
            if (string.IsNullOrEmpty(cleaned))
            {
                cleaned = "texture";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < cleaned.Length; i++)
            {
                char c = cleaned[i];
                bool bad = c == '/' || c == '\\';
                for (int j = 0; j < invalid.Length && !bad; j++)
                {
                    bad = c == invalid[j];
                }

                builder.Append(bad ? '_' : c);
            }

            string result = builder.ToString().Trim();
            if (result.Length > 120)
            {
                result = result.Substring(0, 120).Trim();
            }

            return string.IsNullOrEmpty(result) ? "texture" : result;
        }

        private string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            string directory = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            for (int i = 2; i < 1000; i++)
            {
                string candidate = Path.Combine(directory, name + "_" + i + extension);
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(directory, name + "_" + System.DateTime.Now.Ticks + extension);
        }

        private string BuildTextureExportMetadata(TextureBrowserEntry entry, TextureExportSource source, Texture2D exportTexture, string regionNote, string pngPath, bool atlasMode)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("ShaderStripMod texture export");
            builder.AppendLine("Version: " + GetVersion());
            builder.AppendLine("Export mode: " + (atlasMode ? "Atlas/full texture" : "Selected PNG"));
            builder.AppendLine("Exported: " + System.DateTime.Now.ToString("O"));
            builder.AppendLine("PNG: " + pngPath);
            builder.AppendLine("Export folder: " + exportDirectory);
            builder.AppendLine("Source texture: " + GetObjectName(source != null ? source.Texture : null));
            builder.AppendLine("Source texture size: " + GetTextureSize(source != null ? source.Texture : null));
            builder.AppendLine("Source material: " + GetObjectName(source != null ? source.Material : null));
            builder.AppendLine("Source renderer: " + (source != null && source.Renderer != null ? source.Renderer.GetType().Name : ""));
            builder.AppendLine("Source path: " + (source != null ? source.SourcePath : ""));
            builder.AppendLine("Source description: " + (source != null ? source.SourceDescription : ""));
            builder.AppendLine("Source score: " + (source != null ? source.Score.ToString() : ""));
            builder.AppendLine("Exported size: " + GetTextureSize(exportTexture));
            builder.AppendLine("Region: " + regionNote);

            if (entry != null)
            {
                builder.AppendLine();
                builder.AppendLine("Entry name: " + entry.Name);
                builder.AppendLine("Entry kind: " + entry.Kind);
                builder.AppendLine("Preserve path: " + entry.PreservePath);
                builder.AppendLine("Scene: " + entry.SceneName);
                builder.AppendLine("Object path: " + entry.ObjectPath);
                builder.AppendLine("Layer: " + entry.Layer);
                builder.AppendLine("Renderer: " + entry.RendererType);
                if (entry.AtlasSource)
                {
                    builder.AppendLine("Atlas score: " + entry.AtlasScore);
                }

                builder.AppendLine("Position: " + FormatVector3(entry.Position));
                builder.AppendLine("Preview texture: " + GetObjectName(entry.Texture));
                builder.AppendLine("Sprite: " + GetObjectName(entry.Sprite));
                if (entry.Sprite != null)
                {
                    builder.AppendLine("Sprite texture: " + GetObjectName(entry.Sprite.texture));
                    builder.AppendLine("Sprite rect: " + entry.Sprite.rect);
                    try
                    {
                        builder.AppendLine("Sprite texture rect: " + entry.Sprite.textureRect);
                    }
                    catch
                    {
                        builder.AppendLine("Sprite texture rect: unavailable");
                    }
                }

                builder.AppendLine("Material: " + GetObjectName(entry.Material));
                if (entry.Material != null)
                {
                    builder.AppendLine("Material texture: " + GetObjectName(GetMaterialMainTexture(entry.Material)));
                    builder.AppendLine("Shader: " + (entry.Material.shader != null ? CleanUnityName(entry.Material.shader.name) : ""));
                }

                if (!string.IsNullOrEmpty(entry.Details))
                {
                    builder.AppendLine();
                    builder.AppendLine(entry.Details);
                }
            }

            return builder.ToString();
        }

    }
}
