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
        private void PromotePreservedRenderers()
        {
            if (heroLayer < 0)
            {
                RestorePreservedRendererDraws();
                RestorePreservedRendererProxies();
                return;
            }

            if (HasEffectivePreserveRules())
            {
                RefreshProjectileVisibilityWatchers();
                if (SyncPreservedRendererDraws())
                {
                    RestorePreservedRendererProxies();
                }
                else
                {
                    SyncPreservedRendererProxies();
                }
            }
            else
            {
                RestorePreservedRendererDraws();
                RestorePreservedRendererProxies();
            }
        }

        private bool SyncPreservedRendererDraws()
        {
            if (heroLayer < 0 || !HasEffectivePreserveRules() || IsMenuScene(GetActiveSceneName()))
            {
                RestorePreservedRendererDraws();
                return false;
            }

            Camera worldCamera = GetWorldCamera();
            if (worldCamera == null)
            {
                RestorePreservedRendererDraws();
                return false;
            }

            CommandBuffer buffer = EnsurePreservedRendererCommandBuffer(worldCamera);
            if (buffer == null)
            {
                return false;
            }

            buffer.Clear();
            RemovePreservedRendererCommandBuffersExcept(worldCamera);

            string sceneName = GetActiveSceneName();
            if (ShouldRebuildPreservedRendererCache(sceneName))
            {
                RebuildPreservedRendererDrawSources(sceneName);
            }
            else
            {
                RefreshActivePreservedRendererRules(sceneName);
                RefreshActivePreservedPathRenderers(sceneName);
            }

            bool sawStaleRenderer = false;
            int drawnRenderers = 0;
            for (int i = 0; i < preservedRendererDrawSources.Count; i++)
            {
                Renderer renderer = preservedRendererDrawSources[i];
                if (!CanDrawPreservedRenderer(renderer, sceneName))
                {
                    sawStaleRenderer = true;
                    continue;
                }

                if (!HasActiveSpriteMask(renderer) && RendererHasProjectilePath(renderer) && TryUseNormalRendererPipeline(renderer))
                {
                    drawnRenderers++;
                }
                else if (AddPreservedRendererDrawCommands(buffer, renderer) > 0)
                {
                    drawnRenderers++;
                }

                if (drawnRenderers >= maxPreservedRenderers)
                {
                    break;
                }
            }

            if (sawStaleRenderer)
            {
                preservedRendererCacheDirty = true;
            }

            return true;
        }

        private bool ShouldRebuildPreservedRendererCache(string sceneName)
        {
            return preservedRendererCacheDirty ||
                !drawSourceCacheEnabled ||
                !string.Equals(preservedRendererCacheSceneName, sceneName, System.StringComparison.OrdinalIgnoreCase) ||
                Time.unscaledTime >= nextPreservedRendererCacheRebuildTime;
        }

        private void RebuildPreservedRendererDrawSources(string sceneName)
        {
            preservedRendererDrawSources.Clear();
            preservedRendererCacheSceneName = sceneName;
            preservedRendererCacheDirty = false;
            nextPreservedRendererCacheRebuildTime = Time.unscaledTime + PreservedRendererCacheRefreshInterval;

            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!CanDrawPreservedRenderer(renderer, sceneName))
                {
                    continue;
                }

                AddPreservedRendererDrawSource(renderer);
                if (preservedRendererDrawSources.Count >= maxPreservedRenderers)
                {
                    break;
                }
            }
        }

        private bool AddPreservedRendererDrawSource(Renderer renderer)
        {
            if (renderer == null || preservedRendererDrawSources.Count >= maxPreservedRenderers)
            {
                return false;
            }

            if (preservedRendererDrawSources.Contains(renderer))
            {
                return true;
            }

            if (IsParticleSystemRenderer(renderer) &&
                !RendererHasProjectileRendererRule(renderer) &&
                CountProjectileParticleDrawSources() >= maxProjectileParticleRenderers)
            {
                return false;
            }

            preservedRendererDrawSources.Add(renderer);
            return true;
        }

        private void RefreshActivePreservedRendererRules(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) ||
                preservedRendererRules.Count == 0 ||
                preservedRendererDrawSources.Count >= maxPreservedRenderers)
            {
                return;
            }

            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                if (preservedRendererDrawSources.Count >= maxPreservedRenderers)
                {
                    return;
                }

                RendererPreserveRule rule = preservedRendererRules[i];
                string path = rule != null ? CleanUnityPath(rule.ObjectPath) : "";
                if (string.IsNullOrEmpty(path) || IsIgnoredRendererPreserveRule(rule))
                {
                    continue;
                }

                AddActivePreservedRenderersUnderPath(path, sceneName, false);
            }
        }

        private int CountProjectileParticleDrawSources()
        {
            int count = 0;
            for (int i = 0; i < preservedRendererDrawSources.Count; i++)
            {
                Renderer renderer = preservedRendererDrawSources[i];
                if (IsParticleSystemRenderer(renderer) && RendererHasProjectilePath(renderer))
                {
                    count++;
                }
            }

            return count;
        }

        private void RefreshActivePreservedPathRenderers(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) ||
                !HasPathPreserveRules() ||
                preservedRendererDrawSources.Count >= maxPreservedRenderers)
            {
                return;
            }

            RefreshActivePreservedPathRenderers(sceneName, preservedObjectPaths, false, false);
            RefreshActivePreservedPathRenderers(sceneName, preservedGroupObjectPaths, true, false);
            RefreshActivePreservedPathRenderers(sceneName, preservedProjectileObjectPaths, true, true);
        }

        private void RefreshActivePreservedPathRenderers(string sceneName, HashSet<string> paths, bool includeChildren, bool projectile)
        {
            foreach (string path in paths)
            {
                if (preservedRendererDrawSources.Count >= maxPreservedRenderers)
                {
                    return;
                }

                string cleanedPath = CleanUnityPath(path);
                if (string.IsNullOrEmpty(cleanedPath) ||
                    (projectile ? IsIgnoredProjectilePreservePath(cleanedPath) : IsIgnoredPreservedObjectPathRule(cleanedPath)))
                {
                    continue;
                }

                AddActivePreservedRenderersUnderPath(cleanedPath, sceneName, includeChildren);
            }
        }

        private void AddActivePreservedRenderersUnderPath(string cleanedPath, string sceneName, bool includeChildren)
        {
            string[] parts = cleanedPath.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            int sceneCount;
            try
            {
                sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            }
            catch
            {
                return;
            }

            for (int i = 0; i < sceneCount; i++)
            {
                if (preservedRendererDrawSources.Count >= maxPreservedRenderers)
                {
                    return;
                }

                UnityEngine.SceneManagement.Scene scene;
                try
                {
                    scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                }
                catch
                {
                    continue;
                }

                if (!IsCurrentGameplayScene(scene, sceneName))
                {
                    continue;
                }

                GameObject[] roots;
                try
                {
                    roots = scene.GetRootGameObjects();
                }
                catch
                {
                    continue;
                }

                if (roots == null)
                {
                    continue;
                }

                for (int j = 0; j < roots.Length; j++)
                {
                    AddActivePreservedRenderersFromPathMatch(roots[j] != null ? roots[j].transform : null, parts, 0, sceneName, includeChildren);
                }
            }
        }

        private void AddActivePreservedRenderersFromPathMatch(Transform current, string[] parts, int partIndex, string sceneName, bool includeChildren)
        {
            if (current == null ||
                parts == null ||
                partIndex < 0 ||
                partIndex >= parts.Length ||
                current.gameObject == null ||
                !current.gameObject.activeInHierarchy ||
                preservedRendererDrawSources.Count >= maxPreservedRenderers)
            {
                return;
            }

            if (!string.Equals(CleanUnityName(current.name), parts[partIndex], System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (partIndex == parts.Length - 1)
            {
                AddActivePreservedRenderersFromObject(current.gameObject, sceneName, includeChildren);
                return;
            }

            int nextIndex = partIndex + 1;
            for (int i = 0; i < current.childCount; i++)
            {
                Transform child = current.GetChild(i);
                AddActivePreservedRenderersFromPathMatch(child, parts, nextIndex, sceneName, includeChildren);
            }
        }

        private void AddActivePreservedRenderersFromObject(GameObject obj, string sceneName, bool includeChildren)
        {
            if (obj == null || preservedRendererDrawSources.Count >= maxPreservedRenderers)
            {
                return;
            }

            Renderer[] renderers;
            try
            {
                renderers = includeChildren ? obj.GetComponentsInChildren<Renderer>(false) : obj.GetComponents<Renderer>();
            }
            catch
            {
                return;
            }

            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (preservedRendererDrawSources.Count >= maxPreservedRenderers)
                {
                    return;
                }

                Renderer renderer = renderers[i];
                if (CanDrawPreservedRenderer(renderer, sceneName))
                {
                    AddPreservedRendererDrawSource(renderer);
                }
            }
        }

        private Camera GetWorldCamera()
        {
            return GetGameplayWorldCamera(Camera.allCameras);
        }

        private CommandBuffer EnsurePreservedRendererCommandBuffer(Camera camera)
        {
            if (camera == null)
            {
                return null;
            }

            CommandBuffer buffer;
            if (preservedRendererCommandBuffers.TryGetValue(camera, out buffer) && buffer != null)
            {
                return buffer;
            }

            buffer = new CommandBuffer();
            buffer.name = "ShaderStripMod.PreservedRendererDraws";
            camera.AddCommandBuffer(PreservedRendererCameraEvent, buffer);
            preservedRendererCommandBuffers[camera] = buffer;
            return buffer;
        }

        private void RemovePreservedRendererCommandBuffersExcept(Camera keep)
        {
            if (preservedRendererCommandBuffers.Count == 0)
            {
                return;
            }

            List<Camera> remove = null;
            foreach (KeyValuePair<Camera, CommandBuffer> pair in preservedRendererCommandBuffers)
            {
                if (pair.Key == keep)
                {
                    continue;
                }

                if (pair.Value != null)
                {
                    if (pair.Key != null) pair.Key.RemoveCommandBuffer(PreservedRendererCameraEvent, pair.Value);
                    pair.Value.Release();
                }

                if (remove == null)
                {
                    remove = new List<Camera>();
                }

                remove.Add(pair.Key);
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                preservedRendererCommandBuffers.Remove(remove[i]);
            }
        }

        private void RestorePreservedRendererDraws()
        {
            foreach (KeyValuePair<Camera, CommandBuffer> pair in preservedRendererCommandBuffers)
            {
                if (pair.Value != null)
                {
                    pair.Value.Clear();
                    if (pair.Key != null) pair.Key.RemoveCommandBuffer(PreservedRendererCameraEvent, pair.Value);
                    pair.Value.Release();
                }
            }

            preservedRendererCommandBuffers.Clear();
            preservedRendererDrawSources.Clear();
            preservedRendererCacheSceneName = "";
            nextPreservedRendererCacheRebuildTime = 0f;
            preservedRendererCacheDirty = true;
        }

        private void MarkPreservedRendererCacheDirty()
        {
            preservedRendererCacheDirty = true;
            nextPreservedRendererCacheRebuildTime = 0f;
            preserveDrawSyncRequested = true;
        }

        private bool CanDrawPreservedRenderer(Renderer renderer, string sceneName)
        {
            if (renderer == null || renderer.gameObject == null || IsProxyRenderer(renderer))
            {
                return false;
            }

            if (!renderer.enabled ||
                !renderer.gameObject.activeInHierarchy ||
                (IsRendererLayerAlreadyVisible(renderer) && !IsRendererPromotedByMod(renderer) && !HasActiveSpriteMask(renderer)) ||
                IsExpensivePreservedRenderer(renderer) ||
                !IsRoomRenderer(renderer, sceneName) ||
                !RendererIsPreserved(renderer))
            {
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return false;
            }

            return true;
        }

        private bool TryUseNormalRendererPipeline(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject == null || heroLayer < 0)
            {
                return false;
            }

            if (IsRendererLayerAlreadyVisible(renderer))
            {
                return true;
            }

            if (!CanPromoteVisualEffectLayer(renderer.gameObject))
            {
                return false;
            }

            PromoteSingleObject(renderer.gameObject);
            if (promotedEffects.ContainsKey(renderer.gameObject) && renderer.gameObject.layer == heroLayer)
            {
                promotedProjectileVisuals.Add(renderer.gameObject);
            }

            return IsRendererPromotedByMod(renderer);
        }

        private bool IsRendererPromotedByMod(Renderer renderer)
        {
            return renderer != null &&
                renderer.gameObject != null &&
                promotedProjectileVisuals.Contains(renderer.gameObject) &&
                promotedEffects.ContainsKey(renderer.gameObject) &&
                renderer.gameObject.layer == heroLayer;
        }

        private void RefreshProjectileVisibilityWatchers()
        {
            if (heroLayer < 0 || CountEffectiveProjectilePreserveRules() <= 0 || IsMenuScene(GetActiveSceneName()))
            {
                return;
            }

            string sceneName = GetActiveSceneName();
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    renderer.gameObject == null ||
                    IsProxyRenderer(renderer) ||
                    !IsRendererInCurrentScene(renderer, sceneName) ||
                    !RendererHasProjectilePath(renderer))
                {
                    continue;
                }

                if (renderer.gameObject.GetComponent<ProjectileVisibilityWatcher>() == null)
                {
                    renderer.gameObject.AddComponent<ProjectileVisibilityWatcher>();
                }
            }
        }

        private void HandleProjectileVisualEnabled(GameObject visualObject)
        {
            if (!initialized || !visualsActive || heroLayer < 0 || visualObject == null || IsMenuScene(GetActiveSceneName()))
            {
                return;
            }

            string sceneName = GetActiveSceneName();
            Renderer[] renderers = visualObject.GetComponents<Renderer>();
            bool needsFallbackSync = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    !IsRendererInCurrentScene(renderer, sceneName) ||
                    !RendererHasProjectilePath(renderer))
                {
                    continue;
                }

                if (!TryUseNormalRendererPipeline(renderer))
                {
                    needsFallbackSync = true;
                }
            }

            if (needsFallbackSync)
            {
                MarkPreservedRendererCacheDirty();
            }
        }

        private void RestoreProjectileVisibilityWatchers()
        {
            ProjectileVisibilityWatcher[] watchers = Resources.FindObjectsOfTypeAll<ProjectileVisibilityWatcher>();
            for (int i = 0; i < watchers.Length; i++)
            {
                if (watchers[i] != null)
                {
                    Object.Destroy(watchers[i]);
                }
            }
        }

        private bool IsRendererLayerAlreadyVisible(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject == null || renderer.gameObject.layer < 0 || renderer.gameObject.layer > 31)
            {
                return false;
            }

            return (visibleMask & (1 << renderer.gameObject.layer)) != 0;
        }

        private bool IsExpensivePreservedRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return true;
            }

            return IsParticleSystemRenderer(renderer) && !RendererHasProjectilePath(renderer);
        }

        private bool IsParticleSystemRenderer(Renderer renderer)
        {
            return renderer != null && string.Equals(renderer.GetType().Name, "ParticleSystemRenderer", System.StringComparison.Ordinal);
        }

        private bool HasEffectivePreserveRules()
        {
            if (HasActiveSpriteMasks() || HasEffectiveRendererPreserveRules())
            {
                return true;
            }

            if (HasAnyAssetPreserveRules())
            {
                return true;
            }

            return HasEffectivePathRules(preservedObjectPaths) ||
                HasEffectivePathRules(preservedGroupObjectPaths) ||
                HasEffectiveProjectilePathRules();
        }

        private bool HasEffectiveRendererPreserveRules()
        {
            for (int i = 0; i < preservedRendererRules.Count; i++)
            {
                if (!IsIgnoredRendererPreserveRule(preservedRendererRules[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasEffectivePathRules(HashSet<string> paths)
        {
            foreach (string path in paths)
            {
                if (!IsIgnoredPreservedObjectPathRule(path))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasEffectiveProjectilePathRules()
        {
            foreach (string path in preservedProjectileObjectPaths)
            {
                if (!IsIgnoredProjectilePreservePath(path))
                {
                    return true;
                }
            }

            return false;
        }

        private int AddPreservedRendererDrawCommands(CommandBuffer buffer, Renderer renderer)
        {
            if (buffer == null || renderer == null)
            {
                return 0;
            }

            int maskedDraws;
            if (TryDrawSpriteMask(buffer, renderer, out maskedDraws)) return maskedDraws;

            Material[] materials = renderer.sharedMaterials;
            if (materials == null)
            {
                return 0;
            }

            int added = 0;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                buffer.DrawRenderer(renderer, material, i);
                added++;
            }

            return added;
        }

        private void SyncPreservedRendererProxies()
        {
            if (heroLayer < 0 || !HasEffectivePreserveRules() || IsMenuScene(GetActiveSceneName()))
            {
                RestorePreservedRendererProxies();
                return;
            }

            EnsureProxyRoot();
            int frame = Time.frameCount;
            string sceneName = GetActiveSceneName();
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!CanProxyPreservedRenderer(renderer, sceneName))
                {
                    continue;
                }

                PreservedRendererProxy proxy = EnsurePreservedRendererProxy(renderer);
                if (proxy == null)
                {
                    continue;
                }

                proxy.LastSeenFrame = frame;
                UpdatePreservedRendererProxy(proxy);
            }

            RemoveStalePreservedRendererProxies(frame);
        }

        private bool CanProxyPreservedRenderer(Renderer renderer, string sceneName)
        {
            if (HasActiveSpriteMask(renderer)) return false;
            if (renderer == null ||
                renderer.gameObject == null ||
                IsProxyRenderer(renderer) ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy ||
                IsRendererLayerAlreadyVisible(renderer) ||
                !IsRoomRenderer(renderer, sceneName) ||
                !RendererIsPreserved(renderer))
            {
                return false;
            }

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            if (spriteRenderer != null)
            {
                return true;
            }

            MeshRenderer meshRenderer = renderer as MeshRenderer;
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            return meshRenderer != null && meshFilter != null && meshFilter.sharedMesh != null;
        }

        private void UpdatePreservedRendererProxies()
        {
            if (preservedRendererProxies.Count == 0)
            {
                return;
            }

            proxyRemoveBuffer.Clear();
            foreach (KeyValuePair<Renderer, PreservedRendererProxy> pair in preservedRendererProxies)
            {
                PreservedRendererProxy proxy = pair.Value;
                if (!IsPreservedRendererProxyValid(proxy))
                {
                    proxyRemoveBuffer.Add(pair.Key);
                    continue;
                }

                UpdatePreservedRendererProxy(proxy);
            }

            if (proxyRemoveBuffer.Count > 0)
            {
                for (int i = 0; i < proxyRemoveBuffer.Count; i++)
                {
                    RemovePreservedRendererProxy(proxyRemoveBuffer[i]);
                }

                proxyRemoveBuffer.Clear();
            }
        }

        private void EnsureProxyRoot()
        {
            if (proxyRoot != null)
            {
                return;
            }

            proxyRoot = new GameObject("ShaderStripMod.PreservedRendererProxies");
            if (runner != null)
            {
                proxyRoot.transform.SetParent(runner.transform, false);
            }
            else
            {
                Object.DontDestroyOnLoad(proxyRoot);
            }
        }

        private PreservedRendererProxy EnsurePreservedRendererProxy(Renderer source)
        {
            if (source == null)
            {
                return null;
            }

            PreservedRendererProxy existing;
            if (preservedRendererProxies.TryGetValue(source, out existing))
            {
                return existing;
            }

            PreservedRendererProxy created = CreatePreservedRendererProxy(source);
            if (created != null)
            {
                preservedRendererProxies[source] = created;
            }

            return created;
        }

        private PreservedRendererProxy CreatePreservedRendererProxy(Renderer source)
        {
            if (source == null || source.gameObject == null)
            {
                return null;
            }

            EnsureProxyRoot();
            GameObject proxyObject = new GameObject("ShaderStripMod.Proxy." + CleanUnityName(source.gameObject.name));
            proxyObject.layer = heroLayer;
            if (proxyRoot != null)
            {
                proxyObject.transform.SetParent(proxyRoot.transform, false);
            }

            Renderer proxyRenderer = null;
            SpriteRenderer sourceSpriteRenderer = source as SpriteRenderer;
            if (sourceSpriteRenderer != null)
            {
                proxyRenderer = proxyObject.AddComponent<SpriteRenderer>();
            }
            else
            {
                MeshRenderer sourceMeshRenderer = source as MeshRenderer;
                MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();
                if (sourceMeshRenderer != null && sourceMeshFilter != null && sourceMeshFilter.sharedMesh != null)
                {
                    proxyObject.AddComponent<MeshFilter>();
                    proxyRenderer = proxyObject.AddComponent<MeshRenderer>();
                }
            }

            if (proxyRenderer == null)
            {
                Object.Destroy(proxyObject);
                return null;
            }

            PreservedRendererProxy proxy = new PreservedRendererProxy();
            proxy.Source = source;
            proxy.ProxyObject = proxyObject;
            proxy.ProxyRenderer = proxyRenderer;
            proxy.SourcePath = GetObjectPath(source.gameObject);
            return proxy;
        }

        private void UpdatePreservedRendererProxy(PreservedRendererProxy proxy)
        {
            if (proxy == null || proxy.Source == null || proxy.ProxyObject == null || proxy.ProxyRenderer == null || heroLayer < 0)
            {
                return;
            }

            Renderer source = proxy.Source;
            Transform sourceTransform = source.transform;
            Transform proxyTransform = proxy.ProxyObject.transform;
            proxy.ProxyObject.layer = heroLayer;
            proxyTransform.position = sourceTransform.position;
            proxyTransform.rotation = sourceTransform.rotation;
            proxyTransform.localScale = sourceTransform.lossyScale;
            proxy.ProxyRenderer.enabled = source.enabled && source.gameObject.activeInHierarchy;
            CopyRendererCommon(source, proxy.ProxyRenderer);

            SpriteRenderer sourceSpriteRenderer = source as SpriteRenderer;
            SpriteRenderer proxySpriteRenderer = proxy.ProxyRenderer as SpriteRenderer;
            if (sourceSpriteRenderer != null && proxySpriteRenderer != null)
            {
                CopySpriteRenderer(sourceSpriteRenderer, proxySpriteRenderer);
                return;
            }

            MeshRenderer sourceMeshRenderer = source as MeshRenderer;
            MeshRenderer proxyMeshRenderer = proxy.ProxyRenderer as MeshRenderer;
            if (sourceMeshRenderer != null && proxyMeshRenderer != null)
            {
                CopyMeshRenderer(sourceMeshRenderer, proxyMeshRenderer);
            }
        }

        private void CopyRendererCommon(Renderer source, Renderer target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.sharedMaterials = source.sharedMaterials;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
            HeroController hero = HeroController.SilentInstance;
            Renderer heroRenderer = hero != null ? hero.GetComponentInChildren<Renderer>() : null;
            if (heroRenderer != null && !RendererHasProjectilePath(source))
            {
                target.sortingLayerID = heroRenderer.sortingLayerID;
                target.sortingOrder = Mathf.Clamp(heroRenderer.sortingOrder + (preservedObjectOrder == "InFront" ? 1 : -1), -32768, 32767);
            }
        }

        private void CopySpriteRenderer(SpriteRenderer source, SpriteRenderer target)
        {
            target.sprite = source.sprite;
            target.color = source.color;
            target.flipX = source.flipX;
            target.flipY = source.flipY;
            target.drawMode = source.drawMode;
            target.size = source.size;
            target.tileMode = source.tileMode;
            target.maskInteraction = source.maskInteraction;
        }

        private void CopyMeshRenderer(MeshRenderer source, MeshRenderer target)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            MeshFilter targetFilter = target.GetComponent<MeshFilter>();
            if (sourceFilter != null && targetFilter != null)
            {
                targetFilter.sharedMesh = sourceFilter.sharedMesh;
            }
        }

        private bool IsPreservedRendererProxyValid(PreservedRendererProxy proxy)
        {
            if (proxy == null || proxy.Source == null || proxy.ProxyObject == null || proxy.ProxyRenderer == null || heroLayer < 0)
            {
                return false;
            }

            Renderer source = proxy.Source;
            return source.gameObject != null &&
                source.enabled &&
                source.gameObject.activeInHierarchy &&
                !IsRendererLayerAlreadyVisible(source) &&
                RendererIsPreserved(source) &&
                !IsMenuScene(GetActiveSceneName());
        }

        private void RemoveStalePreservedRendererProxies(int frame)
        {
            if (preservedRendererProxies.Count == 0)
            {
                return;
            }

            proxyRemoveBuffer.Clear();
            foreach (KeyValuePair<Renderer, PreservedRendererProxy> pair in preservedRendererProxies)
            {
                if (pair.Value == null || pair.Value.LastSeenFrame != frame || !IsPreservedRendererProxyValid(pair.Value))
                {
                    proxyRemoveBuffer.Add(pair.Key);
                }
            }

            if (proxyRemoveBuffer.Count == 0)
            {
                return;
            }

            for (int i = 0; i < proxyRemoveBuffer.Count; i++)
            {
                RemovePreservedRendererProxy(proxyRemoveBuffer[i]);
            }

            proxyRemoveBuffer.Clear();
        }

        private void RemovePreservedRendererProxy(Renderer source)
        {
            if (object.ReferenceEquals(source, null))
            {
                return;
            }

            PreservedRendererProxy proxy;
            if (preservedRendererProxies.TryGetValue(source, out proxy))
            {
                if (proxy != null && proxy.ProxyObject != null)
                {
                    Object.Destroy(proxy.ProxyObject);
                }

                preservedRendererProxies.Remove(source);
            }
        }

        private void RestorePreservedRendererProxies()
        {
            foreach (KeyValuePair<Renderer, PreservedRendererProxy> pair in preservedRendererProxies)
            {
                PreservedRendererProxy proxy = pair.Value;
                if (proxy != null && proxy.ProxyObject != null)
                {
                    Object.Destroy(proxy.ProxyObject);
                }
            }

            preservedRendererProxies.Clear();
            if (proxyRoot != null)
            {
                Object.Destroy(proxyRoot);
                proxyRoot = null;
            }
        }

    }
}




