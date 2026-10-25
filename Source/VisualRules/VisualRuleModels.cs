using System;
using System.Collections.Generic;

namespace ShaderStripMod.VisualRules
{
    [Flags]
    internal enum VisualRuleChannel
    {
        None = 0,
        Color = 1,
        Replacement = 2,
        StaticDisplay = 4,
        All = Color | Replacement | StaticDisplay
    }

    internal enum VisualRuleCategory
    {
        Color,
        Replacement,
        StaticDisplay,
        OriginalBlock
    }

    internal enum VisualRuleScope
    {
        Renderer,
        Object,
        ProjectileFamily,
        Group,
        Sprite,
        Texture,
        Material,
        Global
    }

    internal enum VisualRuleDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    internal sealed class VisualOverrideRule
    {
        internal string Id = "";
        internal bool Enabled = true;
        internal VisualRuleCategory RuleCategory;
        internal VisualRuleChannel ChannelMask = VisualRuleChannel.None;
        internal VisualRuleScope Scope;
        internal int Priority;
        internal long StableOrder;
        internal int IdentityVersion = RendererIdentity.CurrentIdentityVersion;
        internal string SceneName = "";
        internal string ObjectPath = "";
        internal string HierarchyDiscriminator = "";
        internal string RendererType = "";
        internal int RendererComponentIndex = -1;
        internal int MaterialIndex = -1;
        internal string TexturePropertyName = "";
        internal string SpriteName = "";
        internal string TextureName = "";
        internal int TextureWidth = -1;
        internal int TextureHeight = -1;
        internal string MaterialName = "";
        internal string ProjectileFamily = "";
        internal string ColorMode = "";
        internal string Color = "";
        internal float Intensity = 1f;
        internal bool PreserveAlpha = true;
        internal string ReplacementRelativePath = "";
        internal string PlacementMode = "";
        internal float OffsetX;
        internal float OffsetY;
        internal float ScaleX = 1f;
        internal float ScaleY = 1f;
        internal float Rotation;
        internal float AnchorX = 0.5f;
        internal float AnchorY = 0.5f;
        internal bool ClipToOriginalBounds;
        internal string AnimationMode = "";
        internal string FrameIdentity = "";
        internal bool IncludePreserved;
        internal string CreatedVersion = "";
        internal string Comment = "";
        internal string DisplayName = "";
        internal readonly SortedDictionary<string, string> ExtraFields =
            new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal VisualRuleChannel GetCategoryChannel()
        {
            switch (RuleCategory)
            {
                case VisualRuleCategory.Color:
                    return VisualRuleChannel.Color;
                case VisualRuleCategory.Replacement:
                    return VisualRuleChannel.Replacement;
                case VisualRuleCategory.StaticDisplay:
                    return VisualRuleChannel.StaticDisplay;
                case VisualRuleCategory.OriginalBlock:
                    return ChannelMask;
                default:
                    return VisualRuleChannel.None;
            }
        }
    }

    internal sealed class VisualRuleDocument
    {
        internal const int SupportedFormatVersion = 1;

        internal int FormatVersion = SupportedFormatVersion;
        internal readonly List<VisualOverrideRule> Rules = new List<VisualOverrideRule>();
        internal readonly List<string> HeaderComments = new List<string>();
    }

    internal sealed class VisualRuleDiagnostic
    {
        internal int LineNumber;
        internal VisualRuleDiagnosticSeverity Severity;
        internal string Message = "";

        public override string ToString()
        {
            string prefix = LineNumber > 0 ? "Line " + LineNumber + ": " : "";
            return Severity + ": " + prefix + Message;
        }
    }

    internal sealed class VisualRuleParseResult
    {
        internal VisualRuleDocument Document = new VisualRuleDocument();
        internal readonly List<VisualRuleDiagnostic> Diagnostics = new List<VisualRuleDiagnostic>();

        internal bool Success
        {
            get
            {
                for (int i = 0; i < Diagnostics.Count; i++)
                {
                    if (Diagnostics[i].Severity == VisualRuleDiagnosticSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
