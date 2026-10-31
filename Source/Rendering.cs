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
        internal void ApplyStrip()
        {
            HeroController hero = HeroController.SilentInstance;
            RenderSceneState state = RenderStatePolicy.Classify(IsMenuScene(GetActiveSceneName()), hero != null);

            if (state == RenderSceneState.Menu)
            {
                heroLayer = -1;
                visibleMask = 0;
                ApplyMenuBackground();
                return;
            }

            if (state == RenderSceneState.GameplayWithoutHero)
            {
                heroLayer = -1;
                visibleMask = 0;
                RestoreMenuBackgroundIfActive();
                return;
            }

            SetHeroLayer(hero.gameObject.layer);

            if (heroLayer < 0 || visibleMask == 0)
            {
                RestoreMenuBackgroundIfActive();
                return;
            }

            RestoreMenuBackgroundIfActive();
            RestorePromotedEffects();
            PromotePreservedRenderers();
            PromoteEffects();
            ApplyCameraMask();
            StripAllMaterials();
        }

        private void RestoreMenuBackgroundIfActive()
        {
            if (!menuBackgroundMode)
            {
                return;
            }

            RestoreBackgrounds();
            RestoreCameraClearFlags();
            RestoreCameraMasks();
            menuBackgroundMode = false;
        }

        private void SetHeroLayer(int layer)
        {
            if (layer < 0 || layer > 31)
            {
                heroLayer = -1;
                visibleMask = 0;
                Log("ShaderStripMod ignored invalid hero layer " + layer + ".");
                return;
            }

            if (heroLayer == layer && visibleMask != 0)
            {
                return;
            }

            heroLayer = layer;
            visibleMask = 1 << heroLayer;
            AddLayerToMask("Attack", ref visibleMask);
            Log("ShaderStripMod hero layer = " + heroLayer + " (" + LayerMask.LayerToName(heroLayer) + "), visibleMask = 0x" + visibleMask.ToString("X8") + ".");
        }

        private void ApplyCameraMask()
        {
            Camera[] cameras=Camera.allCameras;
            Camera world=GetGameplayWorldCamera(cameras);
            foreach(var camera in cameras) ConfigureGameplayCamera(camera,world);
            ConfigureImageBackgroundForCamera(world,true);
        }
        private void ApplyMenuBackground()
        {
            Camera[] cameras = Camera.allCameras;
            if (cameras.Length == 0)
            {
                return;
            }

            if (!menuBackgroundMode)
            {
                RestoreShaders();
                RestorePromotedEffects();
                menuBackgroundMode = true;
            }

            if (!menuMaskCached)
            {
                cachedMenuMask = GetMenuVisibleMask();
                menuMaskCached = true;
            }

            Camera backgroundCamera=GetMenuBackgroundCamera(cameras);
            foreach(var camera in cameras) ConfigureMenuCamera(camera,backgroundCamera,cachedMenuMask);

            ConfigureImageBackgroundForCamera(backgroundCamera, true);
        }

        private Camera GetMenuBackgroundCamera(Camera[] cameras) { return GetBackgroundCamera(cameras); }

        private void ResetMenuBackgroundCache()
        {
            menuBackgroundMode = false;
            menuMaskCached = false;
            cachedMenuMask = 0;

        }

        private void StoreCameraState(Camera cam)
        {
            if (!strippedMasks.ContainsKey(cam))
            {
                strippedMasks[cam] = cam.cullingMask;
            }

            if (!strippedBackgrounds.ContainsKey(cam))
            {
                strippedBackgrounds[cam] = cam.backgroundColor;
            }

            if (!strippedClearFlags.ContainsKey(cam))
            {
                strippedClearFlags[cam] = cam.clearFlags;
            }
        }

        private Camera GetBackgroundCamera(Camera[] cameras)
        {
            Camera backgroundCamera = null;
            float lowestDepth = float.PositiveInfinity;

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam == null || cam.targetTexture != null)
                {
                    continue;
                }

                if (backgroundCamera == null || cam.depth < lowestDepth)
                {
                    backgroundCamera = cam;
                    lowestDepth = cam.depth;
                }
            }

            return backgroundCamera;
        }

        private int GetMenuVisibleMask()
        {
            int mask = 0;
            AddLayerToMask("UI", ref mask);
            AddLayerToMask("HUD", ref mask);
            AddLayerToMask("Menu", ref mask);
            AddLayerToMask("uGUI", ref mask);

            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj == null || obj.layer < 0 || obj.layer > 31)
                {
                    continue;
                }

                if (IsTextObject(obj))
                {
                    mask |= 1 << obj.layer;
                }
            }

            return mask;
        }

        private void AddLayerToMask(string layerName, ref int mask)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0 && layer <= 31)
            {
                mask |= 1 << layer;
            }
        }

        private bool IsTextObject(GameObject obj)
        {
            if (obj.GetComponent("TextMesh") != null ||
                obj.GetComponent("Text") != null ||
                obj.GetComponent("TMP_Text") != null ||
                obj.GetComponent("TextMeshPro") != null ||
                obj.GetComponent("TextMeshProUGUI") != null)
            {
                return true;
            }

            Component[] components = obj.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;
                if (typeName == "TMP_Text" ||
                    typeName == "Text" ||
                    typeName == "TextMesh" ||
                    typeName == "TextMeshPro" ||
                    typeName == "TextMeshProUGUI")
                {
                    return true;
                }
            }

            return false;
        }

        private void RestoreCameraMasks()
        {
            foreach (KeyValuePair<Camera, int> pair in strippedMasks)
            {
                if (pair.Key != null)
                {
                    pair.Key.cullingMask = pair.Value;
                }
            }

            strippedMasks.Clear();
        }

        private void RestoreCameraClearFlags()
        {
            foreach (KeyValuePair<Camera, CameraClearFlags> pair in strippedClearFlags)
            {
                if (pair.Key != null)
                {
                    pair.Key.clearFlags = pair.Value;
                }
            }

            strippedClearFlags.Clear();
            RestoreBackgroundCameraFrames();
        }

        private void RestoreBackgrounds()
        {
            foreach (KeyValuePair<Camera, Color> pair in strippedBackgrounds)
            {
                if (pair.Key != null)
                {
                    pair.Key.backgroundColor = pair.Value;
                }
            }

            strippedBackgrounds.Clear();
        }

        private void PromoteEffects()
        {
            if (heroLayer < 0)
            {
                return;
            }

            int found = 0;
            int promotedNow = 0;

            for (int i = 0; i < EffectTags.Length; i++)
            {
                GameObject[] tagged;
                try
                {
                    tagged = GameObject.FindGameObjectsWithTag(EffectTags[i]);
                }
                catch (System.Exception ex)
                {
                    Log("ShaderStripMod tag '" + EffectTags[i] + "' lookup failed: " + ex.Message);
                    continue;
                }

                for (int j = 0; j < tagged.Length; j++)
                {
                    found++;
                    if (PromoteObject(tagged[j]))
                    {
                        promotedNow++;
                    }
                }
            }

            HeroController hero = HeroController.SilentInstance;
            if (hero == null)
            {
                return;
            }

            Transform[] children = hero.gameObject.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null)
                {
                    continue;
                }

                string name = children[i].name;
                for (int j = 0; j < EffectNames.Length; j++)
                {
                    if (name.IndexOf(EffectNames[j], System.StringComparison.Ordinal) >= 0)
                    {
                        found++;
                        if (PromoteObject(children[i].gameObject))
                        {
                            promotedNow++;
                        }

                        break;
                    }
                }
            }

            if (Time.unscaledTime - lastPromoteLogTime > 5.0f)
            {
                lastPromoteLogTime = Time.unscaledTime;
                Log("ShaderStripMod promote scan: matched=" + found + ", promotedNow=" + promotedNow + ", tracked=" + promotedEffects.Count + ".");
            }
        }

        private bool PromoteObject(GameObject effect)
        {
            if (effect == null || heroLayer < 0)
            {
                return false;
            }

            bool promoted = false;
            Transform[] children = effect.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && PromoteSingleObject(children[i].gameObject))
                {
                    promoted = true;
                }
            }

            return promoted;
        }

        private bool PromoteSingleObject(GameObject effect)
        {
            if (!CanPromoteVisualEffectLayer(effect))
            {
                return false;
            }

            if (!promotedEffects.ContainsKey(effect))
            {
                promotedEffects[effect] = effect.layer;
            }

            effect.layer = heroLayer;
            return true;
        }

        private bool CanPromoteVisualEffectLayer(GameObject effect)
        {
            if (effect != null) { foreach (var renderer in effect.GetComponents<Renderer>()) if (HasActiveSpriteMask(renderer)) return false; }
            if (effect == null || effect.layer == heroLayer || heroLayer < 0)
            {
                return false;
            }

            if (effect.GetComponent<Renderer>() == null)
            {
                return false;
            }

            if (effect.GetComponent<Collider2D>() != null ||
                effect.GetComponent<Rigidbody2D>() != null ||
                effect.GetComponent<Joint2D>() != null)
            {
                return false;
            }

            return !HasUnsafeGameplayComponent(effect);
        }

        private bool HasUnsafeGameplayComponent(GameObject obj)
        {
            if (obj == null)
            {
                return true;
            }

            string[] unsafeComponentNames =
            {
                "DamageEnemies",
                "DamageHero",
                "PlayMakerFSM",
                "HealthManager",
                "HitTaker",
                "NonBouncer",
                "Recoil",
                "Rigidbody2D",
                "Collider2D"
            };

            for (int i = 0; i < unsafeComponentNames.Length; i++)
            {
                try
                {
                    if (obj.GetComponent(unsafeComponentNames[i]) != null)
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private void RestorePromotedEffects()
        {
            foreach (KeyValuePair<GameObject, int> pair in promotedEffects)
            {
                if (pair.Key != null)
                {
                    pair.Key.layer = pair.Value;
                }
            }

            promotedEffects.Clear();
            promotedProjectileVisuals.Clear();
        }

    }
}




