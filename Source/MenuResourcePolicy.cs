using System;
namespace ShaderStripMod
{
    internal static class MenuResourcePolicy
    {
        internal static bool Includes(string scene, bool validScene, bool belongsToMenuCanvas, bool hasCanvas, bool menuLayer)
        {
            if (!validScene || string.IsNullOrEmpty(scene)) return false;
            return belongsToMenuCanvas || scene.StartsWith("Menu", StringComparison.OrdinalIgnoreCase) || (hasCanvas && menuLayer);
        }
        internal static bool Matches(string search, string name, string kind, string details)
        {
            search = (search ?? "").Trim();
            if (search.Length == 0) return true;
            return Contains(name, search) || Contains(kind, search) || Contains(details, search);
        }
        private static bool Contains(string value, string search)
        { return !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0; }
    }
}