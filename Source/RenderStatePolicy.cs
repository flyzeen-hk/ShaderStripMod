namespace ShaderStripMod
{
    internal enum RenderSceneState
    {
        Menu,
        GameplayWithHero,
        GameplayWithoutHero
    }

    internal static class RenderStatePolicy
    {
        internal static RenderSceneState Classify(bool menuScene, bool heroPresent)
        {
            if (menuScene)
            {
                return RenderSceneState.Menu;
            }

            return heroPresent ? RenderSceneState.GameplayWithHero : RenderSceneState.GameplayWithoutHero;
        }
    }
}
