using System;
namespace ShaderStripMod
{
    internal static class MenuViewport
    {
        // ForceCameraAspect uses 16:9. At 1360x768 it leaves 1.5 pixels above and below.
        // Snap only rounding-sized gaps; preserve deliberate letterboxing/split viewports.
        internal static bool HasRoundingGap(float x, float y, float width, float height, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || width <= 0 || height <= 0) return false;
            return Math.Abs(x * screenWidth) <= 2.01f && Math.Abs(y * screenHeight) <= 2.01f &&
                Math.Abs((1 - x - width) * screenWidth) <= 2.01f && Math.Abs((1 - y - height) * screenHeight) <= 2.01f;
        }
    }
}