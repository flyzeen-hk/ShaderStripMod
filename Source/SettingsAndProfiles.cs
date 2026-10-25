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
        private void InitializeListFiles()
        {
            string assemblyDirectory = null;
            try
            {
                string location = typeof(ShaderStripMod).Assembly.Location;
                if (!string.IsNullOrEmpty(location))
                {
                    assemblyDirectory = Path.GetDirectoryName(location);
                }
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod could not resolve mod directory: " + ex.Message);
            }

            if (TryUseListDirectory(assemblyDirectory))
            {
                return;
            }

            if (TryUseListDirectory(Application.persistentDataPath))
            {
                return;
            }

            preserveFilePath = PreserveFileName;
            dumpFilePath = DumpFileName;
            settingsFilePath = SettingsFileName;
            Log("ShaderStripMod using current directory for preserve/dump files.");
        }

        private bool TryUseListDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(directory);
                string candidatePreservePath = Path.Combine(directory, PreserveFileName);
                if (!File.Exists(candidatePreservePath))
                {
                    AtomicTextFile.Write(candidatePreservePath,BuildDefaultPreserveFile());
                }

                preserveFilePath = candidatePreservePath;
                dumpFilePath = Path.Combine(directory, DumpFileName);
                settingsFilePath = Path.Combine(directory, SettingsFileName);
                return true;
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod cannot use list directory '" + directory + "': " + ex.Message);
                return false;
            }
        }

        private void LoadExportSettings()
        {
            customMenuEnabled=customMenuPaused=false;customMenuFile=customMenuLogoFile="";customMenuLayout="Cover";customMenuTextColor="Original";customMenuTextScale=1;
            string configuredUiLanguage = "English";
            string configuredMode = "";
            string configuredDirectory = "";
            string configuredBackgroundColor = BackgroundColorGreen;
            string configuredBackgroundMode = BackgroundModeColor;
            string configuredBackgroundImage = "";
            string configuredBackgroundImageLayout = BackgroundImageLayoutCover;
            string configuredPreservedObjectOrder = "BehindHero";
            string configuredPerformanceProfile = PerformanceProfileBalanced;
            string configuredProjectileCaptureMode = ProjectileCaptureOnlyProjectilesTab;
            string configuredProjectilePerformance = ProjectilePerformanceBalanced;
            float configuredApplyStripInterval = DefaultApplyStripIntervalSetting;
            float configuredProxyUpdateInterval = DefaultProxyUpdateIntervalSetting;
            float configuredBrowserRefreshInterval = DefaultTextureBrowserRefreshInterval;
            float configuredHeavyWorkBudgetMs = DefaultHeavyWorkBudgetMs;
            float configuredGcBudgetKb = DefaultGcBudgetKb;
            float configuredProjectileCaptureInterval = DefaultProjectileCaptureInterval;
            float configuredRecentProjectileLifetime = DefaultRecentProjectileLifetime;
            float configuredPreservedSyncInterval = DefaultPreservedSyncIntervalSetting;
            int configuredMaxPreservedRenderers = DefaultMaxPreservedRenderers;
            int configuredMaxProjectileParticleRenderers = DefaultMaxProjectileParticleRenderers;
            int configuredMaxVisibleBrowserEntries = DefaultMaxVisibleBrowserEntries;
            int configuredBrowserListOverscanRows = DefaultBrowserListOverscanRows;
            int configuredMaxCachedRendererPaths = DefaultMaxCachedRendererPaths;
            bool configuredStaggerPerformanceTasks = true;
            bool configuredScanOnlyActiveBrowserTab = true;
            bool configuredPauseBrowserScansWhenClosed = true;
            bool configuredPauseGameWhileBrowserOpen = true;
            bool configuredVisualOverridesEnabled = false;
            bool configuredColorOverridesEnabled = false;
            bool configuredTextureReplacementsEnabled = false;
            bool configuredStaticDisplayEnabled = false;
            bool configuredRendererPathCache = true;
            bool configuredPreserveMatchCache = true;
            bool configuredProjectileCandidateCache = true;
            bool configuredBrowserSnapshotCache = true;
            bool configuredDrawSourceCache = true;
            bool configuredShowPerformanceCounters = true;
            bool configuredShowCacheStats = true;
            bool configuredCollectDetailedMetrics = false;
            bool configuredShowGcAllocStats = true;
            bool configuredVirtualizedBrowserList = true;
            bool configuredCacheWarmupAfterSceneChange = true;
            bool configuredProjectileUseUnscaledTime = true;
            bool hasPerformanceProfile = false;
            bool hasProjectilePerformance = false;
            if (string.IsNullOrEmpty(settingsFilePath))
            {
                InitializeListFiles();
            }

            try
            {
                if (!string.IsNullOrEmpty(settingsFilePath) && File.Exists(settingsFilePath))
                {
                    string[] lines = File.ReadAllLines(settingsFilePath, Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i] != null ? lines[i].Trim() : "";
                        if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//"))
                        {
                            continue;
                        }

                        int separator = line.IndexOf('=');
                        if (separator <= 0)
                        {
                            continue;
                        }

                        string key = line.Substring(0, separator).Trim();
                        if (string.Equals(key,"uiLanguage",System.StringComparison.OrdinalIgnoreCase))
                        { configuredUiLanguage=UiLocalization.NormalizeLanguage(line.Substring(separator+1)); }
                        else if (string.Equals(key,"customMenuEnabled",System.StringComparison.OrdinalIgnoreCase)) bool.TryParse(line.Substring(separator+1).Trim(),out customMenuEnabled);
                        else if (string.Equals(key,"customMenuPaused",System.StringComparison.OrdinalIgnoreCase)) bool.TryParse(line.Substring(separator+1).Trim(),out customMenuPaused);
                        else if (string.Equals(key,"customMenuFile",System.StringComparison.OrdinalIgnoreCase)) customMenuFile=CleanPathSetting(line.Substring(separator+1));
                        else if (string.Equals(key,"customMenuLogoFile",System.StringComparison.OrdinalIgnoreCase)) customMenuLogoFile=CleanPathSetting(line.Substring(separator+1));
                        else if (string.Equals(key,"customMenuLayout",System.StringComparison.OrdinalIgnoreCase)) customMenuLayout=NormalizeBackgroundImageLayout(line.Substring(separator+1).Trim());
                        else if (string.Equals(key,"customMenuTextColor",System.StringComparison.OrdinalIgnoreCase)) customMenuTextColor=NormalizeMenuTextColor(line.Substring(separator+1).Trim());
                        else if (string.Equals(key,"customMenuTextScale",System.StringComparison.OrdinalIgnoreCase))
                        {float parsed;if(float.TryParse(line.Substring(separator+1).Trim(),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out parsed)&&!float.IsNaN(parsed)&&!float.IsInfinity(parsed))customMenuTextScale=Mathf.Clamp(parsed,.75f,1.35f);}
                        else if (string.Equals(key, "exportFolderMode", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredMode = NormalizeExportFolderMode(line.Substring(separator + 1));
                        }
                        else if (string.Equals(key, "exportFolder", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(key, "exportDirectory", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredDirectory = CleanPathSetting(line.Substring(separator + 1));
                        }
                        else if (string.Equals(key, "backgroundColor", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredBackgroundColor = NormalizeBackgroundColorName(line.Substring(separator + 1));
                        }
                        else if (string.Equals(key, "backgroundMode", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredBackgroundMode = NormalizeBackgroundMode(line.Substring(separator + 1));
                        }
                        else if (string.Equals(key, "backgroundImage", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredBackgroundImage = CleanPathSetting(line.Substring(separator + 1));
                        }
                        else if (string.Equals(key, "backgroundImageLayout", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredBackgroundImageLayout = NormalizeBackgroundImageLayout(line.Substring(separator + 1));
                        }
                        else if (string.Equals(key, "preservedObjectOrder", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredPreservedObjectOrder = NormalizePreservedObjectOrder(line.Substring(separator + 1));
                        }
                        else if (string.Equals(key, "performanceProfile", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredPerformanceProfile = NormalizePerformanceProfile(line.Substring(separator + 1));
                            hasPerformanceProfile = true;
                        }
                        else if (string.Equals(key, "applyStripInterval", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredApplyStripInterval = ClampPerformanceInterval(ParseSettingsFloat(line.Substring(separator + 1), configuredApplyStripInterval));
                        }
                        else if (string.Equals(key, "proxyUpdateInterval", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredProxyUpdateInterval = ClampPerformanceInterval(ParseSettingsFloat(line.Substring(separator + 1), configuredProxyUpdateInterval));
                        }
                        else if (string.Equals(key, "browserRefreshInterval", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(key, "textureBrowserRefreshInterval", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredBrowserRefreshInterval = ClampPerformanceInterval(ParseSettingsFloat(line.Substring(separator + 1), configuredBrowserRefreshInterval));
                        }
                        else if (string.Equals(key, "totalHeavyWorkBudgetMs", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(key, "heavyWorkBudgetMs", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredHeavyWorkBudgetMs = ClampHeavyWorkBudget(ParseSettingsFloat(line.Substring(separator + 1), configuredHeavyWorkBudgetMs));
                        }
                        else if (string.Equals(key, "totalGcBudgetPerFrameKb", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(key, "gcBudgetKb", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredGcBudgetKb = ClampGcBudget(ParseSettingsFloat(line.Substring(separator + 1), configuredGcBudgetKb));
                        }
                        else if (string.Equals(key, "staggerPerformanceTasks", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredStaggerPerformanceTasks = ParseSettingsBool(line.Substring(separator + 1), configuredStaggerPerformanceTasks);
                        }
                        else if (string.Equals(key, "scanOnlyActiveBrowserTab", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredScanOnlyActiveBrowserTab = ParseSettingsBool(line.Substring(separator + 1), configuredScanOnlyActiveBrowserTab);
                        }
                        else if (string.Equals(key, "pauseBrowserScansWhenClosed", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredPauseBrowserScansWhenClosed = ParseSettingsBool(line.Substring(separator + 1), configuredPauseBrowserScansWhenClosed);
                        }
                        else if (string.Equals(key, "pauseGameWhileBrowserOpen", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredPauseGameWhileBrowserOpen = ParseSettingsBool(line.Substring(separator + 1), configuredPauseGameWhileBrowserOpen);
                        }
                        else if (string.Equals(key, "visualOverridesEnabled", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredVisualOverridesEnabled = ParseSettingsBool(line.Substring(separator + 1), configuredVisualOverridesEnabled);
                        }
                        else if (string.Equals(key, "colorOverridesEnabled", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredColorOverridesEnabled = ParseSettingsBool(line.Substring(separator + 1), configuredColorOverridesEnabled);
                        }
                        else if (string.Equals(key, "textureReplacementsEnabled", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredTextureReplacementsEnabled = ParseSettingsBool(line.Substring(separator + 1), configuredTextureReplacementsEnabled);
                        }
                        else if (string.Equals(key, "staticDisplayEnabled", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredStaticDisplayEnabled = ParseSettingsBool(line.Substring(separator + 1), configuredStaticDisplayEnabled);
                        }
                        else if (string.Equals(key, "rendererPathCache", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredRendererPathCache = ParseSettingsBool(line.Substring(separator + 1), configuredRendererPathCache);
                        }
                        else if (string.Equals(key, "preserveMatchCache", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredPreserveMatchCache = ParseSettingsBool(line.Substring(separator + 1), configuredPreserveMatchCache);
                        }
                        else if (string.Equals(key, "projectileCandidateCache", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredProjectileCandidateCache = ParseSettingsBool(line.Substring(separator + 1), configuredProjectileCandidateCache);
                        }
                        else if (string.Equals(key, "browserSnapshotCache", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredBrowserSnapshotCache = ParseSettingsBool(line.Substring(separator + 1), configuredBrowserSnapshotCache);
                        }
                        else if (string.Equals(key, "drawSourceCache", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredDrawSourceCache = ParseSettingsBool(line.Substring(separator + 1), configuredDrawSourceCache);
                        }
                        else if (string.Equals(key, "showPerformanceCounters", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredShowPerformanceCounters = ParseSettingsBool(line.Substring(separator + 1), configuredShowPerformanceCounters);
                        }
                        else if (string.Equals(key, "showCacheStats", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredShowCacheStats = ParseSettingsBool(line.Substring(separator + 1), configuredShowCacheStats);
                        }
                        else if (string.Equals(key, "collectDetailedMetrics", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredCollectDetailedMetrics = ParseSettingsBool(line.Substring(separator + 1), configuredCollectDetailedMetrics);
                        }
                        else if (string.Equals(key, "showGcAllocStats", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredShowGcAllocStats = ParseSettingsBool(line.Substring(separator + 1), configuredShowGcAllocStats);
                        }
                        else if (string.Equals(key, "virtualizedBrowserList", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredVirtualizedBrowserList = ParseSettingsBool(line.Substring(separator + 1), configuredVirtualizedBrowserList);
                        }
                        else if (string.Equals(key, "cacheWarmupAfterSceneChange", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredCacheWarmupAfterSceneChange = ParseSettingsBool(line.Substring(separator + 1), configuredCacheWarmupAfterSceneChange);
                        }
                        else if (string.Equals(key, "projectileUseUnscaledTime", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(key, "projectileTimeSource", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredProjectileUseUnscaledTime = ParseProjectileTimeSource(line.Substring(separator + 1), configuredProjectileUseUnscaledTime);
                        }
                        else if (string.Equals(key, "maxVisibleBrowserEntries", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredMaxVisibleBrowserEntries = ClampVisibleBrowserEntries(ParseSettingsInt(line.Substring(separator + 1), configuredMaxVisibleBrowserEntries));
                        }
                        else if (string.Equals(key, "browserListOverscanRows", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredBrowserListOverscanRows = ClampBrowserOverscan(ParseSettingsInt(line.Substring(separator + 1), configuredBrowserListOverscanRows));
                        }
                        else if (string.Equals(key, "maxCachedRendererPaths", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(key, "maxCachedRenderers", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredMaxCachedRendererPaths = ClampCachedRendererPaths(ParseSettingsInt(line.Substring(separator + 1), configuredMaxCachedRendererPaths));
                        }
                        else if (string.Equals(key, "projectileCaptureMode", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredProjectileCaptureMode = NormalizeProjectileCaptureMode(line.Substring(separator + 1));
                        }
                        else if (string.Equals(key, "projectilePerformance", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredProjectilePerformance = NormalizeProjectilePerformanceMode(line.Substring(separator + 1));
                            hasProjectilePerformance = true;
                        }
                        else if (string.Equals(key, "projectileCaptureInterval", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredProjectileCaptureInterval = ClampProjectilePerformanceFloat(ParseSettingsFloat(line.Substring(separator + 1), configuredProjectileCaptureInterval));
                        }
                        else if (string.Equals(key, "recentProjectileLifetime", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredRecentProjectileLifetime = ClampProjectilePerformanceFloat(ParseSettingsFloat(line.Substring(separator + 1), configuredRecentProjectileLifetime));
                        }
                        else if (string.Equals(key, "preservedSyncInterval", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredPreservedSyncInterval = ClampProjectilePerformanceFloat(ParseSettingsFloat(line.Substring(separator + 1), configuredPreservedSyncInterval));
                        }
                        else if (string.Equals(key, "maxPreservedRenderers", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredMaxPreservedRenderers = ClampProjectilePerformanceInt(ParseSettingsInt(line.Substring(separator + 1), configuredMaxPreservedRenderers));
                        }
                        else if (string.Equals(key, "maxProjectileParticleRenderers", System.StringComparison.OrdinalIgnoreCase))
                        {
                            configuredMaxProjectileParticleRenderers = ClampProjectilePerformanceInt(ParseSettingsInt(line.Substring(separator + 1), configuredMaxProjectileParticleRenderers));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod settings read failed: " + ex.Message);
            }

            if (string.IsNullOrEmpty(configuredMode))
            {
                configuredMode = GetExportModeForLegacyDirectory(configuredDirectory);
            }

            exportFolderMode = configuredMode;
            if (string.Equals(exportFolderMode, ExportModeCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                exportDirectory = !string.IsNullOrEmpty(configuredDirectory) ? configuredDirectory : GetDefaultExportDirectory();
            }
            else
            {
                RefreshExportDirectoryFromMode();
            }

            exportDirectoryInput = exportDirectory;
            backgroundColorName = NormalizeBackgroundColorName(configuredBackgroundColor);
            stripBackground = GetStripBackgroundColor(backgroundColorName);
            backgroundMode = NormalizeBackgroundMode(configuredBackgroundMode);
            backgroundImageRelativePath = configuredBackgroundImage;
            uiLanguage=UiLocalization.NormalizeLanguage(configuredUiLanguage);
            backgroundImageLayout = NormalizeBackgroundImageLayout(configuredBackgroundImageLayout);
            preservedObjectOrder = NormalizePreservedObjectOrder(configuredPreservedObjectOrder);
            InitializeBackgroundImagePaths();InitializeCustomMenuPaths();
            pauseGameWhileBrowserOpen = configuredPauseGameWhileBrowserOpen;
            visualOverridesEnabled = configuredVisualOverridesEnabled;
            colorOverridesEnabled = configuredColorOverridesEnabled;
            textureReplacementsEnabled = configuredTextureReplacementsEnabled;
            staticDisplayEnabled = configuredStaticDisplayEnabled;
            bool migratedProjectileCustom = !hasPerformanceProfile && hasProjectilePerformance && string.Equals(configuredProjectilePerformance, ProjectilePerformanceCustom, System.StringComparison.OrdinalIgnoreCase);
            performanceProfile = hasPerformanceProfile ? NormalizePerformanceProfile(configuredPerformanceProfile) : (migratedProjectileCustom ? PerformanceProfileCustom : PerformanceProfileBalanced);
            if (string.Equals(performanceProfile, PerformanceProfileCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                projectilePerformanceMode = migratedProjectileCustom ? ProjectilePerformanceCustom : NormalizeProjectilePerformanceMode(configuredProjectilePerformance);
                if (!string.Equals(projectilePerformanceMode, ProjectilePerformanceCustom, System.StringComparison.OrdinalIgnoreCase))
                {
                    projectilePerformanceMode = ProjectilePerformanceCustom;
                }

                SetPerformanceRuntimeValues(
                    configuredApplyStripInterval,
                    configuredProxyUpdateInterval,
                    configuredBrowserRefreshInterval,
                    configuredHeavyWorkBudgetMs,
                    configuredGcBudgetKb,
                    configuredMaxVisibleBrowserEntries,
                    configuredBrowserListOverscanRows,
                    configuredMaxCachedRendererPaths,
                    configuredStaggerPerformanceTasks,
                    configuredScanOnlyActiveBrowserTab,
                    configuredPauseBrowserScansWhenClosed,
                    configuredRendererPathCache,
                    configuredPreserveMatchCache,
                    configuredProjectileCandidateCache,
                    configuredBrowserSnapshotCache,
                    configuredDrawSourceCache,
                    configuredShowPerformanceCounters,
                    configuredShowCacheStats,
                    configuredCollectDetailedMetrics,
                    configuredShowGcAllocStats,
                    configuredVirtualizedBrowserList,
                    configuredCacheWarmupAfterSceneChange,
                    configuredProjectileUseUnscaledTime,
                    configuredProjectileCaptureMode);
                SetProjectilePerformanceValues(
                    configuredProjectileCaptureInterval,
                    configuredRecentProjectileLifetime,
                    configuredPreservedSyncInterval,
                    configuredMaxPreservedRenderers,
                    configuredMaxProjectileParticleRenderers);
            }
            else
            {
                ApplyPerformanceProfilePreset(performanceProfile);
            }

            UpdateProjectilePerformanceInputs();
            UpdatePerformanceInputs();
            SaveExportSettings();
        }

        private void SaveExportSettings()
        {
            if (string.IsNullOrEmpty(settingsFilePath))
            {
                InitializeListFiles();
            }

            if (string.IsNullOrEmpty(exportDirectory))
            {
                RefreshExportDirectoryFromMode();
            }

            exportDirectoryInput = exportDirectory;

            try
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("# ShaderStripMod settings");
                builder.AppendLine("# exportFolderMode can be Downloads, Mod, or Custom.");
                builder.AppendLine("# Downloads is resolved from the current Windows user at game startup.");
                builder.AppendLine("# backgroundColor can be Green, Blue, Red, Yellow, Cyan, Magenta, Orange, White, or Black.");
                builder.AppendLine("# backgroundMode can be Color or Image. Images are loaded from background-images beside the mod DLL.");
                builder.AppendLine("# backgroundImageLayout can be Cover (crop), Contain (borders), or Stretch.");
                builder.AppendLine("# pauseGameWhileBrowserOpen controls whether F9 pauses Time.timeScale while the browser is open during gameplay.");
                builder.AppendLine("# Visual override switches are reserved for the staged visual-rules runtime. Parts 1-2 only load and diagnose rules; they never mutate renderers.");
                builder.AppendLine("# performanceProfile can be Quality, Balanced, Performance, Ultra, or Custom.");
                builder.AppendLine("# projectileCaptureMode can be OnlyProjectilesTab, BrowserOpen, Always, or Manual.");
                builder.AppendLine("# projectilePerformance can be Safe, Balanced, Aggressive, or Custom.");
                builder.AppendLine("# projectile custom values are clamped to 0..999. Interval 0 means every frame and can cause severe lag.");
                builder.AppendLine("# Performance intervals use 0 for every frame; hard caps still apply to avoid runaway memory use.");
                builder.AppendLine("exportFolderMode=" + exportFolderMode);
                if (string.Equals(exportFolderMode, ExportModeCustom, System.StringComparison.OrdinalIgnoreCase))
                {
                    builder.AppendLine("exportFolder=" + exportDirectory);
                }
                else
                {
                    builder.AppendLine("# resolvedExportFolder=" + exportDirectory);
                }

                builder.AppendLine("# Custom title menu media lives in menu-media; gameplay background settings remain separate.");
                builder.AppendLine("customMenuEnabled="+customMenuEnabled);
                builder.AppendLine("customMenuPaused="+customMenuPaused);
                builder.AppendLine("customMenuFile="+(customMenuFile??""));
                builder.AppendLine("customMenuLogoFile="+(customMenuLogoFile??""));
                builder.AppendLine("customMenuLayout="+customMenuLayout);
                builder.AppendLine("customMenuTextColor="+customMenuTextColor);
                builder.AppendLine("customMenuTextScale="+customMenuTextScale.ToString(System.Globalization.CultureInfo.InvariantCulture));
                builder.AppendLine("uiLanguage=" + UiLocalization.NormalizeLanguage(uiLanguage));
                builder.AppendLine("backgroundColor=" + NormalizeBackgroundColorName(backgroundColorName));
                builder.AppendLine("backgroundMode=" + NormalizeBackgroundMode(backgroundMode));
                builder.AppendLine("backgroundImage=" + (backgroundImageRelativePath ?? ""));
                builder.AppendLine("backgroundImageLayout=" + NormalizeBackgroundImageLayout(backgroundImageLayout));
                builder.AppendLine("preservedObjectOrder=" + NormalizePreservedObjectOrder(preservedObjectOrder));
                builder.AppendLine("pauseGameWhileBrowserOpen=" + FormatSettingsBool(pauseGameWhileBrowserOpen));
                builder.AppendLine("visualOverridesEnabled=" + FormatSettingsBool(visualOverridesEnabled));
                builder.AppendLine("colorOverridesEnabled=" + FormatSettingsBool(colorOverridesEnabled));
                builder.AppendLine("textureReplacementsEnabled=" + FormatSettingsBool(textureReplacementsEnabled));
                builder.AppendLine("staticDisplayEnabled=" + FormatSettingsBool(staticDisplayEnabled));
                builder.AppendLine("performanceProfile=" + NormalizePerformanceProfile(performanceProfile));
                builder.AppendLine("applyStripInterval=" + FormatSettingsFloat(applyStripIntervalSetting));
                builder.AppendLine("proxyUpdateInterval=" + FormatSettingsFloat(proxyUpdateIntervalSetting));
                builder.AppendLine("browserRefreshInterval=" + FormatSettingsFloat(textureBrowserRefreshInterval));
                builder.AppendLine("totalHeavyWorkBudgetMs=" + FormatSettingsFloat(heavyWorkBudgetMs));
                builder.AppendLine("totalGcBudgetPerFrameKb=" + FormatSettingsFloat(gcBudgetKb));
                builder.AppendLine("staggerPerformanceTasks=" + FormatSettingsBool(staggerPerformanceTasks));
                builder.AppendLine("scanOnlyActiveBrowserTab=" + FormatSettingsBool(scanOnlyActiveBrowserTab));
                builder.AppendLine("pauseBrowserScansWhenClosed=" + FormatSettingsBool(pauseBrowserScansWhenClosed));
                builder.AppendLine("rendererPathCache=" + FormatSettingsBool(rendererPathCacheEnabled));
                builder.AppendLine("preserveMatchCache=" + FormatSettingsBool(preserveMatchCacheEnabled));
                builder.AppendLine("projectileCandidateCache=" + FormatSettingsBool(projectileCandidateCacheEnabled));
                builder.AppendLine("browserSnapshotCache=" + FormatSettingsBool(browserSnapshotCacheEnabled));
                builder.AppendLine("drawSourceCache=" + FormatSettingsBool(drawSourceCacheEnabled));
                builder.AppendLine("showPerformanceCounters=" + FormatSettingsBool(showPerformanceCounters));
                builder.AppendLine("showCacheStats=" + FormatSettingsBool(showCacheStats));
                builder.AppendLine("collectDetailedMetrics=" + FormatSettingsBool(collectDetailedMetrics));
                builder.AppendLine("showGcAllocStats=" + FormatSettingsBool(showGcAllocStats));
                builder.AppendLine("virtualizedBrowserList=" + FormatSettingsBool(virtualizedBrowserList));
                builder.AppendLine("cacheWarmupAfterSceneChange=" + FormatSettingsBool(cacheWarmupAfterSceneChange));
                builder.AppendLine("projectileUseUnscaledTime=" + FormatSettingsBool(projectileUseUnscaledTime));
                builder.AppendLine("maxVisibleBrowserEntries=" + maxVisibleBrowserEntries);
                builder.AppendLine("browserListOverscanRows=" + browserListOverscanRows);
                builder.AppendLine("maxCachedRendererPaths=" + maxCachedRendererPaths);
                builder.AppendLine("projectileCaptureMode=" + NormalizeProjectileCaptureMode(projectileCaptureMode));
                builder.AppendLine("projectilePerformance=" + NormalizeProjectilePerformanceMode(projectilePerformanceMode));
                builder.AppendLine("projectileCaptureInterval=" + FormatSettingsFloat(projectileCaptureInterval));
                builder.AppendLine("recentProjectileLifetime=" + FormatSettingsFloat(recentProjectileLifetime));
                builder.AppendLine("preservedSyncInterval=" + FormatSettingsFloat(preservedSyncIntervalSetting));
                builder.AppendLine("maxPreservedRenderers=" + maxPreservedRenderers);
                builder.AppendLine("maxProjectileParticleRenderers=" + maxProjectileParticleRenderers);
                AtomicTextFile.Write(settingsFilePath,builder.ToString());
            }
            catch (System.Exception ex)
            {
                Log("ShaderStripMod settings save failed: " + ex.Message);
            }
        }

        private string NormalizeBackgroundColorName(string colorName)
        {
            string cleaned = CleanUnityName(colorName);
            for (int i = 0; i < BackgroundColorNames.Length; i++)
            {
                if (string.Equals(cleaned, BackgroundColorNames[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return BackgroundColorNames[i];
                }
            }

            return BackgroundColorGreen;
        }

        private Color GetStripBackgroundColor(string colorName)
        {
            string normalized = NormalizeBackgroundColorName(colorName);
            if (string.Equals(normalized, BackgroundColorBlue, System.StringComparison.OrdinalIgnoreCase))
            {
                return new Color(0f, 0.25f, 1f, 1f);
            }

            if (string.Equals(normalized, BackgroundColorRed, System.StringComparison.OrdinalIgnoreCase))
            {
                return new Color(1f, 0f, 0f, 1f);
            }

            if (string.Equals(normalized, BackgroundColorYellow, System.StringComparison.OrdinalIgnoreCase))
            {
                return new Color(1f, 1f, 0f, 1f);
            }

            if (string.Equals(normalized, BackgroundColorCyan, System.StringComparison.OrdinalIgnoreCase))
            {
                return new Color(0f, 1f, 1f, 1f);
            }

            if (string.Equals(normalized, BackgroundColorMagenta, System.StringComparison.OrdinalIgnoreCase))
            {
                return new Color(1f, 0f, 1f, 1f);
            }

            if (string.Equals(normalized, BackgroundColorOrange, System.StringComparison.OrdinalIgnoreCase))
            {
                return new Color(1f, 0.45f, 0f, 1f);
            }

            if (string.Equals(normalized, BackgroundColorWhite, System.StringComparison.OrdinalIgnoreCase))
            {
                return Color.white;
            }

            if (string.Equals(normalized, BackgroundColorBlack, System.StringComparison.OrdinalIgnoreCase))
            {
                return Color.black;
            }

            return DefaultStripBackground;
        }

        private void SetStripBackgroundColor(string colorName)
        {
            string normalized = NormalizeBackgroundColorName(colorName);
            backgroundColorName = normalized;
            stripBackground = GetStripBackgroundColor(normalized);
            backgroundMode = BackgroundModeColor;
            ReleaseImageBackground();
            SaveExportSettings();
            ApplySelectedBackgroundColor();
            lastSettingsMessage = "Background color set to " + normalized + ".";
        }

        private void ApplySelectedBackgroundColor()
        {
            if (!visualsActive)
            {
                return;
            }

            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam != null && strippedBackgrounds.ContainsKey(cam))
                {
                    cam.backgroundColor = stripBackground;
                }
            }

            ApplyStrip();
        }

        private string NormalizePerformanceProfile(string profile)
        {
            string cleaned = CleanUnityName(profile);
            for (int i = 0; i < PerformanceProfileModes.Length; i++)
            {
                if (string.Equals(cleaned, PerformanceProfileModes[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return PerformanceProfileModes[i];
                }
            }

            return PerformanceProfileBalanced;
        }

        private string NormalizeProjectileCaptureMode(string mode)
        {
            string cleaned = CleanUnityName(mode);
            for (int i = 0; i < ProjectileCaptureModes.Length; i++)
            {
                if (string.Equals(cleaned, ProjectileCaptureModes[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return ProjectileCaptureModes[i];
                }
            }

            if (string.Equals(cleaned, "Only Projectiles Tab", System.StringComparison.OrdinalIgnoreCase))
            {
                return ProjectileCaptureOnlyProjectilesTab;
            }

            if (string.Equals(cleaned, "While Browser Open", System.StringComparison.OrdinalIgnoreCase))
            {
                return ProjectileCaptureBrowserOpen;
            }

            return ProjectileCaptureOnlyProjectilesTab;
        }

        private void ApplyPerformanceProfilePreset(string profile)
        {
            string normalized = NormalizePerformanceProfile(profile);
            if (string.Equals(normalized, PerformanceProfileQuality, System.StringComparison.OrdinalIgnoreCase))
            {
                performanceProfile = PerformanceProfileQuality;
                SetPerformanceRuntimeValues(1.0f, 0f, 0.25f, 4.0f, 32.0f, 1000, 6, 6000, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, ProjectileCaptureBrowserOpen);
                projectilePerformanceMode = ProjectilePerformanceAggressive;
                SetProjectilePerformanceValues(0.06f, 8f, 0.06f, 320, 120);
                return;
            }

            if (string.Equals(normalized, PerformanceProfilePerformance, System.StringComparison.OrdinalIgnoreCase))
            {
                performanceProfile = PerformanceProfilePerformance;
                SetPerformanceRuntimeValues(3.0f, 0.12f, 1.0f, 1.0f, 2.0f, 350, 3, 3000, true, true, true, true, true, true, true, true, false, true, false, true, true, true, true, ProjectileCaptureOnlyProjectilesTab);
                projectilePerformanceMode = ProjectilePerformanceSafe;
                SetProjectilePerformanceValues(0.30f, 2f, 0.25f, 100, 24);
                return;
            }

            if (string.Equals(normalized, PerformanceProfileUltra, System.StringComparison.OrdinalIgnoreCase))
            {
                performanceProfile = PerformanceProfileUltra;
                SetPerformanceRuntimeValues(5.0f, 0.25f, 2.0f, 0.5f, 1.0f, 200, 2, 1500, true, true, true, true, true, true, true, true, false, false, false, false, true, true, true, ProjectileCaptureManual);
                projectilePerformanceMode = ProjectilePerformanceSafe;
                SetProjectilePerformanceValues(0.50f, 1.5f, 0.50f, 60, 12);
                return;
            }

            if (string.Equals(normalized, PerformanceProfileCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                performanceProfile = PerformanceProfileCustom;
                projectilePerformanceMode = ProjectilePerformanceCustom;
                return;
            }

            performanceProfile = PerformanceProfileBalanced;
            SetPerformanceRuntimeValues(DefaultApplyStripIntervalSetting, DefaultProxyUpdateIntervalSetting, DefaultTextureBrowserRefreshInterval, DefaultHeavyWorkBudgetMs, DefaultGcBudgetKb, DefaultMaxVisibleBrowserEntries, DefaultBrowserListOverscanRows, DefaultMaxCachedRendererPaths, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, ProjectileCaptureOnlyProjectilesTab);
            projectilePerformanceMode = ProjectilePerformanceBalanced;
            SetProjectilePerformanceValues(DefaultProjectileCaptureInterval, DefaultRecentProjectileLifetime, DefaultPreservedSyncIntervalSetting, DefaultMaxPreservedRenderers, DefaultMaxProjectileParticleRenderers);
        }

        private void SetPerformanceRuntimeValues(float applyInterval, float proxyInterval, float browserRefreshInterval, float heavyBudget, float gcBudget, int visibleEntries, int overscanRows, int cachedPaths, bool staggerTasks, bool scanActiveTabOnly, bool pauseBrowserClosed, bool rendererPathCache, bool preserveCache, bool projectileCache, bool browserSnapshotCache, bool drawCache, bool counters, bool cacheStats, bool detailedMetrics, bool gcStats, bool virtualizedList, bool warmupAfterSceneChange, bool projectileUnscaledTime, string captureMode)
        {
            bool clearPathCache = rendererPathCacheEnabled != rendererPathCache || cachedPaths < maxCachedRendererPaths;
            applyStripIntervalSetting = ClampPerformanceInterval(applyInterval);
            proxyUpdateIntervalSetting = ClampPerformanceInterval(proxyInterval);
            textureBrowserRefreshInterval = ClampPerformanceInterval(browserRefreshInterval);
            heavyWorkBudgetMs = ClampHeavyWorkBudget(heavyBudget);
            gcBudgetKb = ClampGcBudget(gcBudget);
            maxVisibleBrowserEntries = ClampVisibleBrowserEntries(visibleEntries);
            browserListOverscanRows = ClampBrowserOverscan(overscanRows);
            maxCachedRendererPaths = ClampCachedRendererPaths(cachedPaths);
            staggerPerformanceTasks = staggerTasks;
            scanOnlyActiveBrowserTab = scanActiveTabOnly;
            pauseBrowserScansWhenClosed = pauseBrowserClosed;
            rendererPathCacheEnabled = rendererPathCache;
            preserveMatchCacheEnabled = preserveCache;
            projectileCandidateCacheEnabled = projectileCache;
            browserSnapshotCacheEnabled = browserSnapshotCache;
            drawSourceCacheEnabled = drawCache;
            showPerformanceCounters = counters;
            showCacheStats = cacheStats;
            collectDetailedMetrics = detailedMetrics;
            showGcAllocStats = gcStats;
            virtualizedBrowserList = virtualizedList;
            cacheWarmupAfterSceneChange = warmupAfterSceneChange;
            projectileUseUnscaledTime = projectileUnscaledTime;
            projectileCaptureMode = NormalizeProjectileCaptureMode(captureMode);

            if (clearPathCache)
            {
                ClearRuntimeCaches();
            }
        }

        private void SetPerformanceProfile(string profile)
        {
            string normalized = NormalizePerformanceProfile(profile);
            if (string.Equals(normalized, PerformanceProfileCustom, System.StringComparison.OrdinalIgnoreCase))
            {
                performanceProfile = PerformanceProfileCustom;
                projectilePerformanceMode = ProjectilePerformanceCustom;
                UpdatePerformanceInputs();
                UpdateProjectilePerformanceInputs();
                SaveExportSettings();
                lastPerformanceMessage = "Custom performance selected. Edit values, then press Apply Custom.";
                return;
            }

            ApplyPerformanceProfilePreset(normalized);
            UpdatePerformanceInputs();
            UpdateProjectilePerformanceInputs();
            SaveExportSettings();
            RefreshAfterPerformanceChange();
            lastPerformanceMessage = "Performance profile set to " + performanceProfile + ".";
        }

        private void CopyPerformanceProfileToCustom(string profile)
        {
            ApplyPerformanceProfilePreset(profile);
            performanceProfile = PerformanceProfileCustom;
            projectilePerformanceMode = ProjectilePerformanceCustom;
            UpdatePerformanceInputs();
            UpdateProjectilePerformanceInputs();
            SaveExportSettings();
            RefreshAfterPerformanceChange();
            lastPerformanceMessage = "Copied " + NormalizePerformanceProfile(profile) + " profile to Custom.";
        }

        private void ResetPerformanceToBalanced()
        {
            ApplyPerformanceProfilePreset(PerformanceProfileBalanced);
            UpdatePerformanceInputs();
            UpdateProjectilePerformanceInputs();
            SaveExportSettings();
            RefreshAfterPerformanceChange();
            lastPerformanceMessage = "Performance reset to Balanced.";
        }

        private void ApplyCustomPerformanceSettings()
        {
            float applyInterval;
            float proxyInterval;
            float browserRefreshInterval;
            float heavyBudget;
            float gcBudget;
            int visibleEntries;
            int overscanRows;
            int cachedPaths;
            float captureInterval;
            float lifetime;
            float preservedSyncInterval;
            int preservedRenderers;
            int projectileParticleRenderers;

            if (!TryParseInputFloat(applyStripIntervalInput, out applyInterval) ||
                !TryParseInputFloat(proxyUpdateIntervalInput, out proxyInterval) ||
                !TryParseInputFloat(textureBrowserRefreshIntervalInput, out browserRefreshInterval) ||
                !TryParseInputFloat(heavyWorkBudgetInput, out heavyBudget) ||
                !TryParseInputFloat(gcBudgetInput, out gcBudget) ||
                !TryParseInputInt(maxVisibleBrowserEntriesInput, out visibleEntries) ||
                !TryParseInputInt(browserListOverscanRowsInput, out overscanRows) ||
                !TryParseInputInt(maxCachedRendererPathsInput, out cachedPaths) ||
                !TryReadProjectilePerformanceInputs(out captureInterval, out lifetime, out preservedSyncInterval, out preservedRenderers, out projectileParticleRenderers))
            {
                lastPerformanceMessage = "Custom performance rejected: numbers only.";
                return;
            }

            performanceProfile = PerformanceProfileCustom;
            projectilePerformanceMode = ProjectilePerformanceCustom;
            SetPerformanceRuntimeValues(
                applyInterval,
                proxyInterval,
                browserRefreshInterval,
                heavyBudget,
                gcBudget,
                visibleEntries,
                overscanRows,
                cachedPaths,
                staggerPerformanceTasks,
                scanOnlyActiveBrowserTab,
                pauseBrowserScansWhenClosed,
                rendererPathCacheEnabled,
                preserveMatchCacheEnabled,
                projectileCandidateCacheEnabled,
                browserSnapshotCacheEnabled,
                drawSourceCacheEnabled,
                showPerformanceCounters,
                showCacheStats,
                collectDetailedMetrics,
                showGcAllocStats,
                virtualizedBrowserList,
                cacheWarmupAfterSceneChange,
                projectileUseUnscaledTime,
                projectileCaptureMode);
            SetProjectilePerformanceValues(captureInterval, lifetime, preservedSyncInterval, preservedRenderers, projectileParticleRenderers);
            UpdatePerformanceInputs();
            UpdateProjectilePerformanceInputs();
            SaveExportSettings();
            RefreshAfterPerformanceChange();
            lastPerformanceMessage = "Custom performance applied.";
        }

        private void UpdatePerformanceInputs()
        {
            applyStripIntervalInput = FormatSettingsFloat(applyStripIntervalSetting);
            proxyUpdateIntervalInput = FormatSettingsFloat(proxyUpdateIntervalSetting);
            textureBrowserRefreshIntervalInput = FormatSettingsFloat(textureBrowserRefreshInterval);
            heavyWorkBudgetInput = FormatSettingsFloat(heavyWorkBudgetMs);
            gcBudgetInput = FormatSettingsFloat(gcBudgetKb);
            maxVisibleBrowserEntriesInput = maxVisibleBrowserEntries.ToString();
            browserListOverscanRowsInput = browserListOverscanRows.ToString();
            maxCachedRendererPathsInput = maxCachedRendererPaths.ToString();
        }

        private void RefreshAfterPerformanceChange()
        {
            nextRecentProjectileCaptureTime = 0f;
            ClearRuntimeCaches();
            MarkPreservedRendererCacheDirty();
            preserveDrawSyncRequested = true;
            if (visualsActive)
            {
                ApplyStrip();
            }
        }

    }
}

