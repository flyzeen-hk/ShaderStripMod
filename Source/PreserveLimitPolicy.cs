namespace ShaderStripMod
{
    internal enum PreserveScanState
    {
        IsolationOff,
        NoRules,
        Suspended,
        Pending,
        NoMatches,
        LimitsReached,
        Healthy
    }

    internal struct PreserveScanStatus
    {
        internal PreserveScanState State;
        internal int Matched;
        internal int Drawn;
        internal int Blocked;
        internal int DrawSkipped;
        internal int ParticleSkipped;
    }

    internal static class PreserveLimitPolicy
    {
        internal static PreserveScanStatus Evaluate(bool isolationEnabled, int ruleCount, PreserveRunStats stats)
        {
            return Evaluate(isolationEnabled, ruleCount, false, stats);
        }

        internal static PreserveScanStatus Evaluate(bool isolationEnabled, int ruleCount, bool rulesSuspended, PreserveRunStats stats)
        {
            PreserveScanStatus status = new PreserveScanStatus();
            if (stats != null)
            {
                status.Matched = stats.CandidateMatched;
                status.Drawn = stats.Drawn;
                status.Blocked = stats.Stale;
                status.DrawSkipped = stats.SkippedByDrawLimit;
                status.ParticleSkipped = stats.SkippedByParticleLimit;
            }

            if (!isolationEnabled)
            {
                status.State = PreserveScanState.IsolationOff;
            }
            else if (ruleCount <= 0)
            {
                status.State = PreserveScanState.NoRules;
            }
            else if (rulesSuspended)
            {
                status.State = PreserveScanState.Suspended;
            }
            else if (stats == null || !stats.ScanCompleted)
            {
                status.State = PreserveScanState.Pending;
            }
            else if (status.Matched <= 0)
            {
                status.State = PreserveScanState.NoMatches;
            }
            else if (status.DrawSkipped > 0 || status.ParticleSkipped > 0)
            {
                status.State = PreserveScanState.LimitsReached;
            }
            else
            {
                status.State = PreserveScanState.Healthy;
            }

            return status;
        }
    }
}
