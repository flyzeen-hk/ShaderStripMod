using System;
namespace ShaderStripMod
{
    internal static class BrowserListLayout
    {
        internal struct Region
        {
            internal float X,Y,Width,Height;
            internal Region(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
        }
        internal static Region Viewport(float x,float y,float width,float height)
        {return new Region(x+2,y+2,Math.Max(1,width-4),Math.Max(1,height-28));}
        internal static Region Footer(float x,float y,float width,float height)
        {return new Region(x+8,y+height-24,Math.Max(1,width-16),20);}
    }
}