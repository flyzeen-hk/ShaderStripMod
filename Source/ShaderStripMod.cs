using System.Collections.Generic;
using System.IO;
using System.Text;
using Modding;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod : Mod, ITogglableMod
    {
        private static readonly Color DefaultStripBackground = new Color(0f, 1f, 0f, 1f);
        private const string PreserveFileName = "ShaderStripMod.preserve.txt";
        private const string DumpFileName = "ShaderStripMod.sprite-dump.txt";
        private const string SettingsFileName = "ShaderStripMod.settings.txt";
        private const string ExportFolderName = "ShaderStripMod";
        private const string ExportModeDownloads = "Downloads";
        private const string ExportModeMod = "Mod";
        private const string ExportModeCustom = "Custom";
        private const string BackgroundColorGreen = "Green";
        private const string BackgroundColorBlue = "Blue";
        private const string BackgroundColorRed = "Red";
        private const string BackgroundColorYellow = "Yellow";
        private const string BackgroundColorCyan = "Cyan";
        private const string BackgroundColorMagenta = "Magenta";
        private const string BackgroundColorOrange = "Orange";
        private const string BackgroundColorWhite = "White";
        private const string BackgroundColorBlack = "Black";
        private const string BackgroundModeColor = "Color";
        private const string BackgroundModeImage = "Image";
        private const string BackgroundImageFolderName = "background-images";
        private const string BackgroundImageLayoutCover = "Cover";
        private const string BackgroundImageLayoutContain = "Contain";
        private const string BackgroundImageLayoutStretch = "Stretch";
        private const string ProjectilePerformanceSafe = "Safe";
        private const string ProjectilePerformanceBalanced = "Balanced";
        private const string ProjectilePerformanceAggressive = "Aggressive";
        private const string ProjectilePerformanceCustom = "Custom";
        private const int TextureBrowserWindowId = 840621;
        private const int TextureBrowserInfoTab = 0;
        private const int TextureBrowserPreviewTab = 1;
        private const int TextureBrowserRulesTab = 2;
        private const int TextureBrowserExportTab = 3;
        private const int TextureBrowserSettingsTab = 4;
        private const int TextureBrowserPerformanceTab = 5;
        private const float TextureBrowserBaseWidth = 940f;
        private const float TextureBrowserBaseHeight = 620f;
        private const float TextureBrowserMinScale = 0.35f;
        private const float TextureBrowserMaxScale = 1.35f;
        private const string PerformanceProfileQuality = "Quality";
        private const string PerformanceProfileBalanced = "Balanced";
        private const string PerformanceProfilePerformance = "Performance";
        private const string PerformanceProfileUltra = "Ultra";
        private const string PerformanceProfileCustom = "Custom";
        private const string ProjectileCaptureOnlyProjectilesTab = "OnlyProjectilesTab";
        private const string ProjectileCaptureBrowserOpen = "BrowserOpen";
        private const string ProjectileCaptureAlways = "Always";
        private const string ProjectileCaptureManual = "Manual";
        private const float DefaultRecentProjectileLifetime = 3.0f;
        private const float DefaultProjectileCaptureInterval = 0.12f;
        private const float DefaultPreservedSyncIntervalSetting = 0.12f;
        private const float DefaultApplyStripIntervalSetting = 2.0f;
        private const float DefaultProxyUpdateIntervalSetting = 0.0f;
        private const float DefaultTextureBrowserRefreshInterval = 0.5f;
        private const float DefaultHeavyWorkBudgetMs = 2.0f;
        private const float DefaultGcBudgetKb = 4.0f;
        private const float MinProjectilePerformanceValue = 0f;
        private const float MaxProjectilePerformanceValue = 999f;
        private const float MaxPerformanceIntervalValue = 999f;
        private const float MaxHeavyWorkBudgetMs = 16f;
        private const float MaxGcBudgetKb = 256f;
        private const int DefaultMaxVisibleBrowserEntries = 500;
        private const int DefaultBrowserListOverscanRows = 4;
        private const int MaxVisibleBrowserEntriesHardCap = 5000;
        private const int MaxBrowserListOverscanRows = 64;
        private const int DefaultMaxCachedRendererPaths = 3000;
        private const int MaxCachedRendererPathsHardCap = 50000;
        private const int PerformanceMetricApplyStrip = 0;
        private const int PerformanceMetricDrawSync = 1;
        private const int PerformanceMetricProjectileScan = 2;
        private const int PerformanceMetricProxyUpdate = 3;
        private CameraEvent PreservedRendererCameraEvent => preservedObjectOrder == "InFront" ? CameraEvent.AfterForwardAlpha : CameraEvent.BeforeForwardAlpha;
        private string preservedObjectOrder = "BehindHero";
        private bool browserWaitingForInputRelease;
        private const float PreservedRendererSyncInterval = 0.5f;
        private const float ProjectileRendererSyncInterval = 0.12f;
        private const float MediumProjectileRendererSyncInterval = 0.2f;
        private const float HeavyProjectileRendererSyncInterval = 0.35f;
        private const float PreservedRendererCacheRefreshInterval = 2.0f;
        private const float StripRefreshInterval = 2.0f;
        private const int DefaultMaxPreservedRenderers = 160;
        private const int DefaultMaxProjectileParticleRenderers = 48;
        private const int MediumProjectileRuleCount = 12;
        private const int HeavyProjectileRuleCount = 32;
        private static readonly string[] ProjectileNameHints =
        {
            "projectile",
            "fireball",
            "flameball",
            "flame",
            "orb",
            "shot",
            "spike",
            "bats",
            "attack",
            "ball",
            "impact",
            "lava",
            "trail",
            "rock",
            "slam",
            "hitter",
            "laser",
            "spit"
        };
        private static readonly string[] EffectTags = { "Nail Attack", "Hero Spell" };
        private static readonly string[] EffectNames = { "Slash", "Fireball", "Focus", "Scream", "Dive", "Shield", "Soul" };
        private static readonly string[] EntityContainerNames = { "Battle Scene", "_Enemies", "_NPCs", "infected_event", "non_infected_event" };
        private static readonly string[] EntityIgnoredRootPrefixes =
        {
            "_GameCameras",
            "_GameManager",
            "_Scenery",
            "_Props",
            "_Areas",
            "Area Title",
            "Arrow Prompt",
            "Charm ",
            "Game_Map",
            "Inventory",
            "Journal ",
            "Map ",
            "sprite_set_",
            "UI "
        };
        private static readonly string[] BossNameHints =
        {
            "False Knight",
            "Hornet",
            "Zote",
            "Grimm",
            "Mantis Lord",
            "Mage Lord",
            "Soul Master",
            "Nosk",
            "Collector",
            "Defender",
            "Vessel",
            "Radiance",
            "Traitor Lord",
            "Broken Vessel",
            "Mawlek",
            "Hive Knight",
            "Uumuu",
            "Oblobb",
            "God Tamer",
            "Nailmaster",
            "Paintmaster",
            "Sly"
        };
        private static readonly string[] NpcNameHints =
        {
            "NPC",
            "Stag",
            "Quirrel",
            "Cornifer",
            "Cloth",
            "Nymm",
            "Bretta",
            "Myla",
            "Tiso",
            "Seer",
            "Hunter",
            "Leg Eater",
            "Divine",
            "Jiji",
            "Elderbug"
        };
        private static readonly string[] MobNameHints =
        {
            "Buzzer",
            "Zombie",
            "Crawler",
            "Spitter",
            "Husk",
            "Moss",
            "Mantis",
            "Miner",
            "Sentry",
            "Guard",
            "Barger",
            "Blob",
            "Jelly",
            "Aspid",
            "Fung",
            "Mawlek",
            "Shade"
        };
        private static readonly string[] MaterialTextureProperties =
        {
            "_MainTex",
            "_BaseMap",
            "_ColorTex",
            "_SpriteTex",
            "_SpriteTexture",
            "_Tex",
            "_Texture",
            "_MaskTex",
            "_NoiseTex",
            "_DetailTex",
            "_BumpMap",
            "_EmissionMap",
            "_Illum",
            "_LightMap",
            "_PaletteTex"
        };
        private static readonly string[] BackgroundColorNames =
        {
            BackgroundColorGreen,
            BackgroundColorBlue,
            BackgroundColorRed,
            BackgroundColorYellow,
            BackgroundColorCyan,
            BackgroundColorMagenta,
            BackgroundColorOrange,
            BackgroundColorWhite,
            BackgroundColorBlack
        };
        private static readonly string[] ProjectilePerformanceModes =
        {
            ProjectilePerformanceSafe,
            ProjectilePerformanceBalanced,
            ProjectilePerformanceAggressive,
            ProjectilePerformanceCustom
        };
        private static readonly string[] PerformanceProfileModes =
        {
            PerformanceProfileQuality,
            PerformanceProfileBalanced,
            PerformanceProfilePerformance,
            PerformanceProfileUltra,
            PerformanceProfileCustom
        };
        private static readonly string[] ProjectileCaptureModes =
        {
            ProjectileCaptureOnlyProjectilesTab,
            ProjectileCaptureBrowserOpen,
            ProjectileCaptureAlways,
            ProjectileCaptureManual
        };

        private readonly HashSet<string> preservedAssetNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> preservedSpriteNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> preservedTextureNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> preservedMaterialNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> preservedObjectPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> preservedGroupObjectPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> preservedProjectileObjectPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly List<RendererPreserveRule> preservedRendererRules = new List<RendererPreserveRule>();
        private readonly HashSet<string> preservedRendererRuleKeys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> preservedRendererExactMatchKeys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> preservedProjectileRendererExactMatchKeys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, RecentRendererSnapshot> recentProjectileRenderers = new Dictionary<int, RecentRendererSnapshot>();
        private readonly Dictionary<Material, Shader> strippedShaders = new Dictionary<Material, Shader>();
        private readonly Dictionary<Camera, Color> strippedBackgrounds = new Dictionary<Camera, Color>();
        private readonly Dictionary<Camera, int> strippedMasks = new Dictionary<Camera, int>();
        private readonly Dictionary<Camera, CameraClearFlags> strippedClearFlags = new Dictionary<Camera, CameraClearFlags>();
        private readonly Dictionary<GameObject, int> promotedEffects = new Dictionary<GameObject, int>();
        private readonly HashSet<GameObject> promotedProjectileVisuals = new HashSet<GameObject>();
        private readonly Dictionary<Camera, CommandBuffer> preservedRendererCommandBuffers = new Dictionary<Camera, CommandBuffer>();
        private readonly Dictionary<Renderer, PreservedRendererProxy> preservedRendererProxies = new Dictionary<Renderer, PreservedRendererProxy>();
        private readonly List<Renderer> preservedRendererDrawSources = new List<Renderer>();
        private readonly List<Renderer> proxyRemoveBuffer = new List<Renderer>();
        private readonly List<int> filteredTextureBrowserIndexes = new List<int>();
        private readonly Dictionary<int, string> objectPathCache = new Dictionary<int, string>();
        private readonly List<int> objectPathCacheOrder = new List<int>();
        private readonly List<string> objectPathSegments = new List<string>(16);
        private readonly StringBuilder objectPathBuilder = new StringBuilder(256);
        private GameObject runner;
        private GameObject proxyRoot;
        private bool initialized;
        private bool visualsActive;
        private bool menuBackgroundMode;
        private bool menuMaskCached;
        private int cachedMenuMask;
        private int browserInputReleaseFrame;
        private bool textureBrowserVisible;
        private bool pauseGameWhileBrowserOpen = true;
        private bool textureBrowserPauseActive;
        private float textureBrowserPreviousTimeScale = 1f;
        private bool textureBrowserCursorActive;
        private bool textureBrowserPreviousCursorVisible;
        private CursorLockMode textureBrowserPreviousCursorLockState;
        private bool textureBrowserNeedsRefresh = true;
        private bool textureBrowserNearbyMode;
        private bool textureBrowserEntityMode;
        private bool textureBrowserAtlasMode;
        private bool textureBrowserPreserveMode;
        private bool textureBrowserProjectileMode;
        private bool textureBrowserMenuMode;
        private readonly List<TextureBrowserEntry> textureBrowserEntries = new List<TextureBrowserEntry>();
        private string textureBrowserSearch = "";
        private int selectedTextureBrowserIndex = -1;
        private Vector2 textureBrowserListScroll;
        private Vector2 textureBrowserDetailScroll;
        private float textureBrowserPreviewZoom = 1f;
        private Vector2 textureBrowserPreviewPan;
        private bool textureBrowserPreviewDragging;
        private Rect textureBrowserWindowRect = new Rect(50f, 50f, TextureBrowserBaseWidth, TextureBrowserBaseHeight);
        private int textureBrowserDetailTab;
        private int heroLayer = -1;
        private int visibleMask;
        private float lastPromoteLogTime;
        private float nextPreservedRendererCacheRebuildTime;
        private bool preservedRendererCacheDirty = true;
        private bool preservedRendererRuleIndexDirty = true;
        private bool preservedRendererRuleIndexHasFallbackRules;
        private string preservedRendererCacheSceneName = "";
        private string preserveFilePath;
        private string dumpFilePath;
        private string settingsFilePath;
        private string exportFolderMode = ExportModeDownloads;
        private string exportDirectory;
        private string exportDirectoryInput = "";
        private string backgroundColorName = BackgroundColorGreen;
        private Color stripBackground = DefaultStripBackground;
        private string backgroundMode = BackgroundModeColor;
        private string backgroundImageRelativePath = "";
        private string backgroundImageLayout = BackgroundImageLayoutCover;

        private string backgroundImagesDirectory = "";


        private Texture2D backgroundImageTexture;

        private string loadedBackgroundImagePath = "";
        private System.DateTime loadedBackgroundImageLastWriteUtc;
        private readonly List<string> backgroundImageFiles = new List<string>();
        private bool backgroundImageFilesDirty = true;
        private string backgroundImageMessage = "Choose an image from background-images.";
        private string performanceProfile = PerformanceProfileBalanced;
        private string projectileCaptureMode = ProjectileCaptureOnlyProjectilesTab;
        private float applyStripIntervalSetting = DefaultApplyStripIntervalSetting;
        private float proxyUpdateIntervalSetting = DefaultProxyUpdateIntervalSetting;
        private float textureBrowserRefreshInterval = DefaultTextureBrowserRefreshInterval;
        private float heavyWorkBudgetMs = DefaultHeavyWorkBudgetMs;
        private float gcBudgetKb = DefaultGcBudgetKb;
        private bool staggerPerformanceTasks = true;
        private bool scanOnlyActiveBrowserTab = true;
        private bool pauseBrowserScansWhenClosed = true;
        private bool rendererPathCacheEnabled = true;
        private bool preserveMatchCacheEnabled = true;
        private bool projectileCandidateCacheEnabled = true;
        private bool browserSnapshotCacheEnabled = true;
        private bool drawSourceCacheEnabled = true;
        private bool showPerformanceCounters = true;
        private bool showCacheStats = true;
        private bool collectDetailedMetrics;
        private bool showGcAllocStats = true;
        private bool virtualizedBrowserList = true;
        private bool cacheWarmupAfterSceneChange = true;
        private bool projectileUseUnscaledTime = true;
        private int maxVisibleBrowserEntries = DefaultMaxVisibleBrowserEntries;
        private int browserListOverscanRows = DefaultBrowserListOverscanRows;
        private int maxCachedRendererPaths = DefaultMaxCachedRendererPaths;
        private string projectilePerformanceMode = ProjectilePerformanceBalanced;
        private float projectileCaptureInterval = DefaultProjectileCaptureInterval;
        private float recentProjectileLifetime = DefaultRecentProjectileLifetime;
        private float preservedSyncIntervalSetting = DefaultPreservedSyncIntervalSetting;
        private int maxPreservedRenderers = DefaultMaxPreservedRenderers;
        private int maxProjectileParticleRenderers = DefaultMaxProjectileParticleRenderers;
        private string projectileCaptureIntervalInput = "";
        private string recentProjectileLifetimeInput = "";
        private string preservedSyncIntervalInput = "";
        private string maxPreservedRenderersInput = "";
        private string maxProjectileParticleRenderersInput = "";
        private string applyStripIntervalInput = "";
        private string proxyUpdateIntervalInput = "";
        private string textureBrowserRefreshIntervalInput = "";
        private string heavyWorkBudgetInput = "";
        private string gcBudgetInput = "";
        private string maxVisibleBrowserEntriesInput = "";
        private string browserListOverscanRowsInput = "";
        private string maxCachedRendererPathsInput = "";
        private string lastTextureExportMessage = "";
        private string lastPreserveManagerMessage = "";
        private string lastSettingsMessage = "";
        private string lastPerformanceMessage = "";
        private float nextRecentProjectileCaptureTime;
        private bool preserveDrawSyncRequested;
        private double lastApplyStripMs;
        private double lastDrawSyncMs;
        private double lastProjectileScanMs;
        private double lastProxyUpdateMs;
        private long lastApplyStripGcBytes;
        private long lastDrawSyncGcBytes;
        private long lastProjectileScanGcBytes;
        private long lastProxyUpdateGcBytes;
        private int performanceTasksDeferred;
        private int objectPathCacheHits;
        private int objectPathCacheMisses;
        private int objectPathCacheEvictions;

        internal static ShaderStripMod Instance { get; private set; }

        public override string GetVersion()
        {
            return "1.29.29";
        }

        public override void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            visualsActive = true;
            ResetMenuBackgroundCache();
            Instance = this;
            heroLayer = -1;
            visibleMask = 0;
            lastPromoteLogTime = 0f;
            InitializeListFiles();
            LoadExportSettings();
            LoadPreserveList();
            LoadSpriteMasks();
            InitializeVisualOverridesFoundation();
            InitializePart0RuntimeGate();

            runner = new GameObject("ShaderStripMod.Runner");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<ScanBehaviour>();
#if RUNTIME_AUDIT
            runner.AddComponent<StableRuntimeAudit>();
#endif

            ModHooks.SceneChanged += OnSceneChanged;
            On.HeroController.Start += OnHeroStart;
            On.HeroController.LookForInput += OnHeroLookForInput;
            On.HeroController.LookForQueueInput += OnHeroLookForQueueInput;
            On.HeroController.DoAttack += OnHeroDoAttack;
            On.InputHandler.Update += OnInputHandlerUpdate;
            On.InputHandler.OnGUI += OnInputHandlerOnGUI;
            // Anchor before background command buffers capture this camera's view matrix.
            Camera.onPreCull += OnPinnedCameraPreCull;
            Camera.onPreCull += OnImageBackgroundCameraPreCull;

            Log("ShaderStripMod v1.29.29 initialized. F8 pins/releases the gameplay camera, F9 opens texture browser, F10 dumps loaded sprites/textures, F11 reloads preserve list, F12 toggles visuals.");
            ApplyStrip();
        }

        public void Unload()
        {
            if (!initialized)
            {
                return;
            }

            ModHooks.SceneChanged -= OnSceneChanged;
            On.HeroController.Start -= OnHeroStart;
            On.HeroController.LookForInput -= OnHeroLookForInput;
            On.HeroController.LookForQueueInput -= OnHeroLookForQueueInput;
            On.HeroController.DoAttack -= OnHeroDoAttack;
            On.InputHandler.Update -= OnInputHandlerUpdate;
            On.InputHandler.OnGUI -= OnInputHandlerOnGUI;
            Camera.onPreCull -= OnImageBackgroundCameraPreCull;
            Camera.onPreCull -= OnPinnedCameraPreCull;
            DisposeCameraPin();
            SetTextureBrowserVisible(false);
            RestoreTextureBrowserMenuInputBlocker();

            ReleaseCustomMenu();ReleaseMenuObjectPreview();
            ReleaseImageBackground();

            if (runner != null)
            {
                Object.Destroy(runner);
                runner = null;
            }

            RestoreAll();
            visualsActive = false;
            menuBackgroundMode = false;
            initialized = false;
            Instance = null;
            Log("ShaderStripMod unloaded. Original shaders, colors and camera masks restored.");
        }

        internal void Toggle()
        {
            if (visualsActive)
            {
                DisableVisuals();
            }
            else
            {
                EnableVisuals();
            }
        }

        internal void ForceStop()
        {
            DisableVisuals();
        }

        private void EnableVisuals()
        {
            if (!initialized || visualsActive)
            {
                return;
            }

            visualsActive = true;
            ApplyStrip();
            Log("ShaderStripMod visuals applied.");
        }

        private void DisableVisuals()
        {
            if (!visualsActive)
            {
                return;
            }

            RestoreAll();
            visualsActive = false;
            Log("ShaderStripMod visuals reverted.");
        }

        private void RestoreAll()
        {
            ReleaseCameraPin();
            RestorePart0RuntimeGate("RestoreAll");
            ReleaseCustomMenu();ReleaseMenuObjectPreview();
            ReleaseImageBackground();
            RestoreShaders();
            RestoreBackgrounds();
            RestoreCameraClearFlags();
            RestoreCameraMasks();
            RestorePromotedEffects();
            RestoreProjectileVisibilityWatchers();
            RestorePreservedRendererDraws();
            RestorePreservedRendererProxies();
            ReleaseSpriteMaskMeshes();
            ReleaseMaskEditor();
            MarkPreservedRendererCacheDirty();
            ClearRuntimeCaches();
            ResetMenuBackgroundCache();
        }

        private void OnHeroStart(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);

            if (self != null)
            {
                SetHeroLayer(self.gameObject.layer);
                ProbeVisualOverridesFoundation(self);
            }

            if (visualsActive)
            {
                ApplyStrip();
            }
        }

        private void OnSceneChanged(string sceneName)
        {
            ReleaseCameraPin("Camera released for a scene change, respawn or transition.");
            HideImageBackground();
            ReleaseCustomMenu();ReleaseMenuObjectPreview();
            RestorePart0RuntimeGate("SceneChanged");
            OnVisualOverrideFoundationSceneChanged();
            RestoreBackgrounds();
            RestoreCameraClearFlags();
            RestoreCameraMasks();
            RestorePromotedEffects();
            RestoreProjectileVisibilityWatchers();
            RestorePreservedRendererDraws();
            RestorePreservedRendererProxies();
            ReleaseSpriteMaskMeshes();
            ReleaseMaskEditor();
            MarkPreservedRendererCacheDirty();
            ClearRuntimeCaches();
            ResetMenuBackgroundCache();
            heroLayer = -1;
            visibleMask = 0;
            textureBrowserNeedsRefresh = true;
            lastPromoteLogTime = 0f;
            UpdateTextureBrowserMenuInputBlocker();
            Log("ShaderStripMod scene changed to " + sceneName + ".");

            if (visualsActive)
            {
                ApplyStrip();
            }
        }



    }
}












