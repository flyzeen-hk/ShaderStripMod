using System;
using System.Collections.Generic;

namespace ShaderStripMod
{
    // Reads artwork directly through the mesh UVs. No preview shader, camera or GL state.
    internal static class SpriteMaskPreview
    {
        internal struct Pixel
        {
            internal byte R,G,B,A;
            internal Pixel(byte r,byte g,byte b,byte a) { R=r;G=g;B=b;A=a; }
        }
        internal static Pixel[] Rasterize(SpriteMaskGeometry.Vertex[] vertices, int[] triangles,
            Pixel[] atlas, int atlasWidth, int atlasHeight, int width, int height,
            double left,double bottom,double boundsWidth,double boundsHeight)
        {
            if (vertices==null||triangles==null||triangles.Length%3!=0||atlasWidth<=0||atlasHeight<=0||
                atlas==null||atlas.Length!=(long)atlasWidth*atlasHeight||width<=0||height<=0||width>2048||height>2048||
                !SpriteMaskGeometry.Finite(boundsWidth)||!SpriteMaskGeometry.Finite(boundsHeight)||boundsWidth<=0||boundsHeight<=0)
                throw new ArgumentException("Invalid preview data.");
            var result=new Pixel[width*height];long visited=0;
            for(int i=0;i<triangles.Length;i+=3)
            {
                for(int j=0;j<3;j++) if(triangles[i+j]<0||triangles[i+j]>=vertices.Length) throw new ArgumentException("Invalid preview triangle.");
                var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
                foreach(var v in new[]{a,b,c}) if(!SpriteMaskGeometry.Finite(v.X)||!SpriteMaskGeometry.Finite(v.Y)||!SpriteMaskGeometry.Finite(v.U)||!SpriteMaskGeometry.Finite(v.V)) throw new ArgumentException("Invalid preview vertex.");
                double area=SpriteMaskGeometry.Cross(a.Position,b.Position,c.Position);
                if(Math.Abs(area)<1e-12) continue;
                int x0=Clamp((int)Math.Floor((Math.Min(a.X,Math.Min(b.X,c.X))-left)/boundsWidth*width),0,width-1);
                int x1=Clamp((int)Math.Ceiling((Math.Max(a.X,Math.Max(b.X,c.X))-left)/boundsWidth*width),0,width-1);
                int y0=Clamp((int)Math.Floor((Math.Min(a.Y,Math.Min(b.Y,c.Y))-bottom)/boundsHeight*height),0,height-1);
                int y1=Clamp((int)Math.Ceiling((Math.Max(a.Y,Math.Max(b.Y,c.Y))-bottom)/boundsHeight*height),0,height-1);
                visited+=(long)(x1-x0+1)*(y1-y0+1);
                if(visited>32000000) throw new ArgumentException("Preview geometry is too complex.");
                for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++)
                {
                    var p=new SpriteMaskGeometry.Point(left+(x+.5)*boundsWidth/width,bottom+(y+.5)*boundsHeight/height);
                    double wa=SpriteMaskGeometry.Cross(b.Position,c.Position,p)/area;
                    double wb=SpriteMaskGeometry.Cross(c.Position,a.Position,p)/area,wc=1-wa-wb;
                    if(wa<-1e-8||wb<-1e-8||wc<-1e-8) continue;
                    result[y*width+x]=Sample(atlas,atlasWidth,atlasHeight,wa*a.U+wb*b.U+wc*c.U,wa*a.V+wb*b.V+wc*c.V);
                }
            }
            return result;
        }
        static int Clamp(int n,int low,int high) { return Math.Max(low,Math.Min(high,n)); }
        static byte Mix(byte a,byte b,byte c,byte d,double x,double y)
        { return (byte)Math.Max(0,Math.Min(255,Math.Round((a*(1-x)+b*x)*(1-y)+(c*(1-x)+d*x)*y))); }
        static Pixel Sample(Pixel[] atlas,int width,int height,double u,double v)
        {
            double px=Math.Max(0,Math.Min(width-1,u*width-.5)),py=Math.Max(0,Math.Min(height-1,v*height-.5));
            int x=(int)Math.Floor(px),y=(int)Math.Floor(py),x1=Math.Min(width-1,x+1),y1=Math.Min(height-1,y+1);
            double fx=px-x,fy=py-y;
            var a=atlas[y*width+x];var b=atlas[y*width+x1];var c=atlas[y1*width+x];var d=atlas[y1*width+x1];
            return new Pixel(Mix(a.R,b.R,c.R,d.R,fx,fy),Mix(a.G,b.G,c.G,d.G,fx,fy),Mix(a.B,b.B,c.B,d.B,fx,fy),Mix(a.A,b.A,c.A,d.A,fx,fy));
        }
        internal static Pixel[] ApplySelection(Pixel[] source,int width,int height,IList<SpriteMaskGeometry.Point> contour)
        {
            if(source==null||source.Length!=(long)width*height||width<=0||height<=0) throw new ArgumentException("Invalid preview image.");
            SpriteMaskGeometry.Triangulate(contour);
            var result=(Pixel[])source.Clone();
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                double px=(x+.5)/width,py=(y+.5)/height;bool inside=false;
                for(int i=0,j=contour.Count-1;i<contour.Count;j=i++)
                {
                    var a=contour[i];var b=contour[j];
                    if((a.Y>py)!=(b.Y>py) && px<(b.X-a.X)*(py-a.Y)/(b.Y-a.Y)+a.X) inside=!inside;
                }
                if(!inside) result[y*width+x].A=0;
            }
            return result;
        }
    }
    // Tracks only a renderer's visual suppression, without touching gameplay layers or enabled.
    internal sealed class RendererVisibilityOverride<T> where T:class
    {
        readonly Dictionary<T,bool> original=new Dictionary<T,bool>();
        internal bool Hold(T renderer, Func<T,bool> read, Action<T,bool> write)
        {
            bool state;
            if(!original.TryGetValue(renderer,out state)) { state=read(renderer);original.Add(renderer,state); }
            if(!read(renderer)) write(renderer,true);
            return !state;
        }
        internal void Reassert(Func<T,bool> alive,Action<T,bool> write)
        { foreach(var renderer in original.Keys) if(alive(renderer)) write(renderer,true); }
        internal void ReleaseAll(Func<T,bool> alive,Action<T,bool> write)
        { foreach(var pair in original) if(alive(pair.Key)) write(pair.Key,pair.Value);original.Clear(); }
    }
}

