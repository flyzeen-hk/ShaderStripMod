using System;
using System.Collections.Generic;
using System.Text;

namespace ShaderStripMod.VisualRules
{
    internal sealed class RendererHierarchySegment
    {
        internal string Name = "";
        internal int SiblingIndex = 0;
    }

    internal sealed class RendererIdentityInput
    {
        internal string SceneName = "";
        internal readonly List<RendererHierarchySegment> Hierarchy = new List<RendererHierarchySegment>();
        internal string RendererType = "";
        internal int RendererComponentIndex = 0;
        internal int MaterialIndex = -1;
        internal string TexturePropertyName = "";
        internal string SpriteName = "";
        internal string TextureName = "";
        internal int TextureWidth = -1;
        internal int TextureHeight = -1;
        internal string MaterialName = "";
        internal string ProjectileFamily = "";
        internal bool IsPreserved = false;
    }

    internal sealed class RendererIdentity
    {
        internal const int CurrentIdentityVersion = 1;

        internal int IdentityVersion = CurrentIdentityVersion;
        internal string SceneName = "";
        internal string ObjectPath = "";
        internal string HierarchyDiscriminator = "";
        internal string RendererType = "";
        internal int RendererComponentIndex;
        internal int MaterialIndex = -1;
        internal string TexturePropertyName = "";
        internal string SpriteName = "";
        internal string TextureName = "";
        internal int TextureWidth = -1;
        internal int TextureHeight = -1;
        internal string MaterialName = "";
        internal string ProjectileFamily = "";
        internal bool IsPreserved;

        internal string BuildStableRendererKey()
        {
            StringBuilder builder = new StringBuilder(256);
            AppendKeyPart(builder, "v", IdentityVersion.ToString());
            AppendKeyPart(builder, "scene", SceneName);
            AppendKeyPart(builder, "path", ObjectPath);
            AppendKeyPart(builder, "hierarchy", HierarchyDiscriminator);
            AppendKeyPart(builder, "renderer", RendererType);
            AppendKeyPart(builder, "component", RendererComponentIndex.ToString());
            AppendKeyPart(builder, "material", MaterialIndex.ToString());
            AppendKeyPart(builder, "property", TexturePropertyName);
            return builder.ToString();
        }

        private static void AppendKeyPart(StringBuilder builder, string key, string value)
        {
            if (builder.Length > 0)
            {
                builder.Append('|');
            }

            builder.Append(key);
            builder.Append('=');
            builder.Append(EscapeKeyValue(value));
        }

        private static string EscapeKeyValue(string value)
        {
            return (value ?? "")
                .Replace("\\", "\\\\")
                .Replace("|", "\\|")
                .Replace("=", "\\=");
        }
    }

    internal static class RendererIdentityBuilder
    {
        internal static RendererIdentity Build(RendererIdentityInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            RendererIdentity identity = new RendererIdentity();
            identity.SceneName = NormalizeName(input.SceneName);
            identity.ObjectPath = BuildObjectPath(input.Hierarchy);
            identity.HierarchyDiscriminator = BuildHierarchyDiscriminator(input.Hierarchy);
            identity.RendererType = NormalizeName(input.RendererType);
            identity.RendererComponentIndex = Math.Max(0, input.RendererComponentIndex);
            identity.MaterialIndex = input.MaterialIndex;
            identity.TexturePropertyName = NormalizeName(input.TexturePropertyName);
            identity.SpriteName = NormalizeUnityName(input.SpriteName);
            identity.TextureName = NormalizeUnityName(input.TextureName);
            identity.TextureWidth = input.TextureWidth;
            identity.TextureHeight = input.TextureHeight;
            identity.MaterialName = NormalizeUnityName(input.MaterialName);
            identity.ProjectileFamily = NormalizePath(input.ProjectileFamily);
            identity.IsPreserved = input.IsPreserved;
            return identity;
        }

        internal static string BuildObjectPath(IList<RendererHierarchySegment> hierarchy)
        {
            if (hierarchy == null || hierarchy.Count == 0)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hierarchy.Count; i++)
            {
                string name = NormalizeUnityName(hierarchy[i] != null ? hierarchy[i].Name : "");
                if (name.Length == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('/');
                }

                builder.Append(name);
            }

            return builder.ToString();
        }

        internal static string BuildHierarchyDiscriminator(IList<RendererHierarchySegment> hierarchy)
        {
            if (hierarchy == null || hierarchy.Count == 0)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hierarchy.Count; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('/');
                }

                RendererHierarchySegment segment = hierarchy[i];
                builder.Append(segment != null ? Math.Max(0, segment.SiblingIndex) : 0);
            }

            return builder.ToString();
        }

        internal static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            string[] parts = path.Trim().Trim('"').Replace('\\', '/').Split('/');
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = NormalizeUnityName(parts[i]);
                if (part.Length == 0)
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

        internal static string NormalizeUnityName(string value)
        {
            string cleaned = NormalizeName(value);
            cleaned = RemoveSuffix(cleaned, " (Instance)");
            cleaned = RemoveSuffix(cleaned, "(Instance)");
            cleaned = RemoveSuffix(cleaned, " (Clone)");
            cleaned = RemoveSuffix(cleaned, "(Clone)");
            return cleaned.Trim();
        }

        internal static string NormalizeName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim().Trim('"');
        }

        private static string RemoveSuffix(string value, string suffix)
        {
            int index = value.IndexOf(suffix, StringComparison.Ordinal);
            return index >= 0 ? value.Substring(0, index) : value;
        }
    }
}
