using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ShaderStripMod
{
    internal sealed class SpriteMaskRecord
    {
        internal string Identity, Label, Fingerprint;
        internal bool Enabled;
        internal readonly List<SpriteMaskGeometry.Point> Points = new List<SpriteMaskGeometry.Point>();
        static string Encode(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? "")); }
        static string Decode(string value) { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        internal string Serialize()
        {
            Validate();
            var text = new StringBuilder("ShaderStripMod.SpriteMask.v1\n");
            text.AppendLine(Encode(Identity)); text.AppendLine(Encode(Label));
            text.AppendLine(Fingerprint); text.AppendLine(Enabled ? "1" : "0");
            foreach (var p in Points) text.AppendLine(p.X.ToString("R",CultureInfo.InvariantCulture)+","+p.Y.ToString("R",CultureInfo.InvariantCulture));
            return text.ToString();
        }
        internal static SpriteMaskRecord Parse(string text)
        {
            if (text==null || text.Length>32768) throw new FormatException("Mask file is too large.");
            var lines=text.Replace("\r","").TrimEnd('\n').Split('\n');
            if (lines.Length<8 || lines.Length>69 || lines[0]!="ShaderStripMod.SpriteMask.v1") throw new FormatException("Invalid mask format.");
            var result=new SpriteMaskRecord { Identity=Decode(lines[1]),Label=Decode(lines[2]),Fingerprint=lines[3],Enabled=lines[4]=="1" };
            if (lines[4]!="0" && lines[4]!="1") throw new FormatException("Invalid mask enabled flag.");
            for (int i=5;i<lines.Length;i++)
            {
                var xy=lines[i].Split(',');
                if (xy.Length!=2) throw new FormatException("Invalid mask point.");
                result.Points.Add(new SpriteMaskGeometry.Point(double.Parse(xy[0],CultureInfo.InvariantCulture),double.Parse(xy[1],CultureInfo.InvariantCulture)));
            }
            result.Validate(); return result;
        }
        internal void Validate()
        {
            if (string.IsNullOrEmpty(Identity)||Identity.Length>4096||Label==null||Label.Length>4096||Fingerprint==null||Fingerprint.Length!=64)
                throw new FormatException("Invalid mask identity.");
            foreach (char c in Fingerprint) if (!Uri.IsHexDigit(c)) throw new FormatException("Invalid mask fingerprint.");
            foreach (var p in Points)
                if (!SpriteMaskGeometry.Finite(p.X)||!SpriteMaskGeometry.Finite(p.Y)||p.X<0||p.X>1||p.Y<0||p.Y>1)
                    throw new FormatException("Mask points must be inside the sprite.");
            SpriteMaskGeometry.Triangulate(Points);
        }
        internal static void WriteAtomic(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllText(temporary,text,Encoding.UTF8);
                if (File.Exists(path)) File.Replace(temporary,path,path+".bak");
                else File.Move(temporary,path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
