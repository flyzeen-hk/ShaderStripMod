using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private bool browserPreviousAudioPause;



        private void UpdateTextureBrowserPause()
        {
            if (textureBrowserVisible && pauseGameWhileBrowserOpen)
            {
                if (!textureBrowserPauseActive)
                {
                    textureBrowserPreviousTimeScale = Time.timeScale;
                    browserPreviousAudioPause = AudioListener.pause;
                    textureBrowserPauseActive = true;
                }
                // Game code may reset its time scale; also suspend audio to prevent stuck loops.
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            else ReleaseTextureBrowserPause();
        }

        private void ReleaseTextureBrowserPause()
        {
            if (!textureBrowserPauseActive) return;
            Time.timeScale = textureBrowserPreviousTimeScale;
            AudioListener.pause = browserPreviousAudioPause;
            textureBrowserPreviousTimeScale = 1f;
            textureBrowserPauseActive = false;
        }
    }
}

