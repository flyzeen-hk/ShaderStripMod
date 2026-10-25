using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        static SpriteMaskGeometry.Point P(double x,double y) { return new SpriteMaskGeometry.Point(x,y); }
        static List<SpriteMaskGeometry.Point> Rectangle(double left,double bottom,double right,double top)
        { return new List<SpriteMaskGeometry.Point>{P(left,bottom),P(right,bottom),P(right,top),P(left,top)}; }
        static double ClippedArea(List<SpriteMaskGeometry.Vertex> vertices)
        {
            double area=0;
            for (int i=0;i<vertices.Count;i+=3) area+=Math.Abs(SpriteMaskGeometry.Cross(vertices[i].Position,vertices[i+1].Position,vertices[i+2].Position))/2;
            return area;
        }
        static double PolygonArea(IList<SpriteMaskGeometry.Point> points)
        {
            double area=0;
            for (int i=0;i<points.Count;i++) area+=points[i].X*points[(i+1)%points.Count].Y-points[(i+1)%points.Count].X*points[i].Y;
            return Math.Abs(area)/2;
        }
        static bool RejectMask(Action operation)
        { try { operation();return false; } catch(ArgumentException) { return true; } catch(FormatException) { return true; } }
        static void RunMaskChecks()
        {
            var source=new[]{new SpriteMaskGeometry.Vertex(0,0,0,.2,.3,0,0,1,.25),
                new SpriteMaskGeometry.Vertex(1,0,0,.7,.3,1,0,1,.75),
                new SpriteMaskGeometry.Vertex(1,1,0,.7,.9,1,1,1,.75),
                new SpriteMaskGeometry.Vertex(0,1,0,.2,.9,0,1,1,.25)};
            var triangles=new[]{0,1,2,0,2,3};
            var crop=SpriteMaskGeometry.Clip(source,triangles,Rectangle(.2,.3,.8,.7));
            Check(Math.Abs(ClippedArea(crop)-.24)<1e-8,"Rectangle has exact visible area, no overlapping triangles");
            foreach(var v in crop)
            {
                Check(v.X>=.2-1e-8&&v.X<=.8+1e-8&&v.Y>=.3-1e-8&&v.Y<=.7+1e-8,"Crop stays within selection");
                Check(Math.Abs(v.U-(.2+.5*v.X))<1e-8&&Math.Abs(v.V-(.3+.6*v.Y))<1e-8,"Atlas UV interpolates correctly");
                Check(Math.Abs(v.R-v.X)<1e-8&&Math.Abs(v.G-v.Y)<1e-8&&Math.Abs(v.A-(.25+.5*v.X))<1e-8,"Vertex colour and alpha interpolate correctly");
            }
            Check(Math.Abs(ClippedArea(SpriteMaskGeometry.Clip(source,triangles,Rectangle(0,0,1,1)))-1)<1e-8,"Full mask covers source exactly once");
            Check(SpriteMaskGeometry.Clip(source,triangles,Rectangle(2,2,3,3)).Count==0,"Disjoint mask emits no geometry");
            var concave=new List<SpriteMaskGeometry.Point>{P(.1,.1),P(.9,.1),P(.9,.4),P(.4,.4),P(.4,.9),P(.1,.9)};
            foreach(bool reverse in new[]{false,true})
            {
                var points=new List<SpriteMaskGeometry.Point>(concave);if(reverse) points.Reverse();
                var output=SpriteMaskGeometry.Clip(source,triangles,points);
                Check(Math.Abs(ClippedArea(output)-PolygonArea(points))<1e-8,"Concave contour in either winding has correct area");
                foreach(var v in output) Check(!(v.X>.400001&&v.Y>.400001),"Concave cutout excludes unwanted artwork");
            }
            var clockwise=new[]{2,1,0,3,2,0};
            var reversed=SpriteMaskGeometry.Clip(source,clockwise,Rectangle(.2,.3,.8,.7));
            Check(Math.Abs(ClippedArea(reversed)-.24)<1e-8,"Clockwise source is clipped correctly");
            for(int i=0;i<reversed.Count;i+=3) Check(SpriteMaskGeometry.Cross(reversed[i].Position,reversed[i+1].Position,reversed[i+2].Position)<0,"Source winding is preserved for culling");
            Check(RejectMask(()=>SpriteMaskGeometry.Triangulate(new[]{P(0,0),P(1,1),P(0,1),P(1,0)})),"Reject crossed contour");
            Check(RejectMask(()=>SpriteMaskGeometry.Triangulate(new[]{P(0,0),P(0,0),P(1,1)})),"Reject duplicate points");
            Check(RejectMask(()=>SpriteMaskGeometry.Triangulate(new[]{P(0,0),P(.5,0),P(1,0)})),"Reject zero-area contour");
            Check(RejectMask(()=>SpriteMaskGeometry.Triangulate(new[]{P(double.NaN,0),P(1,0),P(0,1)})),"Reject nonfinite contour");
            Check(RejectMask(()=>SpriteMaskGeometry.Clip(source,new[]{0,1,99},Rectangle(0,0,1,1))),"Reject bad triangle indices");
            var rng=new Random(29421);
            for(int n=0;n<150;n++)
            {
                double x=rng.NextDouble()*.6,y=rng.NextDouble()*.6,w=.05+rng.NextDouble()*(.9-x),h=.05+rng.NextDouble()*(.9-y);
                var r=Rectangle(x,y,x+w,y+h);
                Check(Math.Abs(ClippedArea(SpriteMaskGeometry.Clip(source,triangles,r))-w*h)<1e-7,"Random rectangle conserves selected area");
                int count=3+rng.Next(20);var polygon=new List<SpriteMaskGeometry.Point>();
                for(int i=0;i<count;i++) { double angle=2*Math.PI*i/count, radius=.15+.3*rng.NextDouble();polygon.Add(P(.5+radius*Math.Cos(angle),.5+radius*Math.Sin(angle))); }
                Check(Math.Abs(ClippedArea(SpriteMaskGeometry.Clip(source,triangles,polygon))-PolygonArea(polygon))<1e-7,"Random concave polygon conserves area without overlap");
            }
            var record=new SpriteMaskRecord { Identity="Town\nСкамейка/деталь[1]\nMeshRenderer#0",Label="Комната / Скамейка",Fingerprint=new string('A',64),Enabled=true };
            record.Points.AddRange(Rectangle(.1,.2,.8,.9));
            string encoded=record.Serialize();var decoded=SpriteMaskRecord.Parse(encoded.Replace("\n","\r\n"));
            Check(decoded.Identity==record.Identity&&decoded.Label==record.Label&&decoded.Enabled,"Mask record preserves Unicode identity and enabled state");
            Check(decoded.Points.Count==4&&Math.Abs(decoded.Points[1].X-.8)<1e-12,"Mask record preserves point precision");
            Check(RejectMask(()=>SpriteMaskRecord.Parse(encoded.Replace(new string('A',64),"invalid"))),"Reject corrupt source fingerprint");
            Check(RejectMask(()=>SpriteMaskRecord.Parse(encoded.Replace("0.1,0.2","NaN,0.2"))),"Reject corrupt point");
            Check(RejectMask(()=>SpriteMaskRecord.Parse(encoded.Replace("ShaderStripMod.SpriteMask.v1","OtherFormat"))),"Reject unknown format");
            string scratch=Path.Combine(Path.GetTempPath(),"ShaderStripModMaskChecks_"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(scratch,"test.mask");
            try
            {
                SpriteMaskRecord.WriteAtomic(path,encoded);
                Check(SpriteMaskRecord.Parse(File.ReadAllText(path)).Enabled,"Mask is readable after initial write");
                record.Enabled=false;SpriteMaskRecord.WriteAtomic(path,record.Serialize());
                Check(!SpriteMaskRecord.Parse(File.ReadAllText(path)).Enabled,"Atomic replace persists disabled state");
                Check(SpriteMaskRecord.Parse(File.ReadAllText(path+".bak")).Enabled,"Atomic replace retains previous mask backup");
                Check(Directory.GetFiles(scratch,"*.tmp").Length==0,"No temporary file remains");
            }
            finally
            {
                foreach(string file in new[]{path,path+".bak"}) if(File.Exists(file)) File.Delete(file);
                if(Directory.Exists(scratch)) Directory.Delete(scratch,false);
            }
        }
    }
}
