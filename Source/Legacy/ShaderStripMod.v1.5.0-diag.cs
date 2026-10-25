using System.Collections.Generic;
using Modding;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed class ShaderStripMod : Mod, ITogglableMod
    {
        private readonly Dictionary<Material, Shader> strippedShaders = new Dictionary<Material, Shader>();
        private readonly Dictionary<Camera, Color> strippedBackgrounds = new Dictionary<Camera, Color>();
        private readonly Dictionary<Camera, int> strippedMasks = new Dictionary<Camera, int>();
        private GameObject runner;
        private bool active;
        private int heroLayer = -1;
        private int fullMask;

        internal static ShaderStripMod Instance { get; private set; }

        public override string GetVersion()
        {
            return "1.5.0";
        }

        public override void Initialize()
        {
            if (active)
            {
                return;
            }

            active = true;
            Instance = this;
            heroLayer = -1;
            fullMask = 0;

            runner = new GameObject("ShaderStripMod.Runner");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<ScanBehaviour>();

            ModHooks.SceneChanged += OnSceneChanged;
            On.HeroController.Start += OnHeroStart;

            Log("ShaderStripMod v1.5.0-diag initialized. Hero, attacks and spell projectiles visible, green background. F12 toggles.");
            ApplyStrip();
        }

        public void Unload()
        {
            if (!active)
            {
                return;
            }

            ModHooks.SceneChanged -= OnSceneChanged;
            On.HeroController.Start -= OnHeroStart;

            if (runner != null)
            {
                Object.Destroy(runner);
                runner = null;
            }

            RestoreShaders();
            RestoreBackgrounds();
            RestoreCameraMasks();
            RestorePromotedEffects();
            active = false;
            Instance = null;
            Log("ShaderStripMod unloaded. Original shaders, colors and camera masks restored.");
        }

        internal void Toggle()
        {
            if (active)
            {
                ForceStop();
            }
            else
            {
                ApplyStrip();
                active = true;
                Log("ShaderStripMod visuals applied.");
            }
        }

        internal void ForceStop()
        {
            if (!active)
            {
                return;
            }

            RestoreShaders();
            RestoreBackgrounds();
            RestoreCameraMasks();
            RestorePromotedEffects();
            active = false;
            Log("ShaderStripMod visuals reverted.");
        }

        private void OnHeroStart(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);

            if (self != null)
            {
                heroLayer = self.gameObject.layer;
            }

            if (active)
            {
                ApplyStrip();
            }
        }

        private void OnSceneChanged(string sceneName)
        {
            heroLayer = -1;
            fullMask = 0;
            strippedBackgrounds.Clear();
            strippedMasks.Clear();
            promotedEffects.Clear();
            Log("ShaderStripMod scene changed to " + sceneName + ".");

            if (active)
            {
                ApplyStrip();
            }
        }

        internal void ApplyStrip()
        {
            RefreshHeroLayer();
            ApplyCameraMask();
            StripAllMaterials();
        }

        private void RefreshHeroLayer()
        {
            if (heroLayer >= 0)
            {
                return;
            }

            HeroController hero = HeroController.SilentInstance;
            if (hero != null)
            {
                heroLayer = hero.gameObject.layer;
                fullMask = 1 << heroLayer;
                Log("ShaderStripMod hero layer = " + heroLayer + " (" + LayerMask.LayerToName(heroLayer) + "), fullMask = " + fullMask + ".");
            }
            else
            {
                Log("ShaderStripMod hero not found yet, fullMask stays 0 (all layers visible!).");
            }
        }

        private void ApplyCameraMask()
        {
            Camera world = Camera.main != null ? Camera.main : (Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null);
            Camera[] cameras = Camera.allCameras;
            Log("ShaderStripMod cameras: " + cameras.Length + ", main = " + (Camera.main != null ? Camera.main.name : "null") + ", world = " + (world != null ? world.name : "null") + ", fullMask = " + fullMask + ".");
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam == null)
                {
                    continue;
                }

                if (!strippedMasks.ContainsKey(cam))
                {
                    strippedMasks[cam] = cam.cullingMask;
                }

                if (cam == world)
                {
                    if (fullMask != 0)
                    {
                        cam.cullingMask = fullMask;
                    }
                }
                else
                {
                    cam.cullingMask = 0;
                }

                Log("ShaderStripMod cam '" + cam.name + "' mask 0x" + strippedMasks[cam].ToString("X8") + " -> 0x" + cam.cullingMask.ToString("X8") + ", tag = " + cam.tag + ".");

                if (!strippedBackgrounds.ContainsKey(cam))
                {
                    strippedBackgrounds[cam] = cam.backgroundColor;
                }

                cam.backgroundColor = new Color(0f, 1f, 0f, 1f);
            }
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

        private static readonly string[] EffectTags = { "Nail Attack", "Hero Spell" };
        private static readonly string[] EffectNames = { "Slash", "Fireball", "Focus", "Scream", "Dive", "Shield", "Soul" };
        private readonly Dictionary<GameObject, int> promotedEffects = new Dictionary<GameObject, int>();
        private int lastPromoteLog;

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

            if (Time.unscaledTime - lastPromoteLog > 5.0f)
            {
                lastPromoteLog = (int)Time.unscaledTime;
                Log("ShaderStripMod promote scan: matched=" + found + ", promotedNow=" + promotedNow + ", tracked=" + promotedEffects.Count + ".");
            }
        }

        private bool PromoteObject(GameObject effect)
        {
            if (effect == null || effect.layer == heroLayer)
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
        }

        private void StripAllMaterials()
        {
            Shader basic = Shader.Find("Sprites/Default");
            if (basic == null)
            {
                return;
            }

            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (material == null || material.shader == basic)
                    {
                        continue;
                    }

                    if (!strippedShaders.ContainsKey(material))
                    {
                        strippedShaders[material] = material.shader;
                    }

                    material.shader = basic;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private void RestoreShaders()
        {
            foreach (KeyValuePair<Material, Shader> pair in strippedShaders)
            {
                if (pair.Key != null)
                {
                    pair.Key.shader = pair.Value;
                }
            }

            strippedShaders.Clear();
        }

        private sealed class ScanBehaviour : MonoBehaviour
        {
            private float nextScanTime;

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.F12))
                {
                    ShaderStripMod.Instance?.Toggle();
                    return;
                }

                if (ShaderStripMod.Instance != null && ShaderStripMod.Instance.active)
                {
                    ShaderStripMod.Instance.PromoteEffects();
                }

                if (Time.unscaledTime < nextScanTime)
                {
                    return;
                }

                nextScanTime = Time.unscaledTime + 1.0f;
                if (ShaderStripMod.Instance != null && ShaderStripMod.Instance.active)
                {
                    ShaderStripMod.Instance.ApplyStrip();
                }
            }
        }
    }
}
