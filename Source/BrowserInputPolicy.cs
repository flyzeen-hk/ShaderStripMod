namespace ShaderStripMod
{
    internal static class BrowserInputPolicy
    {
        internal static bool ShouldClose(bool visible, bool escapePressed) { return visible && escapePressed; }
        internal static bool ShouldToggleCameraPin(bool visible, bool f8Pressed) { return !visible && f8Pressed; }
        internal static bool ShouldBlock(bool visible, bool waitingForRelease, int frame, int closeFrame)
        {
            return visible || waitingForRelease || frame <= closeFrame;
        }
    }
}
