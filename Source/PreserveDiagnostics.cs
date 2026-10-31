namespace ShaderStripMod
{
    internal enum PreserveSkipKind
    {
        Drawable,
        MissingRenderer,
        MissingObject,
        ProxyRenderer,
        Disabled,
        Inactive,
        AlreadyVisible,
        ExpensiveParticle,
        OtherScene,
        NoRule,
        NoMaterials
    }

    internal sealed class PreserveRunStats
    {
        internal int CandidateMatched;
        internal int Sources;
        internal int Drawn;
        internal int Stale;
        internal int SkippedByDrawLimit;
        internal int SkippedByParticleLimit;
        internal int SkippedMissing;
        internal int SkippedDisabled;
        internal int SkippedInactive;
        internal int SkippedAlreadyVisible;
        internal int SkippedExpensiveParticle;
        internal int SkippedOtherScene;
        internal int SkippedNoRule;
        internal int SkippedNoMaterials;
        internal int SkippedProxy;

        internal void ResetFrame()
        {
            Drawn = 0;
            Stale = 0;
            SkippedMissing = 0;
            SkippedDisabled = 0;
            SkippedInactive = 0;
            SkippedAlreadyVisible = 0;
            SkippedExpensiveParticle = 0;
            SkippedOtherScene = 0;
            SkippedNoRule = 0;
            SkippedNoMaterials = 0;
            SkippedProxy = 0;
        }

        internal void ResetScan()
        {
            CandidateMatched = 0;
            Sources = 0;
            SkippedByDrawLimit = 0;
            SkippedByParticleLimit = 0;
        }

        internal void RecordStale(PreserveSkipKind kind)
        {
            Stale++;
            switch (kind)
            {
                case PreserveSkipKind.MissingRenderer:
                case PreserveSkipKind.MissingObject:
                    SkippedMissing++;
                    break;
                case PreserveSkipKind.ProxyRenderer:
                    SkippedProxy++;
                    break;
                case PreserveSkipKind.Disabled:
                    SkippedDisabled++;
                    break;
                case PreserveSkipKind.Inactive:
                    SkippedInactive++;
                    break;
                case PreserveSkipKind.AlreadyVisible:
                    SkippedAlreadyVisible++;
                    break;
                case PreserveSkipKind.ExpensiveParticle:
                    SkippedExpensiveParticle++;
                    break;
                case PreserveSkipKind.OtherScene:
                    SkippedOtherScene++;
                    break;
                case PreserveSkipKind.NoRule:
                    SkippedNoRule++;
                    break;
                case PreserveSkipKind.NoMaterials:
                    SkippedNoMaterials++;
                    break;
            }
        }

        internal static string Describe(PreserveSkipKind kind)
        {
            switch (kind)
            {
                case PreserveSkipKind.MissingRenderer:
                    return "renderer is missing";
                case PreserveSkipKind.MissingObject:
                    return "renderer has no GameObject";
                case PreserveSkipKind.ProxyRenderer:
                    return "renderer is ShaderStripMod proxy";
                case PreserveSkipKind.Disabled:
                    return "renderer disabled";
                case PreserveSkipKind.Inactive:
                    return "object inactive";
                case PreserveSkipKind.AlreadyVisible:
                    return "renderer layer is already visible to the main camera";
                case PreserveSkipKind.ExpensiveParticle:
                    return "particle renderer without projectile path/renderer rule";
                case PreserveSkipKind.OtherScene:
                    return "scene mismatch or non-gameplay scene";
                case PreserveSkipKind.NoRule:
                    return "no matching preserve rule";
                case PreserveSkipKind.NoMaterials:
                    return "renderer has no shared materials";
                default:
                    return "none";
            }
        }
    }
}
