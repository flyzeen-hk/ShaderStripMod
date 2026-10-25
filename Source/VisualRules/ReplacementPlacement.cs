using System;

namespace ShaderStripMod.VisualRules
{
    internal enum ReplacementPlacementMode
    {
        Fit,
        Fill,
        Stretch,
        Native,
        Free
    }

    internal sealed class ReplacementPlacementInput
    {
        internal ReplacementPlacementMode Mode = ReplacementPlacementMode.Fit;
        internal float TargetWidth = 1f;
        internal float TargetHeight = 1f;
        internal float ContentWidth = 1f;
        internal float ContentHeight = 1f;
        internal float ManualScaleX = 1f;
        internal float ManualScaleY = 1f;
        internal float OffsetX = 0f;
        internal float OffsetY = 0f;
        internal float Rotation = 0f;
        internal float AnchorX = 0.5f;
        internal float AnchorY = 0.5f;
        internal bool ClipToOriginalBounds = false;
    }

    internal sealed class ReplacementPlacementResult
    {
        internal float ScaleX;
        internal float ScaleY;
        internal float DisplayWidth;
        internal float DisplayHeight;
        internal float OffsetX;
        internal float OffsetY;
        internal float Rotation;
        internal float AnchorX;
        internal float AnchorY;
        internal bool ClipToOriginalBounds;
    }

    internal static class ReplacementPlacementCalculator
    {
        internal static ReplacementPlacementResult Calculate(ReplacementPlacementInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            RequirePositiveFinite(input.TargetWidth, nameof(input.TargetWidth));
            RequirePositiveFinite(input.TargetHeight, nameof(input.TargetHeight));
            RequirePositiveFinite(input.ContentWidth, nameof(input.ContentWidth));
            RequirePositiveFinite(input.ContentHeight, nameof(input.ContentHeight));
            RequireNonZeroFinite(input.ManualScaleX, nameof(input.ManualScaleX));
            RequireNonZeroFinite(input.ManualScaleY, nameof(input.ManualScaleY));
            RequireFinite(input.OffsetX, nameof(input.OffsetX));
            RequireFinite(input.OffsetY, nameof(input.OffsetY));
            RequireFinite(input.Rotation, nameof(input.Rotation));
            RequireFinite(input.AnchorX, nameof(input.AnchorX));
            RequireFinite(input.AnchorY, nameof(input.AnchorY));

            float baseScaleX;
            float baseScaleY;
            float fitX = input.TargetWidth / input.ContentWidth;
            float fitY = input.TargetHeight / input.ContentHeight;
            switch (input.Mode)
            {
                case ReplacementPlacementMode.Fit:
                    baseScaleX = baseScaleY = Math.Min(fitX, fitY);
                    break;
                case ReplacementPlacementMode.Fill:
                    baseScaleX = baseScaleY = Math.Max(fitX, fitY);
                    break;
                case ReplacementPlacementMode.Stretch:
                    baseScaleX = fitX;
                    baseScaleY = fitY;
                    break;
                case ReplacementPlacementMode.Native:
                case ReplacementPlacementMode.Free:
                    baseScaleX = 1f;
                    baseScaleY = 1f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(input.Mode), input.Mode, "Unknown placement mode.");
            }

            float scaleX = baseScaleX * input.ManualScaleX;
            float scaleY = baseScaleY * input.ManualScaleY;
            return new ReplacementPlacementResult
            {
                ScaleX = scaleX,
                ScaleY = scaleY,
                DisplayWidth = input.ContentWidth * Math.Abs(scaleX),
                DisplayHeight = input.ContentHeight * Math.Abs(scaleY),
                OffsetX = input.OffsetX,
                OffsetY = input.OffsetY,
                Rotation = input.Rotation,
                AnchorX = input.AnchorX,
                AnchorY = input.AnchorY,
                ClipToOriginalBounds = input.ClipToOriginalBounds
            };
        }

        internal static bool TryParseMode(string value, out ReplacementPlacementMode mode)
        {
            string normalized = RendererIdentityBuilder.NormalizeName(value)
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");
            if (string.Equals(normalized, "NativeSize", StringComparison.OrdinalIgnoreCase)) normalized = "Native";
            return Enum.TryParse(normalized, true, out mode);
        }

        private static void RequirePositiveFinite(float value, string name)
        {
            RequireFinite(value, name);
            if (value <= 0f) throw new ArgumentOutOfRangeException(name, "Value must be greater than zero.");
        }

        private static void RequireNonZeroFinite(float value, string name)
        {
            RequireFinite(value, name);
            if (value == 0f) throw new ArgumentOutOfRangeException(name, "Value must not be zero.");
        }

        private static void RequireFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name, "Value must be finite.");
            }
        }
    }
}
