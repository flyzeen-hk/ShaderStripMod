namespace ShaderStripMod
{
    internal static class BrowserInputPolicy
    {
        internal static bool ShouldClose(bool visible, bool escapePressed) { return visible && escapePressed; }
        internal static bool ShouldBlock(bool visible, bool waitingForRelease, int frame, int closeFrame)
        {
            return visible || waitingForRelease || frame <= closeFrame;
        }
    }
}
