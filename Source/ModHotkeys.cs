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
        ToggleVisuals
    }

    internal static class ModHotkeyPolicy
    {
        internal static ModHotkeyAction Resolve(
            bool browserVisible,
            bool escapePressed,
            bool f8Pressed,
            bool f9Pressed,
            bool f10Pressed,
            bool f11Pressed,
            bool f12Pressed)
        {
            if (browserVisible && escapePressed)
            {
                return ModHotkeyAction.CloseBrowser;
            }

            if (f9Pressed)
            {
                return ModHotkeyAction.ToggleBrowser;
            }

            // F9 owns keyboard input while it is open, including text fields.
            if (browserVisible)
            {
                return ModHotkeyAction.None;
            }

            if (f8Pressed)
            {
                return ModHotkeyAction.ToggleCameraPin;
            }

            if (f10Pressed)
            {
                return ModHotkeyAction.DumpLoadedAssets;
            }

            if (f11Pressed)
            {
                return ModHotkeyAction.ReloadPreserveList;
            }

            if (f12Pressed)
            {
                return ModHotkeyAction.ToggleVisuals;
            }

            return ModHotkeyAction.None;
        }
    }
}
