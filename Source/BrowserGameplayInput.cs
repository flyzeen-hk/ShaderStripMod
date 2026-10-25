using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private bool ShouldBlockBrowserGameplayInput()
        {
            if (textureBrowserVisible) return true;
            if (browserWaitingForInputRelease && !Input.anyKey && !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
                browserWaitingForInputRelease = false;
            return BrowserInputPolicy.ShouldBlock(textureBrowserVisible, browserWaitingForInputRelease, Time.frameCount, browserInputReleaseFrame);
        }

        private void OnInputHandlerUpdate(On.InputHandler.orig_Update orig,InputHandler self)
        {
            if(!ShouldBlockBrowserGameplayInput()) { orig(self);return; }
            // InputHandler also toggles the game's pause menu outside EventSystem/OnGUI.
            // Preserve controller updates, and suppress only its acceptingInput branch for this call.
            bool previous=self.acceptingInput;
            try { self.acceptingInput=false;orig(self); }
            finally { self.acceptingInput=previous; }
        }
        private void OnHeroLookForInput(On.HeroController.orig_LookForInput orig, HeroController self)
        {
            if (!ShouldBlockBrowserGameplayInput()) orig(self);
        }
        private void OnHeroLookForQueueInput(On.HeroController.orig_LookForQueueInput orig, HeroController self)
        {
            if (!ShouldBlockBrowserGameplayInput()) orig(self);
        }
        private void OnHeroDoAttack(On.HeroController.orig_DoAttack orig, HeroController self)
        {
            if (!ShouldBlockBrowserGameplayInput()) orig(self);
        }
    }
}
