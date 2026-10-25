using System;
namespace ShaderStripMod
{
    internal static class RuleControlsLayout
    {
        internal struct Cell { internal float X,Y,Width,Height; }
        internal static int Columns(float width,int count)
        {return Math.Max(1,Math.Min(count,(int)((Math.Max(1,width)+6)/142)));}
        internal static float Height(float width,int count)
        {return count<=0 ? 0 : ((count+Columns(width,count)-1)/Columns(width,count))*44-6;}
        internal static Cell Button(float width,int count,int index)
        {
            int columns=Columns(width,count);
            float cellWidth=Math.Max(1,(width-6*(columns-1))/columns);
            return new Cell{X=(index%columns)*(cellWidth+6),Y=(index/columns)*44,Width=cellWidth,Height=38};
        }
    }
}
