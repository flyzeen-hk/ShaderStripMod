using System;
using System.IO;
using System.Text;

namespace ShaderStripMod
{
    internal static class AtomicTextFile
    {
        internal static void Write(string path,string text)
        {
            if(string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A file path is required.","path");
            if(text==null) throw new ArgumentNullException("text");
            // Encode before opening any file; a malformed value cannot truncate saved data.
            byte[] bytes=new UTF8Encoding(false,true).GetBytes(text);
            string fullPath=Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            string temporary=fullPath+".tmp."+Guid.NewGuid().ToString("N");
            try
            {
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    stream.Write(bytes,0,bytes.Length);
                    stream.Flush(true);
                }
                // Never delete the destination as a fallback: failed replacement keeps the old file.
                if(File.Exists(fullPath)) File.Replace(temporary,fullPath,fullPath+".bak");
                else File.Move(temporary,fullPath);
            }
            finally
            {
                try { if(File.Exists(temporary)) File.Delete(temporary); }
                catch { /* Keep the original write failure if temporary cleanup also fails. */ }
            }
        }
    }
}
