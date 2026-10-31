using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {

        private sealed class ProjectileVisibilityWatcher : MonoBehaviour
        {
            private void OnEnable()
            {
                ShaderStripMod.Instance?.HandleProjectileVisualEnabled(gameObject);
            }
        }

        private sealed class ScanBehaviour : MonoBehaviour
        {
            private float nextStripTime;
            private float nextPreserveDrawTime;
            private float nextProxyUpdateTime;

            private void Update()
            {
                var browserMod=ShaderStripMod.Instance;
                if (browserMod!=null && BrowserInputPolicy.ShouldToggleCameraPin(browserMod.textureBrowserVisible,Input.GetKeyDown(KeyCode.F8)))
                { browserMod.ToggleCameraPin();return; }
                if (browserMod!=null && BrowserInputPolicy.ShouldClose(browserMod.textureBrowserVisible,Input.GetKeyDown(KeyCode.Escape)))
                { browserMod.SetTextureBrowserVisible(false);return; }

                if (Input.GetKeyDown(KeyCode.F9))
                {
                    ShaderStripMod.Instance?.ToggleTextureBrowser();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.F10))
                {
                    ShaderStripMod.Instance?.DumpLoadedSpritesAndTextures();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.F11))
                {
                    ShaderStripMod.Instance?.ReloadPreserveList();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.F12))
                {
                    ShaderStripMod.Instance?.Toggle();
                    return;
                }

                ShaderStripMod mod = ShaderStripMod.Instance;
                if (mod == null)
                {
                    return;
                }

                mod.UpdateTextureBrowserPause();
                mod.UpdateTextureBrowserMenuInputBlocker();
                mod.UpdateTextureBrowserCursor();
                mod.UpdatePart0RuntimeGate();
                if (!mod.visualsActive)
                {
                    return;
                }

                float now = Time.unscaledTime;
                bool ranNormalTask = false;
                double frameBudgetUsedMs = 0.0;

                bool urgentProjectileCapture = mod.projectileCaptureInterval <= 0f;
                if (mod.ShouldCaptureProjectilesInCurrentState() && (urgentProjectileCapture || mod.TryEnterScheduledTask(ref ranNormalTask, ref frameBudgetUsedMs, false)))
                {
                    long memory;
                    long start = mod.BeginPerformanceMetric(out memory);
                    mod.CaptureRecentProjectileRenderers(false);
                    frameBudgetUsedMs += mod.EndPerformanceMetric(PerformanceMetricProjectileScan, start, memory);
                }

                float proxyInterval = mod.GetProxyUpdateInterval();
                bool urgentProxyUpdate = proxyInterval <= 0f;
                if (now >= nextProxyUpdateTime && (urgentProxyUpdate || mod.TryEnterScheduledTask(ref ranNormalTask, ref frameBudgetUsedMs, false)))
                {
                    nextProxyUpdateTime = now + proxyInterval;
                    long memory;
                    long start = mod.BeginPerformanceMetric(out memory);
                    mod.UpdatePreservedRendererProxies();
                    frameBudgetUsedMs += mod.EndPerformanceMetric(PerformanceMetricProxyUpdate, start, memory);
                }

                float preserveInterval = mod.GetPreservedRendererSyncInterval();
                bool urgentDrawSync = mod.preserveDrawSyncRequested || preserveInterval <= 0f;
                if ((urgentDrawSync || now >= nextPreserveDrawTime) && mod.TryEnterScheduledTask(ref ranNormalTask, ref frameBudgetUsedMs, urgentDrawSync))
                {
                    mod.preserveDrawSyncRequested = false;
                    nextPreserveDrawTime = now + preserveInterval;
                    long memory;
                    long start = mod.BeginPerformanceMetric(out memory);
                    mod.SyncPreservedRendererDraws();
                    frameBudgetUsedMs += mod.EndPerformanceMetric(PerformanceMetricDrawSync, start, memory);
                }

                float stripInterval = mod.GetApplyStripInterval();
                bool urgentStrip = stripInterval <= 0f;
                if (now >= nextStripTime && (urgentStrip || mod.TryEnterScheduledTask(ref ranNormalTask, ref frameBudgetUsedMs, false)))
                {
                    nextStripTime = now + stripInterval;
                    long memory;
                    long start = mod.BeginPerformanceMetric(out memory);
                    mod.ApplyStrip();
                    mod.EndPerformanceMetric(PerformanceMetricApplyStrip, start, memory);
                }
            }

            private void OnGUI()
            {
                ShaderStripMod.Instance?.DrawTextureBrowserGui();
            }

            private void LateUpdate()
            {
                ShaderStripMod mod = ShaderStripMod.Instance;
                if (mod != null)
                {
                    mod.CancelMaskPointerOnFocusLoss();
                    mod.UpdateCameraPin();
                    mod.UpdateImageBackground();
                }
            }
        }
    }
}

