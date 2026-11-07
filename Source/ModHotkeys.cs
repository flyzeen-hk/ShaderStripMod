namespace ShaderStripMod
{
    internal enum ModHotkeyAction
    {
        None,
        CloseBrowser,
        ToggleBrowser,
        ToggleCameraPin,
        DumpLoadedAssets,
        ReloadPreserveList,
        ToggleGameplayIsolation
    }

    internal static class ModHotkeyPolicy
    {
        internal static ModHotkeyAction Resolve(
            bool browserVisible,
            bool escapePressed,
            bool browserTogglePressed,
            bool cameraPinPressed,
            bool dumpPressed,
            bool reloadPressed,
            bool isolationPressed)
        {
            if (browserVisible && escapePressed)
            {
                return ModHotkeyAction.CloseBrowser;
            }

            if (browserTogglePressed)
            {
                return ModHotkeyAction.ToggleBrowser;
            }

            // The browser owns keyboard input while open, including text fields.
            if (browserVisible)
            {
                return ModHotkeyAction.None;
            }

            if (cameraPinPressed)
            {
                return ModHotkeyAction.ToggleCameraPin;
            }

            if (dumpPressed)
            {
                return ModHotkeyAction.DumpLoadedAssets;
            }

            if (reloadPressed)
            {
                return ModHotkeyAction.ReloadPreserveList;
            }

            if (isolationPressed)
            {
                return ModHotkeyAction.ToggleGameplayIsolation;
            }

            return ModHotkeyAction.None;
        }
    }
}
