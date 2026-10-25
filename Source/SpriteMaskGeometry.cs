using System;
using System.Collections.Generic;

namespace ShaderStripMod
{
    // Pure geometry: coordinates are local to the source, never screen or atlas coordinates.
    internal static class SpriteMaskGeometry
    {
        internal struct Point
        {
            internal double X, Y;
            internal Point(double x, double y) { X = x; Y = y; }
        }
        internal struct Vertex
        {
            internal double X, Y, Z, U, V, R, G, B, A;
            internal Vertex(double x, double y, double z, double u, double v,
                double r = 1, double g = 1, double b = 1, double a = 1)
            { X=x; Y=y; Z=z; U=u; V=v; R=r; G=g; B=b; A=a; }
            internal Point Position { get { return new Point(X,Y); } }
        }
        const double Epsilon = 1e-9;
        internal static double Cross(Point a, Point b, Point c)
        { return (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X); }
        internal static bool Finite(double n) { return !double.IsNaN(n) && !double.IsInfinity(n); }
        internal static List<int> Triangulate(IList<Point> points)
        {
            if (points == null || points.Count < 3 || points.Count > 64)
                throw new ArgumentException("Use 3 to 64 contour points.");
            double area = 0;
            for (int i=0;i<points.Count;i++)
            {
                Point a=points[i], b=points[(i+1)%points.Count];
                if (!Finite(a.X) || !Finite(a.Y)) throw new ArgumentException("Invalid contour coordinate.");
                if (Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)<Epsilon)
                    throw new ArgumentException("Remove duplicate contour points.");
                if (Math.Abs(Cross(points[(i+points.Count-1)%points.Count],a,b)) < Epsilon)
                    throw new ArgumentException("Remove points on a straight contour edge.");
                area += a.X*b.Y-b.X*a.Y;
                for (int j=i+1;j<points.Count;j++)
                {
                    if (j==i+1 || (i==0 && j==points.Count-1)) continue;
                    if (SegmentsIntersect(a,b,points[j],points[(j+1)%points.Count]))
                        throw new ArgumentException("Contour edges must not cross or touch.");
                }
            }
            if (Math.Abs(area)<Epsilon) throw new ArgumentException("The selection is too small.");
            var remaining=new List<int>();
            for (int i=0;i<points.Count;i++) remaining.Add(area>0 ? i : points.Count-1-i);
            var result=new List<int>();
            while (remaining.Count>3)
            {
                bool found=false;
                for (int i=0;i<remaining.Count;i++)
                {
                    int a=remaining[(i+remaining.Count-1)%remaining.Count], b=remaining[i], c=remaining[(i+1)%remaining.Count];
                    if (Cross(points[a],points[b],points[c])<=Epsilon) continue;
                    bool occupied=false;
                    foreach (int p in remaining)
                    {
                        if (p==a || p==b || p==c) continue;
                        if (InsideTriangle(points[p],points[a],points[b],points[c])) { occupied=true; break; }
                    }
                    if (occupied) continue;
                    result.Add(a); result.Add(b); result.Add(c); remaining.RemoveAt(i); found=true; break;
                }
                if (!found) throw new ArgumentException("Cannot triangulate this contour.");
            }
            result.AddRange(remaining);
            return result;
        }
        static bool InsideTriangle(Point p, Point a, Point b, Point c)
        { return Cross(a,b,p)>=-Epsilon && Cross(b,c,p)>=-Epsilon && Cross(c,a,p)>=-Epsilon; }
        static bool OnSegment(Point a, Point b, Point p)
        {
            return Math.Abs(Cross(a,b,p))<=Epsilon && p.X>=Math.Min(a.X,b.X)-Epsilon &&
                p.X<=Math.Max(a.X,b.X)+Epsilon && p.Y>=Math.Min(a.Y,b.Y)-Epsilon && p.Y<=Math.Max(a.Y,b.Y)+Epsilon;
        }
        static bool SegmentsIntersect(Point a, Point b, Point c, Point d)
        {
            double abC=Cross(a,b,c), abD=Cross(a,b,d), cdA=Cross(c,d,a), cdB=Cross(c,d,b);
            return ((abC>Epsilon && abD<-Epsilon || abC<-Epsilon && abD>Epsilon) &&
                    (cdA>Epsilon && cdB<-Epsilon || cdA<-Epsilon && cdB>Epsilon)) ||
                OnSegment(a,b,c) || OnSegment(a,b,d) || OnSegment(c,d,a) || OnSegment(c,d,b);
        }
        internal static List<Vertex> Clip(Vertex[] source, int[] triangles, IList<Point> contour)
        {
            if (source==null || triangles==null || triangles.Length%3!=0 || source.Length>4096)
                throw new ArgumentException("Unsupported source geometry.");
            foreach (Vertex v in source)
                if (!Finite(v.X)||!Finite(v.Y)||!Finite(v.Z)||!Finite(v.U)||!Finite(v.V)||
                    !Finite(v.R)||!Finite(v.G)||!Finite(v.B)||!Finite(v.A))
                    throw new ArgumentException("Invalid source vertex.");
            var maskTriangles=Triangulate(contour);
            var result=new List<Vertex>();
            for (int i=0;i<triangles.Length;i+=3)
            {
                for (int j=0;j<3;j++)
                    if (triangles[i+j]<0 || triangles[i+j]>=source.Length) throw new ArgumentException("Invalid source triangle.");
                for (int m=0;m<maskTriangles.Count;m+=3)
                {
                    var polygon=new List<Vertex>{source[triangles[i]],source[triangles[i+1]],source[triangles[i+2]]};
                    for (int edge=0;edge<3 && polygon.Count>0;edge++)
                        polygon=ClipEdge(polygon,contour[maskTriangles[m+edge]],contour[maskTriangles[m+(edge+1)%3]]);
                    for (int t=1;t+1<polygon.Count;t++)
                    {
                        if (Math.Abs(Cross(polygon[0].Position,polygon[t].Position,polygon[t+1].Position))<Epsilon) continue;
                        result.Add(polygon[0]); result.Add(polygon[t]); result.Add(polygon[t+1]);
                        if (result.Count>60000) throw new ArgumentException("Selection geometry is too complex.");
                    }
                }
            }
            return result;
        }
        static List<Vertex> ClipEdge(List<Vertex> input, Point a, Point b)
        {
            var output=new List<Vertex>();
            Vertex previous=input[input.Count-1]; double prevDistance=Cross(a,b,previous.Position);
            foreach (Vertex current in input)
            {
                double distance=Cross(a,b,current.Position);
                bool prevInside=prevDistance>=-Epsilon, inside=distance>=-Epsilon;
                if (prevInside!=inside)
                {
                    double amount=prevDistance/(prevDistance-distance);
                    output.Add(Lerp(previous,current,Math.Max(0,Math.Min(1,amount))));
                }
                if (inside) output.Add(current);
                previous=current; prevDistance=distance;
            }
            return output;
        }
        static Vertex Lerp(Vertex a, Vertex b, double t)
        {
            return new Vertex(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Z+(b.Z-a.Z)*t,
                a.U+(b.U-a.U)*t,a.V+(b.V-a.V)*t,a.R+(b.R-a.R)*t,a.G+(b.G-a.G)*t,a.B+(b.B-a.B)*t,a.A+(b.A-a.A)*t);
        }
    }
}
