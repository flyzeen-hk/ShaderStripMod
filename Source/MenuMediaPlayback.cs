using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Video;
namespace ShaderStripMod
{
    internal sealed class MenuMediaPlayback : IDisposable
    {
        private GameObject videoObject;
        private VideoPlayer video;
        private Texture2D image;
        private Task<GifAnimation> pending;
        private CancellationTokenSource decodeCancellation;
        private GifAnimation animation;
        private GifAnimation.Playback gif;
        private byte[] upload;
        private bool disposed,paused,failed;
        private float lastTime,prepareStarted;
        internal string Message {get;private set;}
        internal Texture Texture {get {return disposed||failed ? null : (image!=null ? image : (video!=null&&video.isPrepared ? video.texture : null));}}
        internal bool Animated {get {return gif!=null||pending!=null||video!=null;}}
        internal bool Failed {get {return failed;}}
        internal MenuMediaPlayback(string path,bool initiallyPaused=false)
        {
            paused=initiallyPaused;lastTime=Time.realtimeSinceStartup;Message="Loading menu media...";
            try
            {
                var file=new FileInfo(path);string ext=Path.GetExtension(path);
                long limit=MenuMediaPolicy.IsVideo(ext)?1024L*1024*1024:64L*1024*1024;
                if(!file.Exists||file.Length==0||file.Length>limit)throw new IOException(MenuMediaPolicy.IsVideo(ext)?"Video must be smaller than 1 GiB.":"Image/GIF must be smaller than 64 MiB.");
                if(MenuMediaPolicy.IsImage(ext))
                {
                    var encoded=File.ReadAllBytes(path);MenuMediaPolicy.ValidateImageDimensions(encoded,ext);
                    image=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    if(!image.LoadImage(encoded,true))throw new IOException("Cannot decode menu image.");
                    MenuMediaPolicy.ValidateDecodedImageDimensions(image.width,image.height);
                    ConfigureTexture(image);Message="Menu image loaded.";
                }
                else if(ext.Equals(".gif",StringComparison.OrdinalIgnoreCase))
                {decodeCancellation=new CancellationTokenSource();var token=decodeCancellation.Token;pending=Task.Run(()=>GifAnimation.Decode(File.ReadAllBytes(path),token),token);}
                else if(MenuMediaPolicy.IsVideo(ext))
                {
                    videoObject=new GameObject("ShaderStripMod Menu Video") {hideFlags=HideFlags.HideAndDontSave};
                    UnityEngine.Object.DontDestroyOnLoad(videoObject);
                    video=videoObject.AddComponent<VideoPlayer>();video.playOnAwake=false;video.source=VideoSource.Url;
                    video.renderMode=VideoRenderMode.APIOnly;video.audioOutputMode=VideoAudioOutputMode.None;
                    video.isLooping=true;video.waitForFirstFrame=true;video.timeReference=VideoTimeReference.Freerun;
                    if(video.canSetSkipOnDrop)video.skipOnDrop=true;
                    // Unity's Windows Media Foundation backend treats escaped file-URI characters
                    // as literal path characters. Keep Unicode, spaces and percent signs intact.
                    video.url=Path.GetFullPath(path);video.errorReceived+=OnVideoError;video.prepareCompleted+=OnVideoPrepared;
                    prepareStarted=Time.realtimeSinceStartup;video.Prepare();
                }
                else throw new IOException("Unsupported menu media format.");
            }
            catch(Exception ex){Fail(ex.Message);}
        }
        private static void ConfigureTexture(Texture2D texture)
        {texture.name="ShaderStripMod Menu Media";texture.hideFlags=HideFlags.HideAndDontSave;texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;}
        private void OnVideoPrepared(VideoPlayer source)
        {
            if(disposed||source!=video||failed)return;
            if(source.width>8192||source.height>8192){Fail("Video dimensions exceed 8192.");return;}
            Message="Menu video ready (silent, looped).";if(!paused)source.Play();
        }
        private void OnVideoError(VideoPlayer source,string message)
        {if(!disposed&&!failed&&source==video)Fail("Video cannot play: "+message+". Try an MP4 encoded with H.264.");}
        private void Fail(string message)
        {
            failed=true;Message="Menu media error: "+message;
            if(video!=null){try{video.Stop();}catch(Exception){ }}
            if(image!=null){UnityEngine.Object.Destroy(image);image=null;}
        }
        internal void Update(bool pause)
        {
            if(disposed)return;
            float now=Time.realtimeSinceStartup;double delta=Math.Max(0,now-lastTime);lastTime=now;
            bool wasPaused=paused;paused=pause;
            if(failed)return;
            try
            {
                if(pending!=null&&pending.IsCompleted)
                {
                    var task=pending;pending=null;
                    try {animation=task.GetAwaiter().GetResult();}
                    finally {decodeCancellation.Dispose();decodeCancellation=null;}
                    gif=new GifAnimation.Playback(animation);
                    upload=new byte[animation.Width*animation.Height*4];image=new Texture2D(animation.Width,animation.Height,TextureFormat.RGBA32,false);ConfigureTexture(image);UploadGif();
                    Message="Menu GIF loaded.";delta=0;
                }
                if(gif!=null&&!pause&&!wasPaused&&gif.Advance(delta))UploadGif();
                if(video!=null)
                {
                    if(!video.isPrepared&&now-prepareStarted>30){Fail("Video preparation timed out. Try an MP4 encoded with H.264.");return;}
                    if(video.isPrepared)
                    {
                        if(pause&&video.isPlaying)video.Pause();else if(!pause&&!video.isPlaying)video.Play();
                    }
                }
            }
            catch(Exception ex){Fail(ex.Message);}
        }
        private void UploadGif()
        {
            // GIF rows start at the top; Unity textures start at the bottom.
            for(int y=0;y<animation.Height;y++)for(int x=0;x<animation.Width;x++)
            {
                uint pixel=gif.Pixels[y*animation.Width+x];int i=((animation.Height-1-y)*animation.Width+x)*4;
                upload[i]=(byte)pixel;upload[i+1]=(byte)(pixel>>8);upload[i+2]=(byte)(pixel>>16);upload[i+3]=(byte)(pixel>>24);
            }
            image.LoadRawTextureData(upload);image.Apply(false,false);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            if(decodeCancellation!=null){decodeCancellation.Cancel();decodeCancellation.Dispose();decodeCancellation=null;}
            if(pending!=null)pending.ContinueWith(t=>{var observed=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
            pending=null;gif=null;animation=null;upload=null;
            // Transfer ownership before native calls: Stop may throw, and Dispose must still destroy
            // all of our objects on this first call (subsequent calls are deliberately harmless).
            var ownedVideo=video;var ownedVideoObject=videoObject;var ownedImage=image;
            video=null;videoObject=null;image=null;
            if(ownedVideo!=null)
            {
                ownedVideo.errorReceived-=OnVideoError;ownedVideo.prepareCompleted-=OnVideoPrepared;
                try{ownedVideo.Stop();}catch(Exception ex){Message="Menu media error: "+ex.Message;}
            }
            DestroyOwnedObject(ownedVideoObject);DestroyOwnedObject(ownedImage);
        }
        private void DestroyOwnedObject(UnityEngine.Object target)
        {
            if(target==null)return;
            try{UnityEngine.Object.Destroy(target);}catch(Exception ex){Message="Menu media error: "+ex.Message;}
        }
    }
}
