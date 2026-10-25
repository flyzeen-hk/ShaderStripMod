using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
namespace ShaderStripMod
{
    // GIF89a: bounded LZW decoding, local palettes, interlace, transparency and disposal 2/3.
    // Kept independent of Unity so malformed input and composition can be regression tested.
    internal sealed class GifAnimation
    {
        internal sealed class Frame
        {
            internal int X,Y,Width,Height,Delay,Disposal,Transparent=-1;
            internal uint[] Palette;
            internal byte[] Indices;
        }
        internal readonly int Width,Height;
        internal readonly Frame[] Frames;
        internal readonly double Duration;
        internal readonly uint Background;
        private GifAnimation(int width,int height,List<Frame> frames,uint background)
        {
            Width=width;Height=height;Frames=frames.ToArray();Background=background;
            foreach(var f in frames)Duration+=f.Delay/1000.0;
        }
        internal static GifAnimation Decode(byte[] data,CancellationToken cancellation=default(CancellationToken))
        {
            cancellation.ThrowIfCancellationRequested();
            if(data==null||data.Length>64*1024*1024)throw new InvalidDataException("GIF file exceeds 64 MiB.");
            using(var stream=new MemoryStream(data,false))using(var r=new BinaryReader(stream))
            {
                string signature=Encoding.ASCII.GetString(Read(r,6));
                if(signature!="GIF87a"&&signature!="GIF89a")throw new InvalidDataException("Invalid GIF signature.");
                int width=r.ReadUInt16(),height=r.ReadUInt16();
                if(width<1||height<1||width>4096||height>4096||(long)width*height>8388608)throw new InvalidDataException("GIF dimensions exceed 4096 or 8 million pixels.");
                int packed=r.ReadByte(),backgroundIndex=r.ReadByte();r.ReadByte();
                uint[] global=(packed&128)!=0 ? Palette(r,1<<((packed&7)+1)) : null;
                uint background=global!=null&&backgroundIndex<global.Length ? global[backgroundIndex] : 0;
                int delay=100,disposal=0,transparent=-1;long decoded=(long)width*height*12;
                var frames=new List<Frame>();bool trailer=false;
                while(stream.Position<stream.Length)
                {
                    cancellation.ThrowIfCancellationRequested();
                    int block=r.ReadByte();
                    if(block==0x3b){trailer=true;break;}
                    if(block==0x21)
                    {
                        int label=r.ReadByte();
                        if(label==0xf9)
                        {
                            if(r.ReadByte()!=4)throw new InvalidDataException("Invalid GIF graphic control.");
                            int control=r.ReadByte();disposal=(control>>2)&7;
                            if(disposal>3)throw new InvalidDataException("Unsupported GIF disposal method.");
                            delay=Math.Max(20,(int)r.ReadUInt16()*10);int index=r.ReadByte();transparent=(control&1)!=0 ? index : -1;
                            if(r.ReadByte()!=0)throw new InvalidDataException("Invalid GIF control terminator.");
                        }
                        else
                        {
                            // Plain Text is a rendering block, not an image. Do not leak its graphic control to the next image.
                            SubBlocks(r,cancellation);if(label==1){delay=100;disposal=0;transparent=-1;}
                        }
                        continue;
                    }
                    if(block!=0x2c)throw new InvalidDataException("Unexpected GIF block.");
                    var frame=new Frame {X=r.ReadUInt16(),Y=r.ReadUInt16(),Width=r.ReadUInt16(),Height=r.ReadUInt16(),Delay=delay,Disposal=disposal,Transparent=transparent};
                    if(frame.Width==0||frame.Height==0||frame.X+frame.Width>width||frame.Y+frame.Height>height)throw new InvalidDataException("GIF frame extends outside its canvas.");
                    int imagePacked=r.ReadByte();frame.Palette=(imagePacked&128)!=0 ? Palette(r,1<<((imagePacked&7)+1)) : global;
                    if(frame.Palette==null)throw new InvalidDataException("GIF frame has no color table.");
                    int count=checked(frame.Width*frame.Height);decoded+=count+1024;
                    if(decoded>128L*1024*1024||frames.Count>=1024)throw new InvalidDataException("GIF decoded memory/frame limit reached; use a video for long animations.");
                    int minimum=r.ReadByte();var compressed=SubBlocks(r,cancellation);var pixels=Lzw(compressed,minimum,count,cancellation);
                    foreach(byte index in pixels)if(index>=frame.Palette.Length)throw new InvalidDataException("GIF color index is outside its palette.");
                    if((imagePacked&64)!=0)
                    {
                        var ordered=new byte[count];int source=0;
                        int[] starts={0,4,2,1},steps={8,8,4,2};
                        for(int pass=0;pass<4;pass++)for(int y=starts[pass];y<frame.Height;y+=steps[pass]){Array.Copy(pixels,source,ordered,y*frame.Width,frame.Width);source+=frame.Width;}
                        pixels=ordered;
                    }
                    frame.Indices=pixels;frames.Add(frame);delay=100;disposal=0;transparent=-1;
                }
                if(!trailer||frames.Count==0)throw new InvalidDataException("GIF has no complete image sequence.");
                if(frames[0].Transparent>=0)background=0;
                return new GifAnimation(width,height,frames,background);
            }
        }
        private static uint[] Palette(BinaryReader r,int count)
        {
            var colors=new uint[count];for(int i=0;i<count;i++){uint red=r.ReadByte(),green=r.ReadByte(),blue=r.ReadByte();colors[i]=red|(green<<8)|(blue<<16)|0xff000000;}return colors;
        }
        private static byte[] Read(BinaryReader r,int count)
        {var bytes=r.ReadBytes(count);if(bytes.Length!=count)throw new EndOfStreamException("Truncated GIF.");return bytes;}
        private static byte[] SubBlocks(BinaryReader r,CancellationToken cancellation)
        {
            using(var data=new MemoryStream())
            {int n;while((n=r.ReadByte())!=0){cancellation.ThrowIfCancellationRequested();if(data.Length+n>64*1024*1024)throw new InvalidDataException("GIF data limit.");var b=Read(r,n);data.Write(b,0,b.Length);}return data.ToArray();}
        }
        private static byte[] Lzw(byte[] compressed,int minimum,int expected,CancellationToken cancellation)
        {
            if(minimum<2||minimum>8)throw new InvalidDataException("Invalid GIF LZW code size.");
            int clear=1<<minimum,end=clear+1,next=end+1,size=minimum+1,previous=-1,bit=0,output=0,first=0;
            var prefix=new short[4096];var suffix=new byte[4096];var stack=new byte[4097];var pixels=new byte[expected];bool finished=false;
            for(int i=0;i<clear;i++)suffix[i]=(byte)i;
            while(bit+size<=compressed.Length*8)
            {
                if((bit&4095)<size)cancellation.ThrowIfCancellationRequested();
                int code=0;for(int i=0;i<size;i++)code|=((compressed[(bit+i)>>3]>>((bit+i)&7))&1)<<i;bit+=size;
                if(code==clear){next=end+1;size=minimum+1;previous=-1;continue;}
                if(code==end){finished=true;break;}
                if(code>next||code>=4096)throw new InvalidDataException("Invalid GIF LZW dictionary reference.");
                int original=code,top=0;
                if(code==next){if(previous<0)throw new InvalidDataException("Missing GIF initial LZW symbol.");stack[top++]=(byte)first;code=previous;}
                while(code>=clear)
                {
                    if(code>=next||top>=4096)throw new InvalidDataException("Invalid GIF LZW chain.");
                    stack[top++]=suffix[code];code=prefix[code];
                }
                first=suffix[code];stack[top++]=(byte)first;
                if(output+top>expected)throw new InvalidDataException("GIF frame has excess pixels.");
                while(top>0)pixels[output++]=stack[--top];
                if(previous>=0&&next<4096)
                {
                    prefix[next]=(short)previous;suffix[next]=(byte)first;next++;
                    if(next==(1<<size)&&size<12)size++;
                }
                previous=original;
            }
            if(!finished||output!=expected)throw new InvalidDataException("Incomplete GIF LZW image.");
            return pixels;
        }
        internal sealed class Playback
        {
            private readonly GifAnimation animation;
            internal readonly uint[] Pixels;
            private uint[] restore;
            private int current=-1;
            private double elapsed;
            internal Playback(GifAnimation animation){this.animation=animation;Pixels=new uint[animation.Width*animation.Height];Advance(0);}
            internal bool Advance(double seconds)
            {
                if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)seconds=0;
                double next=(elapsed+seconds)%animation.Duration;bool wrapped=next<elapsed||seconds>=animation.Duration;elapsed=next;
                int target=0;double edge=animation.Frames[0].Delay/1000.0;
                while(target+1<animation.Frames.Length&&elapsed>=edge)edge+=animation.Frames[++target].Delay/1000.0;
                if(target==current&&!wrapped)return false;
                if(current<0||wrapped||target<current){for(int i=0;i<Pixels.Length;i++)Pixels[i]=animation.Background;current=-1;restore=null;}
                while(current<target)
                {
                    if(current>=0)DisposePrevious(animation.Frames[current]);
                    var frame=animation.Frames[++current];if(frame.Disposal==3)restore=(uint[])Pixels.Clone();else restore=null;
                    for(int y=0;y<frame.Height;y++)for(int x=0;x<frame.Width;x++)
                    {int index=frame.Indices[y*frame.Width+x];if(index!=frame.Transparent)Pixels[(frame.Y+y)*animation.Width+frame.X+x]=frame.Palette[index];}
                }
                return true;
            }
            private void DisposePrevious(Frame frame)
            {
                if(frame.Disposal==3&&restore!=null){Array.Copy(restore,Pixels,Pixels.Length);return;}
                if(frame.Disposal!=2)return;
                uint background=frame.Transparent>=0 ? 0 : animation.Background;
                for(int y=0;y<frame.Height;y++)for(int x=0;x<frame.Width;x++)Pixels[(frame.Y+y)*animation.Width+frame.X+x]=background;
            }
        }
    }
}
