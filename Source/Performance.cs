using System.Collections.Generic;
using System.IO;
using System.Text;
using Modding;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private void ClearRuntimeCaches()
        {
            objectPathCache.Clear();
            objectPathCacheOrder.Clear();
            objectPathCacheHits = 0;
            objectPathCacheMisses = 0;
            objectPathCacheEvictions = 0;
            InvalidateVisualOverrideResolutionCache();
        }

        private float GetApplyStripInterval()
        {
            return applyStripIntervalSetting;
        }

        private float GetProxyUpdateInterval()
        {
            return proxyUpdateIntervalSetting;
        }

        private float GetProjectileTime()
        {
            return projectileUseUnscaledTime ? Time.unscaledTime : Time.time;
        }

        private bool ShouldCaptureProjectilesInCurrentState()
        {
            string mode = NormalizeProjectileCaptureMode(projectileCaptureMode);
            if (string.Equals(mode, ProjectileCaptureManual, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(mode, ProjectileCaptureAlways, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(mode, ProjectileCaptureBrowserOpen, System.StringComparison.OrdinalIgnoreCase))
            {
                return textureBrowserVisible || !pauseBrowserScansWhenClosed;
            }

            return textureBrowserVisible && textureBrowserProjectileMode;
        }

        private bool TryEnterScheduledTask(ref bool ranNormalTask, ref double frameBudgetUsedMs, bool urgent)
        {
            if (urgent)
            {
                return true;
            }

            if (!staggerPerformanceTasks)
            {
                return true;
            }

            if (ranNormalTask || (heavyWorkBudgetMs > 0f && frameBudgetUsedMs >= heavyWorkBudgetMs))
            {
                performanceTasksDeferred++;
                return false;
            }

            ranNormalTask = true;
            return true;
        }

        private bool ShouldCollectPerformanceMetrics()
        {
            return showPerformanceCounters || showCacheStats || collectDetailedMetrics;
        }

        private long BeginPerformanceMetric(out long startMemory)
        {
            startMemory = 0L;
            if (!ShouldCollectPerformanceMetrics())
            {
                return 0L;
            }

            if (showGcAllocStats)
            {
                startMemory = System.GC.GetTotalMemory(false);
            }

            return System.Diagnostics.Stopwatch.GetTimestamp();
        }

        private double EndPerformanceMetric(int metric, long startTicks, long startMemory)
        {
            if (startTicks == 0L)
            {
                return 0.0;
            }

            long endTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            double elapsedMs = (endTicks - startTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            long gcBytes = 0L;
            if (showGcAllocStats)
            {
                gcBytes = System.GC.GetTotalMemory(false) - startMemory;
            }

            if (metric == PerformanceMetricApplyStrip)
            {
                lastApplyStripMs = elapsedMs;
                lastApplyStripGcBytes = gcBytes;
            }
            else if (metric == PerformanceMetricDrawSync)
            {
                lastDrawSyncMs = elapsedMs;
                lastDrawSyncGcBytes = gcBytes;
            }
            else if (metric == PerformanceMetricProjectileScan)
            {
                lastProjectileScanMs = elapsedMs;
                lastProjectileScanGcBytes = gcBytes;
            }
            else if (metric == PerformanceMetricProxyUpdate)
            {
                lastProxyUpdateMs = elapsedMs;
                lastProxyUpdateGcBytes = gcBytes;
            }

            return elapsedMs;
        }

        private void SetPerformanceDirtyFromToggle()
        {
            if (!string.Equals(performanceProfile, PerformanceProfileCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                performanceProfile = PerformanceProfileCustom;
                projectilePerformanceMode = ProjectilePerformanceCustom;
                UpdatePerformanceInputs();
                UpdateProjectilePerformanceInputs();
            }
        }

        private string NormalizeProjectilePerformanceMode(string mode)
        {
            string cleaned = CleanUnityName(mode);
            for (int i = 0; i < ProjectilePerformanceModes.Length; i++)
            {
                if (string.Equals(cleaned, ProjectilePerformanceModes[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return ProjectilePerformanceModes[i];
                }
            }

            return ProjectilePerformanceBalanced;
        }

        private void ApplyProjectilePerformancePreset(string mode)
        {
            string normalized = NormalizeProjectilePerformanceMode(mode);
            if (string.Equals(normalized, ProjectilePerformanceSafe, System.StringComparison.OrdinalIgnoreCase))
            {
                projectilePerformanceMode = ProjectilePerformanceSafe;
                SetProjectilePerformanceValues(0.20f, 3f, 0.20f, 120, 24);
                return;
            }

            if (string.Equals(normalized, ProjectilePerformanceAggressive, System.StringComparison.OrdinalIgnoreCase))
            {
                projectilePerformanceMode = ProjectilePerformanceAggressive;
                SetProjectilePerformanceValues(0.06f, 8f, 0.06f, 320, 120);
                return;
            }

            if (string.Equals(normalized, ProjectilePerformanceCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                projectilePerformanceMode = ProjectilePerformanceCustom;
                return;
            }

            projectilePerformanceMode = ProjectilePerformanceBalanced;
            SetProjectilePerformanceValues(DefaultProjectileCaptureInterval, DefaultRecentProjectileLifetime, DefaultPreservedSyncIntervalSetting, DefaultMaxPreservedRenderers, DefaultMaxProjectileParticleRenderers);
        }

        private void SetProjectilePerformanceValues(float captureInterval, float lifetime, float preservedSyncInterval, int preservedRenderers, int projectileParticleRenderers)
        {
            projectileCaptureInterval = ClampProjectilePerformanceFloat(captureInterval);
            recentProjectileLifetime = ClampProjectilePerformanceFloat(lifetime);
            preservedSyncIntervalSetting = ClampProjectilePerformanceFloat(preservedSyncInterval);
            maxPreservedRenderers = ClampProjectilePerformanceInt(preservedRenderers);
            maxProjectileParticleRenderers = ClampProjectilePerformanceInt(projectileParticleRenderers);
        }

        private float ClampProjectilePerformanceFloat(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp(value, MinProjectilePerformanceValue, MaxProjectilePerformanceValue);
        }

        private int ClampProjectilePerformanceInt(int value)
        {
            return Mathf.Clamp(value, (int)MinProjectilePerformanceValue, (int)MaxProjectilePerformanceValue);
        }

        private float ParseSettingsFloat(string value, float fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            float parsed;
            string normalized = value.Trim().Replace(',', '.');
            if (float.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private int ParseSettingsInt(string value, int fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            int parsed;
            if (int.TryParse(value.Trim(), out parsed))
            {
                return parsed;
            }

            float parsedFloat = ParseSettingsFloat(value, fallback);
            return Mathf.RoundToInt(parsedFloat);
        }

        private bool ParseSettingsBool(string value, bool fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            string cleaned = value.Trim();
            if (string.Equals(cleaned, "true", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "yes", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "on", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "1", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(cleaned, "false", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "no", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "off", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "0", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return fallback;
        }

        private bool ParseProjectileTimeSource(string value, bool fallback)
        {
            string cleaned = CleanUnityName(value);
            if (string.Equals(cleaned, "UnscaledTime", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "Unscaled", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(cleaned, "GameTime", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleaned, "Game", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return ParseSettingsBool(value, fallback);
        }

        private string FormatSettingsFloat(float value)
        {
            return ClampProjectilePerformanceFloat(value).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private string FormatSettingsBool(bool value)
        {
            return value ? "true" : "false";
        }

        private float ClampPerformanceInterval(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp(value, 0f, MaxPerformanceIntervalValue);
        }

        private float ClampHeavyWorkBudget(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return DefaultHeavyWorkBudgetMs;
            }

            return Mathf.Clamp(value, 0f, MaxHeavyWorkBudgetMs);
        }

        private float ClampGcBudget(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return DefaultGcBudgetKb;
            }

            return Mathf.Clamp(value, 0f, MaxGcBudgetKb);
        }

        private int ClampVisibleBrowserEntries(int value)
        {
            return Mathf.Clamp(value, 0, MaxVisibleBrowserEntriesHardCap);
        }

        private int ClampBrowserOverscan(int value)
        {
            return Mathf.Clamp(value, 0, MaxBrowserListOverscanRows);
        }

        private int ClampCachedRendererPaths(int value)
        {
            return Mathf.Clamp(value, 0, MaxCachedRendererPathsHardCap);
        }

        private void UpdateProjectilePerformanceInputs()
        {
            projectileCaptureIntervalInput = FormatSettingsFloat(projectileCaptureInterval);
            recentProjectileLifetimeInput = FormatSettingsFloat(recentProjectileLifetime);
            preservedSyncIntervalInput = FormatSettingsFloat(preservedSyncIntervalSetting);
            maxPreservedRenderersInput = maxPreservedRenderers.ToString();
            maxProjectileParticleRenderersInput = maxProjectileParticleRenderers.ToString();
        }

        private void SetProjectilePerformanceMode(string mode)
        {
            string normalized = NormalizeProjectilePerformanceMode(mode);
            if (string.Equals(normalized, ProjectilePerformanceCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                performanceProfile = PerformanceProfileCustom;
                projectilePerformanceMode = ProjectilePerformanceCustom;
                UpdateProjectilePerformanceInputs();
                UpdatePerformanceInputs();
                SaveExportSettings();
                lastPerformanceMessage = "Custom projectile settings selected. Edit values, then press Apply Custom.";
                return;
            }

            performanceProfile = PerformanceProfileCustom;
            ApplyProjectilePerformancePreset(normalized);
            UpdateProjectilePerformanceInputs();
            UpdatePerformanceInputs();
            SaveExportSettings();
            RefreshAfterProjectilePerformanceChange();
            lastPerformanceMessage = "Projectile performance set to " + projectilePerformanceMode + " inside Custom profile.";
        }

        private void ApplyCustomProjectilePerformanceSettings()
        {
            float captureInterval;
            float lifetime;
            float preservedSyncInterval;
            int preservedRenderers;
            int projectileParticleRenderers;
            if (!TryReadProjectilePerformanceInputs(out captureInterval, out lifetime, out preservedSyncInterval, out preservedRenderers, out projectileParticleRenderers))
            {
                lastSettingsMessage = "Projectile custom settings rejected: numbers only.";
                return;
            }

            performanceProfile = PerformanceProfileCustom;
            projectilePerformanceMode = ProjectilePerformanceCustom;
            SetProjectilePerformanceValues(captureInterval, lifetime, preservedSyncInterval, preservedRenderers, projectileParticleRenderers);
            UpdateProjectilePerformanceInputs();
            UpdatePerformanceInputs();
            SaveExportSettings();
            RefreshAfterProjectilePerformanceChange();
            lastPerformanceMessage = "Projectile custom settings applied.";
        }

        private bool TryReadProjectilePerformanceInputs(out float captureInterval, out float lifetime, out float preservedSyncInterval, out int preservedRenderers, out int projectileParticleRenderers)
        {
            captureInterval = 0f;
            lifetime = 0f;
            preservedSyncInterval = 0f;
            preservedRenderers = 0;
            projectileParticleRenderers = 0;
            if (!TryParseInputFloat(projectileCaptureIntervalInput, out captureInterval) ||
                !TryParseInputFloat(recentProjectileLifetimeInput, out lifetime) ||
                !TryParseInputFloat(preservedSyncIntervalInput, out preservedSyncInterval) ||
                !TryParseInputInt(maxPreservedRenderersInput, out preservedRenderers) ||
                !TryParseInputInt(maxProjectileParticleRenderersInput, out projectileParticleRenderers))
            {
                return false;
            }

            return true;
        }

        private bool TryParseInputFloat(string value, out float result)
        {
            result = 0f;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return float.TryParse(value.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result);
        }

        private bool TryParseInputInt(string value, out int result)
        {
            result = 0;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (int.TryParse(value.Trim(), out result))
            {
                return true;
            }

            float parsed;
            if (!TryParseInputFloat(value, out parsed))
            {
                return false;
            }

            result = Mathf.RoundToInt(parsed);
            return true;
        }

        private void ResetProjectilePerformanceDefaults()
        {
            ApplyProjectilePerformancePreset(ProjectilePerformanceBalanced);
            performanceProfile = PerformanceProfileCustom;
            projectilePerformanceMode = ProjectilePerformanceCustom;
            UpdateProjectilePerformanceInputs();
            UpdatePerformanceInputs();
            SaveExportSettings();
            RefreshAfterProjectilePerformanceChange();
            lastPerformanceMessage = "Projectile values reset to Balanced inside Custom profile.";
        }

        private void RefreshAfterProjectilePerformanceChange()
        {
            nextRecentProjectileCaptureTime = 0f;
            if (recentProjectileRenderers.Count > 0)
            {
                RemoveExpiredRecentProjectileRenderers(GetProjectileTime());
            }

            MarkPreservedRendererCacheDirty();
            preserveDrawSyncRequested = true;
            RestorePreservedRendererDraws();
            RestorePreservedRendererProxies();
            if (visualsActive)
            {
                ApplyStrip();
            }

            if (textureBrowserProjectileMode)
            {
                RefreshRecentProjectileBrowserEntries();
            }
        }

    }
}
