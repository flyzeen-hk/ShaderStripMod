using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
namespace UnityEngine
{
    public enum HideFlags {HideAndDontSave}
    public enum TextureFormat {RGBA32}
    public enum TextureWrapMode {Clamp}
    public enum FilterMode {Bilinear}
    public enum SpriteMeshType {FullRect}
    public class Object
    {
        public string name;public HideFlags hideFlags;public bool destroyed;
        public static void Destroy(Object obj){if(obj!=null){obj.destroyed=true;if(obj is GameObject)foreach(var c in ((GameObject)obj).components)c.destroyed=true;}}
        public static void DontDestroyOnLoad(Object obj){}
    }
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
    public struct Bounds {public Vector3 size,center;}
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);}
    public static class Mathf
    {public static float Max(float a,float b)=>Math.Max(a,b);public static float Abs(float x)=>Math.Abs(x);public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));public static int RoundToInt(float f)=>(int)Math.Round(f);}
    public class Texture:Object {public int width,height;}
    public class Texture2D:Texture
    {
        public static int Created;
        public TextureWrapMode wrapMode;public FilterMode filterMode;public byte[] bytes;
        public Texture2D(int w,int h,TextureFormat f,bool mip){width=w;height=h;Created++;}
        public bool LoadImage(byte[] data,bool unused){if(data.Length<24)return false;ShaderStripMod.MenuMediaPolicy.ReadImageDimensions(data,".png",out width,out height);return true;}
        public void LoadRawTextureData(byte[] data){bytes=(byte[])data.Clone();}
        public void Apply(bool m,bool r){}
    }
    public class Sprite:Object
    {
        public Bounds bounds;public Texture texture;
        public static Sprite Create(Texture2D t,Rect rect,Vector2 pivot,float ppu,uint extrude=0,SpriteMeshType type=SpriteMeshType.FullRect){return new Sprite {texture=t,bounds=new Bounds {size=new Vector3(t.width/ppu,t.height/ppu,0),center=new Vector3((.5f-pivot.x)*t.width/ppu,(.5f-pivot.y)*t.height/ppu,0)}};}
    }
    public class Material:Object {public Texture mainTexture;public Material(Shader shader){}}
    public class Shader {public static Shader Find(string name)=>new Shader();}
    public class GameObject:Object
    {
        public Transform transform;public readonly List<Component> components=new List<Component>();
        public GameObject(string name){this.name=name;transform=new Transform {gameObject=this};}
        public T AddComponent<T>() where T:Component,new(){var c=new T {gameObject=this};components.Add(c);if(c is Video.VideoPlayer)Video.VideoPlayer.Last=(Video.VideoPlayer)(Component)c;return c;}
        public T GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault();
        public Component GetComponent(string name)=>components.FirstOrDefault(c=>c.GetType().Name==name);
    }
    public class Component:Object
    {
        public GameObject gameObject;public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
        public Component GetComponent(string n)=>gameObject.GetComponent(n);
        public T[] GetComponentsInChildren<T>(bool inactive) where T:Component
        {var list=new List<T>();Action<Transform> visit=null;visit=t=>{list.AddRange(t.gameObject.components.OfType<T>());foreach(var child in t.children)visit(child);};visit(transform);return list.ToArray();}
    }
    public class Transform:Component {public Transform parent;public readonly List<Transform> children=new List<Transform>();}
    public class CanvasGroup:Component {}
    public class SpriteRenderer:Component {public Sprite sprite;}
    public static class Time {public static float realtimeSinceStartup,unscaledTime;}
    public static class Application {public static string persistentDataPath=AppDomain.CurrentDomain.BaseDirectory;}
}
namespace UnityEngine.UI
{
    public class Selectable:UnityEngine.Component {}
    public class Text:UnityEngine.Component {public UnityEngine.Color color=UnityEngine.Color.white;public int fontSize=24;}
}
namespace UnityEngine.Video
{
    public enum VideoSource {Url}
    public enum VideoRenderMode {APIOnly}
    public enum VideoAudioOutputMode {None}
    public enum VideoTimeReference {Freerun}
    public class VideoPlayer:UnityEngine.Component
    {
        public static VideoPlayer Last;public bool playOnAwake,isLooping,waitForFirstFrame,skipOnDrop,canSetSkipOnDrop=true,isPrepared,isPlaying,stopErrors,stopThrows;
        public VideoSource source;public VideoRenderMode renderMode;public VideoAudioOutputMode audioOutputMode;public VideoTimeReference timeReference;
        public string url;public uint width=1920,height=1080;public int plays,pauses,stops;public UnityEngine.Texture texture;
        public event Action<VideoPlayer,string> errorReceived;public event Action<VideoPlayer> prepareCompleted;
        public void Prepare(){}
        public void Play(){isPlaying=true;plays++;}
        public void Pause(){isPlaying=false;pauses++;}
        public void Stop(){isPlaying=false;isPrepared=false;stops++;if(stopThrows)throw new InvalidOperationException("Native Stop failure");if(stopErrors)errorReceived?.Invoke(this,"Stop failure");}
        public void Ready(){isPrepared=true;texture=new UnityEngine.Texture();prepareCompleted?.Invoke(this);}
        public void Error(string value){errorReceived?.Invoke(this,value);}
    }
}
public class tk2dTextMesh:UnityEngine.Component
{public UnityEngine.Color color=UnityEngine.Color.white,color2=UnityEngine.Color.white;public bool useGradient,commitThrows;public UnityEngine.Vector3 scale=new UnityEngine.Vector3(1,1,1);public void Commit(){if(commitThrows)throw new InvalidOperationException("Commit failed");}}
public class UIManager {public static UIManager instance;public UnityEngine.CanvasGroup mainMenuScreen;public UnityEngine.SpriteRenderer gameTitle;}
public class MenuStyles {public static MenuStyles Instance;public MenuStyleTitle title;}
public class MenuStyleTitle {public UnityEngine.SpriteRenderer Title;}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        string settingsFilePath,testScene="Menu_Title";bool visualsActive=true,textureBrowserVisible,pauseGameWhileBrowserOpen=true;int saved,hidden;
        string GetActiveSceneName()=>testScene;
        void HideImageBackground(){hidden++;}
        // Decoration visibility is covered by MenuBackgroundDecorationsChecks; this harness exercises media/style ownership.
        void UpdateMenuBackgroundDecorations(){}
        void ReleaseMenuBackgroundDecorations(){}
        void SaveExportSettings(){saved++;}
        void ApplyStrip(){}
        string picker="";
        string ShowBackgroundImageFilePicker(string filter,string title,string directory)=>picker;
        static bool IsTmpText(Type t)=>t.FullName=="TMPro.TMP_Text";
        static object ReadMenuProperty(object source,string name)=>source?.GetType().GetProperty(name)?.GetValue(source,null);
        static int checks;
        static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
        static bool Near(float a,float b)=>Math.Abs(a-b)<.001;
        static void Tick(float time){UnityEngine.Time.realtimeSinceStartup=time;UnityEngine.Time.unscaledTime=time;}
        static T Attach<T>(string name,UnityEngine.Transform parent=null) where T:UnityEngine.Component,new()
        {var obj=new UnityEngine.GameObject(name);obj.transform.parent=parent;parent?.children.Add(obj.transform);return obj.AddComponent<T>();}
        static byte[] PngHeader(int width,int height)
        {
            var bytes=new byte[32];Array.Copy(new byte[]{137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82},bytes,16);
            for(int i=0;i<4;i++){bytes[16+i]=(byte)(width>>(24-i*8));bytes[20+i]=(byte)(height>>(24-i*8));}return bytes;
        }
        static void Reject(byte[] bytes,string name){bool failed=false;try{GifAnimation.Decode(bytes);}catch(Exception ex) when(ex is IOException||ex is InvalidDataException){failed=true;}Check(failed,name);}
        static void CheckPixels(uint[] actual,byte[] expected,string name)
        {
            Check(actual.Length*4==expected.Length,name+" size");
            for(int i=0;i<actual.Length;i++){uint p=(uint)(expected[i*4]|(expected[i*4+1]<<8)|(expected[i*4+2]<<16)|(expected[i*4+3]<<24));Check(actual[i]==p,name+" pixel "+i);}
        }
        static void GifChecks()
        {
            foreach(string path in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"GifFixtures"),"*.gif"))
            {
                var animation=GifAnimation.Decode(File.ReadAllBytes(path));var playback=new GifAnimation.Playback(animation);
                using(var r=new BinaryReader(File.OpenRead(Path.ChangeExtension(path,".rgba"))))
                {
                    Check(animation.Width==r.ReadInt32()&&animation.Height==r.ReadInt32()&&animation.Frames.Length==r.ReadInt32(),Path.GetFileName(path)+" dimensions");
                    byte[] first=null;double total=0;
                    for(int frame=0;frame<animation.Frames.Length;frame++)
                    {
                        int delay=r.ReadInt32();var pixels=r.ReadBytes(animation.Width*animation.Height*4);if(first==null)first=pixels;
                        Check(animation.Frames[frame].Delay==delay,"Frame delay");CheckPixels(playback.Pixels,pixels,Path.GetFileName(path)+" frame "+frame);
                        double step=delay/1000.0;total+=step;playback.Advance(step+.0000001);
                    }
                    CheckPixels(playback.Pixels,first,"Loop restores initial canvas");
                    Check(!playback.Advance(0)&&!playback.Advance(double.NaN)&&!playback.Advance(-1),"Zero/invalid delta keeps current frame");
                    playback.Advance(total*3);CheckPixels(playback.Pixels,first,"Long stall skips complete loops safely");
                }
            }
            byte[] valid=File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"GifFixtures","lzw-repeat.gif"));
            for(int n=0;n<valid.Length;n++)Reject(valid.Take(n).ToArray(),"Reject truncated GIF at "+n);
            var bad=(byte[])valid.Clone();bad[6]=0xff;bad[7]=0xff;Reject(bad,"Reject oversized GIF before allocation");
            bad=(byte[])valid.Clone();bad[0]=0;Reject(bad,"Reject invalid signature");
            using(var cancellation=new CancellationTokenSource())
            {
                cancellation.Cancel();bool cancelled=false;
                try{GifAnimation.Decode(valid,cancellation.Token);}catch(OperationCanceledException){cancelled=true;}
                Check(cancelled,"Detached GIF work observes cancellation before parsing/allocating");
            }
        }
        sealed class ValueTarget {internal float value;internal bool alive=true;}
        static void OverrideChecks()
        {
            var target=new ValueTarget {value=23};var overrides=new MenuValueOverride<ValueTarget,float>();
            for(int i=0;i<200;i++)overrides.Apply(target,c=>c.value,(c,v)=>c.value=(float)Math.Round(v),v=>v*1.15f);
            Check(target.value==26,"Rounded font does not compound on repeated updates");
            target.value=31;overrides.Apply(target,c=>c.value,(c,v)=>c.value=(float)Math.Round(v),v=>v*1.15f);
            Check(target.value==36,"Game font changes replace original baseline");
            overrides.Release(c=>c.alive,c=>c.value,(c,v)=>c.value=v);Check(target.value==31,"Restore latest game font");
            overrides.Apply(target,c=>c.value,(c,v)=>c.value=v,v=>v*.5f);target.value=80;
            overrides.Release(c=>c.alive,c=>c.value,(c,v)=>c.value=v);Check(target.value==80,"External edit is retained on disable");
            overrides.Release(c=>c.alive,c=>c.value,(c,v)=>c.value=v);Check(target.value==80,"Repeated release is harmless");
            var bad=new ValueTarget {value=10};var good=new ValueTarget {value=30};int reports=0;
            bool setterFailed=false;
            try{overrides.Apply(bad,c=>c.value,(c,v)=>{c.value=v;throw new InvalidOperationException("Setter");},v=>v*2);}catch(InvalidOperationException){setterFailed=true;}
            overrides.Apply(good,c=>c.value,(c,v)=>c.value=v,v=>v*2);
            Check(setterFailed&&bad.value==20,"Partially successful setter remembers its applied value");
            overrides.Release(c=>c.alive,c=>c.value,(c,v)=>{if(c==bad)throw new InvalidOperationException("Release");c.value=v;},ex=>reports++);
            Check(reports==1&&good.value==30&&bad.value==20,"One release failure does not prevent other targets restoring");
            overrides.Release(c=>c.alive,c=>c.value,(c,v)=>c.value=v);Check(bad.value==10,"Failed release is retained for a later retry");
        }
        static void PolicyChecks(string root)
        {
            Check(MenuMediaPolicy.IsSupported(".GIF")&&MenuMediaPolicy.IsVideo(".WebM")&&MenuMediaPolicy.IsImage(".JPEG"),"Case independent media formats");
            Check(!MenuMediaPolicy.IsSupported(null)&&!MenuMediaPolicy.IsSupported(".exe"),"Null and unsupported extension rejected");
            Check(MenuMediaPolicy.Resolve(root,"моё меню.gif")==Path.Combine(root,"моё меню.gif"),"Unicode filenames");
            foreach(string name in new[]{"..","../a.png","..\\a.png","C:\\a.png","a:b.png","a\n.png","",null})
            {bool failed=false;try{MenuMediaPolicy.Resolve(root,name);}catch(IOException){failed=true;}Check(failed,"Reject invalid media path "+name);}
            Check(MenuMediaPolicy.IsTitleScene("Menu_Title")&&!MenuMediaPolicy.IsTitleScene("Town")&&!MenuMediaPolicy.IsTitleScene(null),"Scope custom theme to menu scenes");
            foreach(string label in new[]{"Menu design","More options","Enable custom menu","As logo","Button text size","Menu GIF loaded."})
                Check(UiLocalization.Text(label,"Russian")!=label&&UiLocalization.Text(label,"English")==label,"Localized "+label);
            Check(UiLocalization.Text("Menu media error: Unsupported menu media format.","Russian")=="Ошибка файла меню: Неподдерживаемый формат файла меню.","Nested status localization");
            MenuMediaPolicy.ValidateImageDimensions(PngHeader(3840,2160),".PNG");Check(true,"4K image fits safe pixel limit");
            foreach(var dimensions in new[]{new[]{0,1},new[]{8193,1},new[]{4096,4096},new[]{int.MaxValue,2}})
            {
                bool failed=false;try{MenuMediaPolicy.ValidateImageDimensions(PngHeader(dimensions[0],dimensions[1]),".png");}catch(IOException){failed=true;}
                Check(failed,"PNG decoded dimensions rejected before Unity allocation");
            }
            byte[] jpeg={255,216,255,224,0,4,10,11,255,255,194,0,8,8,0,40,0,80,0};int width,height;
            MenuMediaPolicy.ReadImageDimensions(jpeg,".jpeg",out width,out height);Check(width==80&&height==40,"JPEG dimension reader skips metadata and accepts progressive SOF");
            for(int n=0;n<jpeg.Length-1;n++)
            {bool failed=false;try{MenuMediaPolicy.ValidateImageDimensions(jpeg.Take(n).ToArray(),".jpg");}catch(IOException){failed=true;}Check(failed,"Truncated JPEG dimension header rejected at "+n);}
            string oversized=Path.Combine(root,"oversized.png");File.WriteAllBytes(oversized,PngHeader(32767,32767));int creations=UnityEngine.Texture2D.Created;
            using(var rejected=new MenuMediaPlayback(oversized))Check(rejected.Failed&&rejected.Texture==null&&UnityEngine.Texture2D.Created==creations,"Oversized encoded image never reaches Unity Texture2D allocation/decoder");
        }
        static void VideoChecks(string root)
        {
            string path=Path.Combine(root,"video.mp4");File.WriteAllBytes(path,new byte[]{1});Tick(0);
            string unicode=Path.Combine(root,"видео меню.mp4");File.WriteAllBytes(unicode,new byte[]{1});
            using(var local=new MenuMediaPlayback(unicode))
            {var player=UnityEngine.Video.VideoPlayer.Last;Check(player.url==Path.GetFullPath(unicode)&&player.url.Contains("видео меню")&&!player.url.Contains("%D0"),"Native local video receives an absolute Windows path with unescaped Unicode and spaces");}
            string percent=Path.Combine(root,"literal%20name.mp4");File.WriteAllBytes(percent,new byte[]{1});
            using(var local=new MenuMediaPlayback(percent))Check(UnityEngine.Video.VideoPlayer.Last.url==percent,"Literal percent sequences in local filenames are not URL-decoded");
            var playback=new MenuMediaPlayback(path,true);var video=UnityEngine.Video.VideoPlayer.Last;video.Ready();
            Check(!video.isPlaying&&playback.Texture==video.texture,"Preparation while paused never starts video");
            Check(video.isLooping&&video.audioOutputMode==UnityEngine.Video.VideoAudioOutputMode.None&&video.renderMode==UnityEngine.Video.VideoRenderMode.APIOnly,"Looped silent video texture mode");
            Tick(1);playback.Update(false);Check(video.isPlaying,"Resume starts prepared video");Tick(2);playback.Update(true);Check(!video.isPlaying&&video.pauses==1,"F9/manual pause stops native player");
            video.stopErrors=true;video.Error("Codec");Check(playback.Failed&&playback.Texture==null&&video.stops==1,"Native error stops once, hides old texture, avoids recursive error");
            int calls=video.plays;playback.Update(false);Check(video.plays==calls,"Failed video never restarts automatically");
            playback.Dispose();playback.Dispose();video.Ready();Check(video.destroyed&&playback.Texture==null&&video.plays==calls,"Dispose detaches preparation callback and is idempotent");
            Tick(0);playback=new MenuMediaPlayback(path);video=UnityEngine.Video.VideoPlayer.Last;Tick(31);playback.Update(false);
            Check(playback.Failed&&playback.Message.Contains("timed out"),"Unprepared video reaches bounded timeout");playback.Dispose();
            Tick(0);playback=new MenuMediaPlayback(path);video=UnityEngine.Video.VideoPlayer.Last;video.width=9000;video.Ready();Check(playback.Failed,"Oversized native video rejected");playback.Dispose();
            Tick(0);playback=new MenuMediaPlayback(path);video=UnityEngine.Video.VideoPlayer.Last;video.Ready();video.stopThrows=true;var owner=video.gameObject;
            playback.Dispose();Check(video.destroyed&&owner.destroyed&&playback.Texture==null&&playback.Message.Contains("Native Stop failure"),"Native Stop exception cannot prevent owned player/GameObject cleanup");
            calls=video.plays;playback.Dispose();video.Ready();Check(video.stops==1&&video.plays==calls,"Cleanup after throwing Stop is idempotent and detaches native callbacks");
        }
        static void MenuChecks(string root)
        {
            string media=Path.Combine(root,"menu-media");Directory.CreateDirectory(media);
            File.WriteAllBytes(Path.Combine(media,"image.png"),PngHeader(100,50));File.WriteAllBytes(Path.Combine(media,"logo.png"),PngHeader(80,20));
            var canvas=Attach<UnityEngine.CanvasGroup>("MainMenu");var button=Attach<UnityEngine.UI.Selectable>("Start",canvas.transform);
            var text=Attach<UnityEngine.UI.Text>("ButtonText",button.transform);text.fontSize=150;text.color=new UnityEngine.Color(.6f,.6f,.6f,.25f);
            var tk=Attach<tk2dTextMesh>("TkButton",button.transform);tk.useGradient=true;tk.color2=new UnityEngine.Color(.4f,.4f,.4f,.3f);
            var unrelated=Attach<UnityEngine.UI.Text>("VersionLabel",canvas.transform);
            var logoRenderer=Attach<UnityEngine.SpriteRenderer>("LogoTitle");var original=new UnityEngine.Sprite {bounds=new UnityEngine.Bounds {size=new UnityEngine.Vector3(10,4,0),center=new UnityEngine.Vector3(1,.7f,0)}};logoRenderer.sprite=original;
            UIManager.instance=new UIManager {gameTitle=logoRenderer,mainMenuScreen=canvas};
            Tick(0);var mod=new ShaderStripMod {settingsFilePath=Path.Combine(root,"settings.txt"),customMenuEnabled=true,customMenuFile="image.png",customMenuLogoFile="logo.png",customMenuTextColor="Cyan"};
            mod.InitializeCustomMenuPaths();Check(mod.customMenuLayout=="Cover","Default menu placement fills screen");mod.UpdateCustomMenu();UnityEngine.Texture texture;UnityEngine.Material material;
            Check(mod.TryGetCustomMenuBackground(out texture,out material)&&texture.width==100,"Image chosen as custom background");
            Check(logoRenderer.sprite!=original&&logoRenderer.sprite.bounds.size.x<=10&&logoRenderer.sprite.bounds.size.y<=4,"Custom logo stays within original layout");
            Check(Near(logoRenderer.sprite.bounds.center.x,1)&&Near(logoRenderer.sprite.bounds.center.y,.7f),"Custom logo preserves offset bounds from the original pivot");
            Check(Near(text.color.a,.25f)&&Near(text.color.r,.18f)&&unrelated.color.r==1,"Text tint preserves alpha/selection brightness and excludes nonbuttons");
            Check(text.fontSize==150,"Color-only style leaves original font size untouched");
            for(int i=0;i<30;i++)
            {
                text.color=new UnityEngine.Color(text.color.r,text.color.g,text.color.b,.2f+i*.01f);
                tk.color2=new UnityEngine.Color(tk.color2.r,tk.color2.g,tk.color2.b,.1f+i*.01f);mod.UpdateCustomMenu();
                Check(Near(text.color.r,.18f)&&Near(text.color.g,.54f)&&Near(text.color.a,.2f+i*.01f),"Alpha-only game animation does not compound button tint");
                Check(Near(tk.color2.r,.12f)&&Near(tk.color2.g,.36f),"Alpha-only gradient animation does not compound tint");
            }
            var ownedLogo=logoRenderer.sprite;var ownedTexture=texture;
            mod.customMenuTextScale=1.2f;Tick(1);mod.UpdateCustomMenu();Check(Near(tk.scale.x,1.2f)&&Near(tk.scale.z,1)&&Near(tk.color2.a,.39f),"tk2d scale and gradient preserve Z and alpha");Check(text.fontSize==180,"Growing a large font uses a relative scale without shrinking it to an absolute cap");
            tk.scale=new UnityEngine.Vector3(tk.scale.x,tk.scale.y,2);mod.UpdateCustomMenu();Check(Near(tk.scale.x,1.2f)&&Near(tk.scale.z,2),"Unowned Z animation does not compound glyph scale");
            tk.useGradient=false;mod.UpdateCustomMenu();Check(Near(tk.color2.r,.4f)&&Near(tk.color2.a,.39f),"Disabling tk2d gradient releases the owned second color immediately");tk.useGradient=true;mod.UpdateCustomMenu();
            mod.customMenuTextScale=1;mod.UpdateCustomMenu();Check(tk.scale.x==1&&text.fontSize==150,"Return to 100 percent restores font and tk2d scale");
            text.color=new UnityEngine.Color(.8f,.8f,.8f,.8f);mod.UpdateCustomMenu();Check(Near(text.color.r,.24f)&&Near(text.color.a,.8f),"Game selection changes remain visible under tint");
            var replacement=new UnityEngine.Sprite {bounds=original.bounds};logoRenderer.sprite=replacement;mod.UpdateCustomMenu();
            Check(ownedLogo.destroyed&&logoRenderer.sprite!=replacement,"Game localization/menu style updates replace logo baseline safely");
            text.color=new UnityEngine.Color(text.color.r,text.color.g,text.color.b,.67f);
            mod.testScene="Town";mod.UpdateCustomMenu();
            Check(!mod.TryGetCustomMenuBackground(out texture,out material)&&ownedTexture.destroyed,"Entering gameplay destroys media and removes menu background");
            Check(logoRenderer.sprite==replacement&&Near(text.color.r,.8f)&&text.fontSize==150&&tk.scale.x==1,"Entering gameplay restores latest game logo and button properties");
            Check(Near(text.color.a,.67f),"Release after an unobserved alpha edit restores owned RGB and retains latest game alpha");
            mod.testScene="Menu_Title";mod.customMenuEnabled=true;mod.customMenuFile="video.mp4";File.Copy(Path.Combine(root,"video.mp4"),Path.Combine(media,"video.mp4"));
            mod.textureBrowserVisible=true;mod.UpdateCustomMenu();var video=UnityEngine.Video.VideoPlayer.Last;video.Ready();Check(!video.isPlaying,"F9 already open at video creation remains paused");
            mod.textureBrowserVisible=false;Tick(2);mod.UpdateCustomMenu();Check(video.isPlaying,"Closing F9 resumes menu video");mod.customMenuPaused=true;mod.UpdateCustomMenu();Check(!video.isPlaying,"Manual pause also stops menu video");
            mod.customMenuEnabled=false;mod.UpdateCustomMenu();Check(video.destroyed&&logoRenderer.sprite==replacement,"Disable theme releases native video and logo");
            mod.testScene="Menu_Title";mod.customMenuEnabled=true;mod.customMenuFile="image.png";mod.customMenuLogoFile="broken.png";
            File.WriteAllBytes(Path.Combine(media,"broken.png"),new byte[]{0,1});mod.UpdateCustomMenu();mod.UpdateCustomMenu();
            Check(mod.customMenuMessage.StartsWith("Logo error:"),"Invalid logo diagnostic remains visible when media updates");
            mod.ReleaseCustomMenu();
            var external=Path.Combine(root,"imported.png");File.WriteAllBytes(external,PngHeader(20,10));mod.picker=external;
            Check(mod.ImportMenuMedia(false)=="imported.png"&&mod.ImportMenuMedia(false)=="imported (2).png","Repeated imports preserve existing files with unique names");
            mod.picker=Path.Combine(media,"imported.png");Check(mod.ImportMenuMedia(true)=="imported.png","Selecting an already imported file avoids a duplicate copy");
            mod.customMenuLogoFile=mod.customMenuFile="image.png";mod.UpdateCustomMenu();var retainedBackground=mod.menuPlayback.Texture;var retainedLogo=logoRenderer.sprite;int priorSaves=mod.saved;
            using(var locked=new FileStream(Path.Combine(media,"image.png"),FileMode.Open,FileAccess.Read,FileShare.None))
            {
                mod.RemoveCustomMenuMedia("image.png");
                Check(mod.customMenuFile=="image.png"&&mod.customMenuLogoFile=="image.png"&&mod.saved==priorSaves,"Failed removal preserves both selected filenames and saved settings");
                Check(mod.menuPlayback.Texture==retainedBackground&&logoRenderer.sprite==retainedLogo,"Failed image removal preserves the currently displayed textures");
            }
            mod.RemoveCustomMenuMedia("image.png");Check(!File.Exists(Path.Combine(media,"image.png"))&&Directory.GetFiles(Path.Combine(media,"removed")).Length==1,"Media removal retains recoverable original");
            Check(mod.customMenuFile==""&&mod.customMenuLogoFile==""&&retainedBackground.destroyed,"Successful removal releases selected media only after move");
            // Real GIF decoder plus upload row order and pause timing; the texture itself is a substitute.
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"GifFixtures","local-palette.gif"),Path.Combine(media,"animation.gif"));
            Tick(10);var playback=new MenuMediaPlayback(Path.Combine(media,"animation.gif"),true);
            for(int i=0;i<500&&playback.Texture==null;i++){Thread.Sleep(2);playback.Update(true);}
            Check(playback.Texture!=null&&!playback.Failed,"Async GIF decode completes while paused");
            var data=GifAnimation.Decode(File.ReadAllBytes(Path.Combine(media,"animation.gif")));var expected=new GifAnimation.Playback(data);var tex=(UnityEngine.Texture2D)playback.Texture;
            uint top=expected.Pixels[0];int bottom=(data.Height-1)*data.Width*4;Check(tex.bytes[bottom]==(byte)top&&tex.bytes[bottom+1]==(byte)(top>>8),"GIF upload flips top row for Unity");
            byte[] before=(byte[])tex.bytes.Clone();Tick(30);playback.Update(true);Check(before.SequenceEqual(tex.bytes),"Pause prevents elapsed animation accumulation");playback.Update(false);Check(before.SequenceEqual(tex.bytes),"Resume does not jump over paused frames");
            Tick(30.05f);playback.Update(false);Check(!before.SequenceEqual(tex.bytes),"Unpaused GIF advances according to frame delay");playback.Dispose();Check(tex.destroyed,"GIF disposal destroys owned texture");
            File.WriteAllBytes(Path.Combine(media,"invalid.gif"),new byte[]{1,2,3});playback=new MenuMediaPlayback(Path.Combine(media,"invalid.gif"));
            for(int i=0;i<500&&!playback.Failed;i++){Thread.Sleep(2);playback.Update(false);}Check(playback.Failed&&playback.Texture==null,"Async decode error surfaces safely");playback.Dispose();
            var detached=Attach<UnityEngine.UI.Text>("ReparentedButtonText",button.transform);detached.fontSize=20;
            mod.customMenuEnabled=true;mod.customMenuTextScale=1.2f;Tick(40);mod.UpdateCustomMenu();
            Check(Near(detached.color.r,.3f)&&detached.fontSize==24,"Newly added menu button text receives current style");
            var otherMenu=Attach<UnityEngine.CanvasGroup>("OutsideMainMenu");button.transform.children.Remove(detached.transform);detached.transform.parent=otherMenu.transform;otherMenu.transform.children.Add(detached.transform);
            Tick(41);mod.UpdateCustomMenu();Check(detached.color.r==1&&detached.fontSize==20,"Leaving the title button hierarchy immediately restores owned text properties on rescan");
            var brokenText=Attach<tk2dTextMesh>("FailingCommit",button.transform);brokenText.commitThrows=true;
            mod.customMenuTextColor="Red";Tick(42);mod.UpdateCustomMenu();Check(Near(brokenText.color.g,.35f),"A failing tk2d Commit may leave a tracked partial property update");
            mod.customMenuEnabled=false;mod.UpdateCustomMenu();
            Check(Near(text.color.r,.8f)&&Near(text.color.g,.8f)&&text.fontSize==150,"A failed tk2d cleanup does not block restoration of other button properties");
            brokenText.commitThrows=false;mod.UpdateCustomMenu();Check(brokenText.color.r==1&&brokenText.color.g==1,"Partial failed setter restores its original color on release/retry");
        }
        static void Main()
        {
            string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-temp-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            try{GifChecks();OverrideChecks();PolicyChecks(root);VideoChecks(root);MenuChecks(root);Console.WriteLine("PASS: "+checks+" GIF pixel, media lifecycle, style restoration and import policy assertions (Unity API substitutes).");}
            finally{Directory.Delete(root,true);}
        }
    }
}
