using System;

namespace ShaderStripMod
{
    internal struct MediaAudioSettings
    {
        internal bool Enabled;
        internal float Volume;
        internal bool Muted;
        internal float MasterVolume;

        internal float EffectiveVolume
        {
            get { return MediaAudioPolicy.EffectiveVolume(Enabled, Volume, Muted, MasterVolume); }
        }

        internal static MediaAudioSettings Silent
        {
            get { return new MediaAudioSettings { Enabled = false, Volume = MediaAudioPolicy.DefaultVolume, Muted = false, MasterVolume = MediaAudioPolicy.DefaultMasterVolume }; }
        }
    }

    internal static class MediaAudioPolicy
    {
        internal const float DefaultVolume = 0.5f;
        internal const float DefaultMasterVolume = 1f;

        internal static float ClampVolume(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return ClampVolume(fallback, 0f);
            }

            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }

        internal static float EffectiveVolume(bool enabled, float volume, bool muted, float masterVolume)
        {
            if (!enabled || muted)
            {
                return 0f;
            }

            return ClampVolume(volume, DefaultVolume) * ClampVolume(masterVolume, DefaultMasterVolume);
        }
    }
}
