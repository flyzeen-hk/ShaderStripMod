using System;
namespace ShaderStripMod
{
    internal static class BackgroundImageGeometry
    {
        internal struct Size { internal float Width, Height; }
        internal static Size Calculate(float imageWidth, float imageHeight, float screenWidth, float screenHeight, string layout)
        {
            if (imageWidth <= 0 || imageHeight <= 0 || screenWidth <= 0 || screenHeight <= 0)
                throw new ArgumentOutOfRangeException("dimensions");
            if (string.Equals(layout, "Stretch", StringComparison.OrdinalIgnoreCase)) return new Size { Width = 1, Height = 1 };
            float scale = string.Equals(layout, "Contain", StringComparison.OrdinalIgnoreCase)
                ? Math.Min(screenWidth / imageWidth, screenHeight / imageHeight)
                : Math.Max(screenWidth / imageWidth, screenHeight / imageHeight);
            return new Size { Width = imageWidth * scale / screenWidth, Height = imageHeight * scale / screenHeight };
        }
    }
}
