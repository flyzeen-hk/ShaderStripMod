using System;
namespace ShaderStripMod
{
    internal static class MaskWorkspaceLayout
    {
        internal struct Region
        {
            internal float X, Y, Width, Height;
            internal Region(float x, float y, float width, float height) { X=x; Y=y; Width=width; Height=height; }
        }
        internal static Region Window(float width,float height,float scale)
        { return new Region(4/scale,4/scale,Math.Max(1,(width-8)/scale),Math.Max(1,(height-8)/scale)); }
        internal static Region Content(float windowWidth, float windowHeight)
        { return new Region(12, 64, Math.Max(1, windowWidth-24), Math.Max(1, windowHeight-80)); }
        internal static Region Canvas(float x, float y, float width, float bottom)
        { return new Region(x, y, Math.Max(1,width), Math.Max(1,bottom-y-30)); }
    }
}