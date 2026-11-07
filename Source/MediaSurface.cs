using System;
using System.IO;
using UnityEngine;

namespace ShaderStripMod
{
    // Owns one decoded image/GIF/video and its optional audio route.
    // The composition work in 1.30.x creates several independent surfaces from this type.
    internal sealed class MediaSurface : IDisposable
    {
        private MenuMediaPlayback playback;
        private bool disposed;
        private bool audioEnabled;

        internal string MediaPath { get; private set; }
        internal string MediaName { get; private set; }
        internal string Message { get { return disposed || playback == null ? "" : playback.Message; } }
        internal Texture Texture { get { return disposed || playback == null ? null : playback.Texture; } }
        internal bool Animated { get { return !disposed && playback != null && playback.Animated; } }
        internal bool Failed { get { return disposed || playback == null || playback.Failed; } }
        internal bool IsLoaded { get { return !disposed && playback != null; } }
        internal bool AudioEnabled { get { return audioEnabled; } }
        internal bool VideoHasNoAudioTrack { get { return !disposed && playback != null && playback.VideoHasNoAudioTrack; } }

        internal MediaSurface(string path, bool initiallyPaused, MediaAudioSettings audio)
        {
            MediaPath = path;
            MediaName = Path.GetFileName(path);
            audioEnabled = audio.Enabled;
            playback = new MenuMediaPlayback(path, initiallyPaused, audio);
        }

        internal void Update(bool pause)
        {
            if (!disposed && playback != null)
            {
                playback.Update(pause);
            }
        }

        // Audio route changes require a new VideoPlayer. Volume and mute can be applied live.
        internal bool TryApplyAudio(MediaAudioSettings audio)
        {
            if (audio.Enabled != audioEnabled)
            {
                return false;
            }

            if (playback != null)
            {
                playback.ApplyAudio(audio);
            }

            return true;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (playback != null)
            {
                playback.Dispose();
                playback = null;
            }
        }
    }
}
