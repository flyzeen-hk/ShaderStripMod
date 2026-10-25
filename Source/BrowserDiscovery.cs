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
        internal void ToggleTextureBrowser()
        {
            SetTextureBrowserVisible(!textureBrowserVisible);
        }

        private void SetTextureBrowserVisible(bool visible)
        {
            textureBrowserVisible = visible;
            backgroundImageFilesDirty = true;
            if (!visible)
            {
                ReleaseMaskEditor();CancelTexturePreviewPointer();maskLibraryMode=false;
                browserInputReleaseFrame = Time.frameCount + 1;
                browserWaitingForInputRelease = true;
            }
            if (visible && textureBrowserNeedsRefresh)
            {
                if (textureBrowserMenuMode || IsMenuScene(GetActiveSceneName())) RefreshMenuTextureBrowserEntries();
                else RefreshTextureBrowserEntries();
            }

            UpdateTextureBrowserPause();
            UpdateTextureBrowserCursor();
            UpdateTextureBrowserMenuInputBlocker();
        }

        private void SetPauseGameWhileBrowserOpen(bool enabled)
        {
            if (pauseGameWhileBrowserOpen == enabled)
            {
                return;
            }

            pauseGameWhileBrowserOpen = enabled;
            UpdateTextureBrowserPause();
            SaveExportSettings();
            lastSettingsMessage = enabled
                ? "Game pause while browser is open: enabled."
                : "Game pause while browser is open: disabled.";
        }

        private void UpdateTextureBrowserCursor()
        {
            if (textureBrowserVisible)
            {
                if (!textureBrowserCursorActive)
                {
                    textureBrowserPreviousCursorVisible = Cursor.visible;
                    textureBrowserPreviousCursorLockState = Cursor.lockState;
                    textureBrowserCursorActive = true;
                }

                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                }

                if (!Cursor.visible)
                {
                    Cursor.visible = true;
                }

                return;
            }

            ReleaseTextureBrowserCursor();
        }

        private void ReleaseTextureBrowserCursor()
        {
            if (!textureBrowserCursorActive)
            {
                return;
            }

            Cursor.lockState = textureBrowserPreviousCursorLockState;
            Cursor.visible = textureBrowserPreviousCursorVisible;
            textureBrowserCursorActive = false;
        }

        private void OnInputHandlerOnGUI(On.InputHandler.orig_OnGUI orig, InputHandler self)
        {
            if (!ShouldBlockBrowserGameplayInput())
            {
                orig(self);
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        internal void RefreshTextureBrowserEntries()
        {
            textureBrowserMenuMode = false;
            textureBrowserEntityMode = false;
            textureBrowserNearbyMode = false;
            textureBrowserAtlasMode = false;
            textureBrowserPreserveMode = false;
            textureBrowserProjectileMode = false;
            textureBrowserEntries.Clear();
            HashSet<string> seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite == null)
                {
                    continue;
                }

                AddTextureBrowserEntry(seen, "Sprite", sprite.name, sprite.texture, sprite, null, "spriteTexture=" + GetObjectName(sprite.texture));
            }

            Texture[] textures = Resources.FindObjectsOfTypeAll<Texture>();
            for (int i = 0; i < textures.Length; i++)
            {
                Texture texture = textures[i];
                if (texture == null)
                {
                    continue;
                }

                AddTextureBrowserEntry(seen, "Texture", texture.name, texture, null, null, "size=" + GetTextureSize(texture));
            }

            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                Texture texture = GetMaterialMainTexture(material);
                string shaderName = material.shader != null ? CleanUnityName(material.shader.name) : "";
                AddTextureBrowserEntry(seen, "Material", material.name, texture, null, material, "materialTexture=" + GetObjectName(texture) + "\nshader=" + shaderName);
            }

            textureBrowserEntries.Sort(CompareTextureBrowserEntries);
            if (selectedTextureBrowserIndex < 0 || selectedTextureBrowserIndex >= textureBrowserEntries.Count)
            {
                selectedTextureBrowserIndex = textureBrowserEntries.Count > 0 ? 0 : -1;
                ResetTextureBrowserPreviewView();
            }

            textureBrowserNeedsRefresh = false;
            Log("ShaderStripMod texture browser refreshed: " + textureBrowserEntries.Count + " entries.");
        }

        internal void RefreshNearbyTextureBrowserEntries()
        {
            textureBrowserMenuMode = false;
            textureBrowserEntityMode = false;
            textureBrowserNearbyMode = true;
            textureBrowserAtlasMode = false;
            textureBrowserPreserveMode = false;
            textureBrowserProjectileMode = false;
            textureBrowserEntries.Clear();
            textureBrowserListScroll = Vector2.zero;
            textureBrowserDetailScroll = Vector2.zero;
            selectedTextureBrowserIndex = -1;
            ResetTextureBrowserPreviewView();

            Vector3 origin = GetNearbyTextureOrigin();
            string sceneName = GetActiveSceneName();
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!IsRoomRenderer(renderer, sceneName))
                {
                    continue;
                }

                CollectNearbyTextureEntries(renderer, origin, GetRendererSceneName(renderer));
            }

            textureBrowserEntries.Sort(CompareNearbyTextureBrowserEntries);
            if (textureBrowserEntries.Count > 0)
            {
                selectedTextureBrowserIndex = 0;
            }

            textureBrowserNeedsRefresh = false;
            Log("ShaderStripMod nearby texture browser refreshed: " + textureBrowserEntries.Count + " renderer texture entries in scene " + sceneName + ".");
        }

        internal void RefreshEntityBrowserEntries()
        {
            textureBrowserMenuMode = false;
            textureBrowserEntityMode = true;
            textureBrowserNearbyMode = false;
            textureBrowserAtlasMode = false;
            textureBrowserPreserveMode = false;
            textureBrowserProjectileMode = false;
            textureBrowserEntries.Clear();
            textureBrowserListScroll = Vector2.zero;
            textureBrowserDetailScroll = Vector2.zero;
            selectedTextureBrowserIndex = -1;
            ResetTextureBrowserPreviewView();

            Vector3 origin = GetNearbyTextureOrigin();
            string sceneName = GetActiveSceneName();
            Dictionary<string, EntityBrowserGroup> groups = new Dictionary<string, EntityBrowserGroup>(System.StringComparer.OrdinalIgnoreCase);
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!IsRoomRenderer(renderer, sceneName))
                {
                    continue;
                }

                string objectPath = GetObjectPath(renderer.gameObject);
                if (!IsEntityCandidateRenderer(renderer, objectPath))
                {
                    continue;
                }

                string rootPath = GetEntityRootPath(objectPath);
                if (string.IsNullOrEmpty(rootPath) || IsHeroEntityRoot(rootPath))
                {
                    continue;
                }

                string rendererSceneName = GetRendererSceneName(renderer);
                string groupKey = rendererSceneName + "\n" + rootPath;
                EntityBrowserGroup group;
                if (!groups.TryGetValue(groupKey, out group))
                {
                    group = new EntityBrowserGroup();
                    group.RootPath = rootPath;
                    group.SceneName = rendererSceneName;
                    group.DisplayName = GetEntityDisplayName(rootPath);
                    group.Category = GetEntityCategory(rootPath, renderer);
                    group.Distance = float.PositiveInfinity;
                    groups[groupKey] = group;
                }

                UpdateEntityBrowserGroup(group, renderer, origin);
            }

            foreach (KeyValuePair<string, EntityBrowserGroup> pair in groups)
            {
                AddEntityBrowserEntry(pair.Value);
            }

            textureBrowserEntries.Sort(CompareEntityBrowserEntries);
            if (textureBrowserEntries.Count > 0)
            {
                selectedTextureBrowserIndex = 0;
            }

            textureBrowserNeedsRefresh = false;
            Log("ShaderStripMod entity browser refreshed: " + textureBrowserEntries.Count + " entity groups in scene " + sceneName + ".");
        }

        internal void RefreshAtlasBrowserEntries()
        {
            textureBrowserMenuMode = false;
            textureBrowserEntityMode = false;
            textureBrowserNearbyMode = false;
            textureBrowserAtlasMode = true;
            textureBrowserPreserveMode = false;
            textureBrowserProjectileMode = false;
            textureBrowserEntries.Clear();
            textureBrowserListScroll = Vector2.zero;
            textureBrowserDetailScroll = Vector2.zero;
            selectedTextureBrowserIndex = -1;
            ResetTextureBrowserPreviewView();

            Vector3 origin = GetNearbyTextureOrigin();
            string sceneName = GetActiveSceneName();
            HashSet<string> seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!IsRoomRenderer(renderer, sceneName) || IsProxyRenderer(renderer) || IsExpensivePreservedRenderer(renderer))
                {
                    continue;
                }

                CollectAtlasBrowserEntries(renderer, origin, GetRendererSceneName(renderer), seen);
            }

            textureBrowserEntries.Sort(CompareAtlasBrowserEntries);
            if (textureBrowserEntries.Count > 0)
            {
                selectedTextureBrowserIndex = 0;
            }

            textureBrowserNeedsRefresh = false;
            Log("ShaderStripMod atlas browser refreshed: " + textureBrowserEntries.Count + " atlas material texture entries in scene " + sceneName + ".");
        }

        internal void RefreshRecentProjectileBrowserEntries()
        {
            textureBrowserMenuMode = false;
            textureBrowserEntityMode = false;
            textureBrowserNearbyMode = false;
            textureBrowserAtlasMode = false;
            textureBrowserPreserveMode = false;
            textureBrowserProjectileMode = true;
            textureBrowserEntries.Clear();
            textureBrowserListScroll = Vector2.zero;
            textureBrowserDetailScroll = Vector2.zero;
            selectedTextureBrowserIndex = -1;
            ResetTextureBrowserPreviewView();

            CaptureRecentProjectileRenderers(true);
            float now = GetProjectileTime();
            List<RecentRendererSnapshot> snapshots = new List<RecentRendererSnapshot>(recentProjectileRenderers.Values);
            snapshots.Sort(CompareRecentProjectileSnapshots);
            for (int i = 0; i < snapshots.Count; i++)
            {
                AddRecentProjectileBrowserEntry(snapshots[i], now);
            }

            if (textureBrowserEntries.Count > 0)
            {
                selectedTextureBrowserIndex = 0;
            }

            textureBrowserNeedsRefresh = false;
            Log("ShaderStripMod recent projectile browser refreshed: " + textureBrowserEntries.Count + " recent projectile renderer entries.");
        }

        private int CompareRecentProjectileSnapshots(RecentRendererSnapshot left, RecentRendererSnapshot right)
        {
            int timeCompare = -left.LastSeenTime.CompareTo(right.LastSeenTime);
            if (timeCompare != 0)
            {
                return timeCompare;
            }

            return string.Compare(left.ObjectPath, right.ObjectPath, System.StringComparison.OrdinalIgnoreCase);
        }

        private void AddRecentProjectileBrowserEntry(RecentRendererSnapshot snapshot, float now)
        {
            if (snapshot == null)
            {
                return;
            }

            string projectilePath = snapshot.Renderer != null && snapshot.Renderer.gameObject != null
                ? GetProjectileRootPath(snapshot.Renderer.gameObject)
                : GetProjectileRootPath(snapshot.ObjectPath, snapshot.Layer);
            if (string.IsNullOrEmpty(projectilePath))
            {
                projectilePath = snapshot.ObjectPath;
            }

            TextureBrowserEntry entry = new TextureBrowserEntry();
            entry.Kind = "Recent Projectile";
            entry.Name = !string.IsNullOrEmpty(snapshot.SpriteName) ? snapshot.SpriteName : (!string.IsNullOrEmpty(snapshot.MaterialName) ? snapshot.MaterialName : snapshot.ObjectPath);
            entry.Texture = snapshot.Texture;
            entry.Sprite = snapshot.Sprite;
            entry.Material = snapshot.Material;
            entry.Renderer = snapshot.Renderer;
            entry.HasDistance = true;
            entry.Distance = Mathf.Max(0f, now - snapshot.LastSeenTime);
            entry.SceneName = snapshot.SceneName;
            entry.ObjectPath = snapshot.ObjectPath;
            entry.PreservePath = projectilePath;
            entry.Layer = snapshot.Layer;
            entry.RendererType = snapshot.RendererType;
            entry.RendererInstanceId = snapshot.InstanceId;
            entry.Position = snapshot.Position;
            entry.Details = "age=" + entry.Distance.ToString("0.00") +
                "\nscene=" + snapshot.SceneName +
                "\npath=" + snapshot.ObjectPath +
                "\nprojectilePath=" + projectilePath +
                "\nlayer=" + snapshot.Layer +
                "\nrenderer=" + snapshot.RendererType +
                "\nsprite=" + snapshot.SpriteName +
                "\nmaterial=" + snapshot.MaterialName +
                "\ntexture=" + snapshot.TextureName +
                "\nposition=" + FormatVector3(snapshot.Position);
            textureBrowserEntries.Add(entry);
        }

        internal void CaptureRecentProjectileRenderers(bool force)
        {
            float now = GetProjectileTime();
            if (!force && now < nextRecentProjectileCaptureTime)
            {
                return;
            }

            nextRecentProjectileCaptureTime = now + projectileCaptureInterval;
            RemoveExpiredRecentProjectileRenderers(now);

            string sceneName = GetActiveSceneName();
            if (string.IsNullOrEmpty(sceneName) || IsMenuScene(sceneName))
            {
                return;
            }

            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.gameObject == null || IsProxyRenderer(renderer) || !IsRoomRenderer(renderer, sceneName) || !IsProjectileCandidateRenderer(renderer))
                {
                    continue;
                }

                AddRecentProjectileRenderer(renderer, now);
            }

            if (!force && textureBrowserProjectileMode)
            {
                RefreshRecentProjectileBrowserEntriesFromCache(now);
            }
        }

        private void RefreshRecentProjectileBrowserEntriesFromCache(float now)
        {
            int selectedInstanceId = -1;
            if (selectedTextureBrowserIndex >= 0 && selectedTextureBrowserIndex < textureBrowserEntries.Count)
            {
                selectedInstanceId = textureBrowserEntries[selectedTextureBrowserIndex].RendererInstanceId;
            }

            textureBrowserEntries.Clear();
            List<RecentRendererSnapshot> snapshots = new List<RecentRendererSnapshot>(recentProjectileRenderers.Values);
            snapshots.Sort(CompareRecentProjectileSnapshots);
            for (int i = 0; i < snapshots.Count; i++)
            {
                AddRecentProjectileBrowserEntry(snapshots[i], now);
                if (snapshots[i].InstanceId == selectedInstanceId)
                {
                    selectedTextureBrowserIndex = textureBrowserEntries.Count - 1;
                }
            }

            if (selectedTextureBrowserIndex < 0 || selectedTextureBrowserIndex >= textureBrowserEntries.Count)
            {
                selectedTextureBrowserIndex = textureBrowserEntries.Count > 0 ? 0 : -1;
            }
        }

        private void AddRecentProjectileRenderer(Renderer renderer, float now)
        {
            if (renderer == null)
            {
                return;
            }

            int instanceId = renderer.GetInstanceID();
            RecentRendererSnapshot snapshot;
            if (!recentProjectileRenderers.TryGetValue(instanceId, out snapshot))
            {
                snapshot = new RecentRendererSnapshot();
                snapshot.InstanceId = instanceId;
                recentProjectileRenderers[instanceId] = snapshot;
            }

            snapshot.Renderer = renderer;
            snapshot.LastSeenTime = now;
            snapshot.SceneName = GetRendererSceneName(renderer);
            snapshot.ObjectPath = GetObjectPath(renderer.gameObject);
            snapshot.Layer = GetLayerLabel(renderer.gameObject);
            snapshot.RendererType = renderer.GetType().Name;
            snapshot.Position = GetRendererCenter(renderer);

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            snapshot.Sprite = sprite;
            snapshot.SpriteName = GetObjectName(sprite);
            snapshot.Texture = sprite != null ? sprite.texture : null;
            snapshot.TextureName = GetObjectName(snapshot.Texture);

            Material material = GetFirstRendererMaterial(renderer);
            snapshot.Material = material;
            snapshot.MaterialName = GetObjectName(material);
            Texture materialTexture = material != null ? GetMaterialMainTexture(material) : null;
            if (snapshot.Texture == null && materialTexture != null)
            {
                snapshot.Texture = materialTexture;
                snapshot.TextureName = GetObjectName(materialTexture);
            }
        }

        private void RemoveExpiredRecentProjectileRenderers(float now)
        {
            if (recentProjectileRenderers.Count == 0)
            {
                return;
            }

            List<int> remove = null;
            foreach (KeyValuePair<int, RecentRendererSnapshot> pair in recentProjectileRenderers)
            {
                if (now - pair.Value.LastSeenTime <= recentProjectileLifetime)
                {
                    continue;
                }

                if (remove == null)
                {
                    remove = new List<int>();
                }

                remove.Add(pair.Key);
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                recentProjectileRenderers.Remove(remove[i]);
            }
        }

    }
}




