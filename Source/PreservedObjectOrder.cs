using System;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private string NormalizePreservedObjectOrder(string value)
        {
            return string.Equals(value != null ? value.Trim() : "", "InFront", StringComparison.OrdinalIgnoreCase) ? "InFront" : "BehindHero";
        }
        private void SetPreservedObjectOrder(string value)
        {
            string normalized = NormalizePreservedObjectOrder(value);
            if (normalized == preservedObjectOrder) return;
            // Detach using the previous event before changing the rendering stage.
            RestorePreservedRendererDraws();
            RestorePreservedRendererProxies();
            preservedObjectOrder = normalized;
            MarkPreservedRendererCacheDirty();
            SaveExportSettings();
            if (visualsActive) ApplyStrip();
        }
    }
}
