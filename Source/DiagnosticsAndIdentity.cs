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
        internal void DumpLoadedSpritesAndTextures()
        {
            if (string.IsNullOrEmpty(dumpFilePath))
            {
                InitializeListFiles();
            }

            try
            {
                HashSet<string> spriteNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                HashSet<string> textureNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                HashSet<string> materialNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                HashSet<string> rendererRows = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                LoadExistingDump(spriteNames, textureNames, materialNames, rendererRows);

                Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite sprite = sprites[i];
                    AddObjectName(sprite, spriteNames);
                    if (sprite != null)
                    {
                        AddObjectName(sprite.texture, textureNames);
                    }
                }

                Texture[] textures = Resources.FindObjectsOfTypeAll<Texture>();
                for (int i = 0; i < textures.Length; i++)
                {
                    AddObjectName(textures[i], textureNames);
                }

                Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    AddObjectName(material, materialNames);
                    AddObjectName(GetMaterialMainTexture(material), textureNames);
                }

                Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    CollectRendererDumpRows(renderers[i], rendererRows, spriteNames, textureNames, materialNames);
                }

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("# ShaderStripMod loaded sprite/texture dump");
                builder.AppendLine("# Scene: " + GetActiveSceneName());
                builder.AppendLine("# Copy a name from [Textures], [Sprites], or [Materials] into " + PreserveFileName + ", then press F11.");
                builder.AppendLine();

                AppendNameSection(builder, "[Textures]", textureNames);
                AppendNameSection(builder, "[Sprites]", spriteNames);
                AppendNameSection(builder, "[Materials]", materialNames);

                builder.AppendLine("[RendererRows]");
                List<string> sortedRows = new List<string>(rendererRows);
                sortedRows.Sort(System.StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < sortedRows.Count; i++)
                {
                    builder.AppendLine(sortedRows[i]);
                }

                File.WriteAllText(dumpFilePath, builder.ToString(), Encoding.UTF8);
                Log("ShaderStripMod wrote cumulative sprite/texture dump to " + dumpFilePath + " (" + textureNames.Count + " textures, " + spriteNames.Count + " sprites, " + rendererRows.Count + " renderer rows).");
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod dump failed: " + ex.Message);
            }
        }

        private void LoadExistingDump(HashSet<string> spriteNames, HashSet<string> textureNames, HashSet<string> materialNames, HashSet<string> rendererRows)
        {
            if (string.IsNullOrEmpty(dumpFilePath) || !File.Exists(dumpFilePath))
            {
                return;
            }

            try
            {
                string section = "";
                string[] lines = File.ReadAllLines(dumpFilePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line;
                        continue;
                    }

                    if (section == "[Textures]")
                    {
                        AddCleanName(line, textureNames);
                    }
                    else if (section == "[Sprites]")
                    {
                        AddCleanName(line, spriteNames);
                    }
                    else if (section == "[Materials]")
                    {
                        AddCleanName(line, materialNames);
                    }
                    else if (section == "[RendererRows]")
                    {
                        rendererRows.Add(line);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod existing dump read failed: " + ex.Message);
            }
        }

        private void CollectRendererDumpRows(Renderer renderer, HashSet<string> rendererRows, HashSet<string> spriteNames, HashSet<string> textureNames, HashSet<string> materialNames)
        {
            if (renderer == null || IsProxyRenderer(renderer))
            {
                return;
            }

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            Texture spriteTexture = sprite != null ? sprite.texture : null;
            AddObjectName(sprite, spriteNames);
            AddObjectName(spriteTexture, textureNames);

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                rendererRows.Add(BuildRendererDumpRow(renderer, sprite, spriteTexture, null, null));
                return;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                Texture materialTexture = GetMaterialMainTexture(material);
                AddObjectName(material, materialNames);
                AddObjectName(materialTexture, textureNames);
                rendererRows.Add(BuildRendererDumpRow(renderer, sprite, spriteTexture, material, materialTexture));
            }
        }

        private string BuildRendererDumpRow(Renderer renderer, Sprite sprite, Texture spriteTexture, Material material, Texture materialTexture)
        {
            string shaderName = material != null && material.shader != null ? CleanUnityName(material.shader.name) : "";
            return "path=" + GetObjectPath(renderer.gameObject) +
                "\tlayer=" + GetLayerLabel(renderer.gameObject) +
                "\trenderer=" + renderer.GetType().Name +
                "\tsprite=" + GetObjectName(sprite) +
                "\tspriteTexture=" + GetObjectName(spriteTexture) +
                "\tmaterial=" + GetObjectName(material) +
                "\tmaterialTexture=" + GetObjectName(materialTexture) +
                "\tshader=" + shaderName;
        }

        private void AppendNameSection(StringBuilder builder, string title, HashSet<string> names)
        {
            List<string> sorted = new List<string>(names);
            sorted.Sort(System.StringComparer.OrdinalIgnoreCase);
            builder.AppendLine(title);
            for (int i = 0; i < sorted.Count; i++)
            {
                builder.AppendLine(sorted[i]);
            }

            builder.AppendLine();
        }

        private void AddObjectName(UnityEngine.Object obj, HashSet<string> names)
        {
            string name = GetObjectName(obj);
            if (!string.IsNullOrEmpty(name))
            {
                names.Add(name);
            }
        }

        private void AddCleanName(string name, HashSet<string> names)
        {
            string cleaned = CleanUnityName(name);
            if (!string.IsNullOrEmpty(cleaned))
            {
                names.Add(cleaned);
            }
        }

        private string GetObjectName(UnityEngine.Object obj)
        {
            return obj != null ? CleanUnityName(obj.name) : "";
        }

        private string CleanUnityName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "";
            }

            string cleaned = name.Trim().Trim('"');
            cleaned = RemoveUnitySuffix(cleaned, " (Instance)");
            cleaned = RemoveUnitySuffix(cleaned, "(Instance)");
            cleaned = RemoveUnitySuffix(cleaned, " (Clone)");
            cleaned = RemoveUnitySuffix(cleaned, "(Clone)");
            return cleaned.Trim();
        }

        private string CleanUnityPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }

            string[] parts = path.Trim().Trim('"').Split('/');
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = CleanUnityName(parts[i]);
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('/');
                }

                builder.Append(part);
            }

            return builder.ToString();
        }

        private string RemoveUnitySuffix(string name, string suffix)
        {
            int suffixIndex = name.IndexOf(suffix, System.StringComparison.Ordinal);
            return suffixIndex >= 0 ? name.Substring(0, suffixIndex) : name;
        }

        private string GetProjectileFamilyPath(string path)
        {
            string cleaned = CleanUnityPath(path);
            if (string.IsNullOrEmpty(cleaned))
            {
                return "";
            }

            string[] parts = cleaned.Split('/');
            int start = GetProjectileFamilyPathStart(parts);
            if (start <= 0)
            {
                return cleaned;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = start; i < parts.Length; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('/');
                }

                builder.Append(parts[i]);
            }

            return builder.ToString();
        }

        private int GetProjectileFamilyPathStart(string[] parts)
        {
            if (parts == null || parts.Length == 0)
            {
                return 0;
            }

            if (parts.Length >= 3 &&
                string.Equals(parts[0], "_GameManager", System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parts[1], "GlobalPool", System.StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (parts.Length >= 2 &&
                (string.Equals(parts[0], "GlobalPool", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parts[0], "Global Pool", System.StringComparison.OrdinalIgnoreCase)))
            {
                return 1;
            }

            return 0;
        }

        private Texture GetMaterialMainTexture(Material material)
        {
            if (material == null)
            {
                return null;
            }

            for (int i = 0; i < MaterialTextureProperties.Length; i++)
            {
                try
                {
                    if (material.HasProperty(MaterialTextureProperties[i]))
                    {
                        Texture texture = material.GetTexture(MaterialTextureProperties[i]);
                        if (texture != null)
                        {
                            return texture;
                        }
                    }
                }
                catch
                {
                }
            }

            try
            {
                return material.mainTexture;
            }
            catch
            {
            }

            return null;
        }

        private string GetObjectPath(GameObject obj)
        {
            if (obj == null)
            {
                return "";
            }

            int instanceId = obj.GetInstanceID();
            string cachedPath;
            if (rendererPathCacheEnabled && objectPathCache.TryGetValue(instanceId, out cachedPath))
            {
                objectPathCacheHits++;
                return cachedPath;
            }

            objectPathCacheMisses++;
            objectPathSegments.Clear();
            Transform current = obj.transform;
            while (current != null)
            {
                objectPathSegments.Add(CleanUnityName(current.name));
                current = current.parent;
            }

            objectPathBuilder.Length = 0;
            for (int i = objectPathSegments.Count - 1; i >= 0; i--)
            {
                if (objectPathBuilder.Length > 0)
                {
                    objectPathBuilder.Append('/');
                }

                objectPathBuilder.Append(objectPathSegments[i]);
            }

            string path = objectPathBuilder.ToString();
            if (rendererPathCacheEnabled && maxCachedRendererPaths > 0)
            {
                AddObjectPathCacheEntry(instanceId, path);
            }

            return path;
        }

        private void AddObjectPathCacheEntry(int instanceId, string path)
        {
            if (objectPathCache.ContainsKey(instanceId))
            {
                objectPathCache[instanceId] = path;
                return;
            }

            while (objectPathCache.Count >= maxCachedRendererPaths && objectPathCacheOrder.Count > 0)
            {
                int removeId = objectPathCacheOrder[0];
                objectPathCacheOrder.RemoveAt(0);
                if (objectPathCache.Remove(removeId))
                {
                    objectPathCacheEvictions++;
                }
            }

            if (objectPathCache.Count >= maxCachedRendererPaths)
            {
                return;
            }

            objectPathCache[instanceId] = path;
            objectPathCacheOrder.Add(instanceId);
        }

        private string GetLayerLabel(GameObject obj)
        {
            if (obj == null)
            {
                return "";
            }

            string layerName = LayerMask.LayerToName(obj.layer);
            if (string.IsNullOrEmpty(layerName))
            {
                layerName = "Layer" + obj.layer;
            }

            return layerName + "(" + obj.layer + ")";
        }

        private string FormatVector3(Vector3 value)
        {
            return "(" + value.x.ToString("0.00") + ", " + value.y.ToString("0.00") + ", " + value.z.ToString("0.00") + ")";
        }

        private string GetActiveSceneName()
        {
            try
            {
                return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
            catch
            {
                return "";
            }
        }

        private bool IsMenuScene(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) &&
                sceneName.StartsWith("Menu", System.StringComparison.OrdinalIgnoreCase);
        }

    }
}
