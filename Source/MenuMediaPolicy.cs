using System;
using System.IO;
namespace ShaderStripMod
{
    internal static class MenuMediaPolicy
    {
        internal static bool IsImage(string extension)
        {return string.Equals(extension,".png",StringComparison.OrdinalIgnoreCase)||string.Equals(extension,".jpg",StringComparison.OrdinalIgnoreCase)||string.Equals(extension,".jpeg",StringComparison.OrdinalIgnoreCase);}
        internal static bool IsVideo(string extension)
        {return string.Equals(extension,".mp4",StringComparison.OrdinalIgnoreCase)||string.Equals(extension,".webm",StringComparison.OrdinalIgnoreCase)||string.Equals(extension,".mov",StringComparison.OrdinalIgnoreCase)||string.Equals(extension,".m4v",StringComparison.OrdinalIgnoreCase)||string.Equals(extension,".avi",StringComparison.OrdinalIgnoreCase);}
        internal static bool IsSupported(string extension){return IsImage(extension)||string.Equals(extension,".gif",StringComparison.OrdinalIgnoreCase)||IsVideo(extension);}
        internal static void ValidateImageDimensions(byte[] encoded,string extension)
        {
            int width,height;ReadImageDimensions(encoded,extension,out width,out height);
            ValidateDecodedImageDimensions(width,height);
        }
        internal static void ValidateDecodedImageDimensions(int width,int height)
        {
            if(width<1||height<1||width>8192||height>8192||(long)width*height>8388608)
                throw new IOException("Image dimensions exceed 8192 or 8 million pixels.");
        }
        internal static void ReadImageDimensions(byte[] encoded,string extension,out int width,out int height)
        {
            width=height=0;
            if(encoded==null)throw new IOException("Invalid PNG/JPG image dimensions.");
            if(string.Equals(extension,".png",StringComparison.OrdinalIgnoreCase))
            {
                if(encoded.Length<24||encoded[0]!=137||encoded[1]!=80||encoded[2]!=78||encoded[3]!=71||encoded[4]!=13||encoded[5]!=10||encoded[6]!=26||encoded[7]!=10||
                    encoded[12]!=73||encoded[13]!=72||encoded[14]!=68||encoded[15]!=82)throw new IOException("Invalid PNG/JPG image dimensions.");
                long w=((uint)encoded[16]<<24)|((uint)encoded[17]<<16)|((uint)encoded[18]<<8)|encoded[19];
                long h=((uint)encoded[20]<<24)|((uint)encoded[21]<<16)|((uint)encoded[22]<<8)|encoded[23];
                if(w>int.MaxValue||h>int.MaxValue)throw new IOException("Image dimensions exceed 8192 or 8 million pixels.");
                width=(int)w;height=(int)h;return;
            }
            if((string.Equals(extension,".jpg",StringComparison.OrdinalIgnoreCase)||string.Equals(extension,".jpeg",StringComparison.OrdinalIgnoreCase))&&encoded.Length>=4&&encoded[0]==255&&encoded[1]==216)
            {
                int position=2;
                while(position<encoded.Length)
                {
                    if(encoded[position++]!=255)break;
                    while(position<encoded.Length&&encoded[position]==255)position++;
                    if(position>=encoded.Length)break;int marker=encoded[position++];
                    if(marker==217||marker==218)break;
                    if(marker==216||marker==1||(marker>=208&&marker<=215))continue;
                    if(position+2>encoded.Length)break;
                    int length=(encoded[position]<<8)|encoded[position+1];position+=2;
                    if(length<2||position+length-2>encoded.Length)break;
                    bool startOfFrame=marker>=192&&marker<=207&&marker!=196&&marker!=200&&marker!=204;
                    if(startOfFrame)
                    {
                        if(length<8)break;
                        height=(encoded[position+1]<<8)|encoded[position+2];width=(encoded[position+3]<<8)|encoded[position+4];return;
                    }
                    position+=length-2;
                }
            }
            throw new IOException("Invalid PNG/JPG image dimensions.");
        }
        internal static string Resolve(string directory,string name)
        {
            if(string.IsNullOrWhiteSpace(name)||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name.IndexOfAny(new[]{'/', '\\', ':', '\r', '\n'})>=0)throw new IOException("Choose a file from the imported menu media list.");
            string root=Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
            string path=Path.GetFullPath(Path.Combine(root,name));if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("Media must stay inside menu-media.");return path;
        }
        internal static bool IsTitleScene(string scene)
        {return !string.IsNullOrEmpty(scene)&&scene.StartsWith("Menu",StringComparison.OrdinalIgnoreCase);}
    }
}
