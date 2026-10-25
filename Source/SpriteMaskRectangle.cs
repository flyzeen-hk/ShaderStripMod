using System;
using System.Collections.Generic;
namespace ShaderStripMod
{
    internal static class SpriteMaskRectangle
    {
        internal struct Box
        {
            internal double X,Y,Width,Height;
            internal Box(double x,double y,double width,double height) { X=x;Y=y;Width=width;Height=height; }
        }
        private static double Clamp(double value)
        {
            if(double.IsNaN(value)||double.IsInfinity(value)) throw new ArgumentException("Rectangle coordinate must be finite.");
            return Math.Max(0,Math.Min(1,value));
        }
        internal static SpriteMaskGeometry.Point[] FromCorners(double x1,double y1,double x2,double y2)
        {
            x1=Clamp(x1);y1=Clamp(y1);x2=Clamp(x2);y2=Clamp(y2);
            double left=Math.Min(x1,x2),right=Math.Max(x1,x2),bottom=Math.Min(y1,y2),top=Math.Max(y1,y2);
            return new[]{new SpriteMaskGeometry.Point(left,bottom),new SpriteMaskGeometry.Point(right,bottom),new SpriteMaskGeometry.Point(right,top),new SpriteMaskGeometry.Point(left,top)};
        }
        internal static Box GetBounds(IList<SpriteMaskGeometry.Point> points)
        {
            if(points==null||points.Count==0) return new Box();
            double left=1,right=0,bottom=1,top=0;
            foreach(var p in points) { double x=Clamp(p.X),y=Clamp(p.Y);left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y); }
            return new Box(left,bottom,right-left,top-bottom);
        }
        internal static SpriteMaskGeometry.Point[] FromBounds(IList<SpriteMaskGeometry.Point> points)
        {
            var b=GetBounds(points);return FromCorners(b.X,b.Y,b.X+b.Width,b.Y+b.Height);
        }
        internal static bool HasArea(IList<SpriteMaskGeometry.Point> points)
        {
            if(points==null||points.Count!=4) return false;
            var b=GetBounds(points);return b.Width>0.00001&&b.Height>0.00001&&b.Width*b.Height>1e-9;
        }
        // All four strips lie inside the selection, including selections touching the image edges.
        internal static Box[] Frame(double x,double y,double width,double height,double thickness)
        {
            width=Math.Max(0,width);height=Math.Max(0,height);
            double tx=Math.Min(Math.Max(0,thickness),width),ty=Math.Min(Math.Max(0,thickness),height);
            return new[]{new Box(x,y,width,ty),new Box(x,y+height-ty,width,ty),new Box(x,y,tx,height),new Box(x+width-tx,y,tx,height)};
        }
    }
}
