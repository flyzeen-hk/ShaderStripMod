using System;
using System.Collections.Generic;
using Modding;
using On;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed class ShaderStripMod : Mod, ITogglableMod
    {
        private sealed class ScanBehaviour : MonoBehaviour
        {
            private float nextScanTime;

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.F12))
                {
                    Instance?.Toggle();
                }
                else if (!(Time.unscaledTime < nextScanTime))
                {
                    nextScanTime = Time.unscaledTime + 1f;
                    if (Instance != null && Instance.active)
                    {
                        Instance.ApplyStrip();
                    }
                }
            }
        }

        private readonly Dictionary<Material, Shader> strippedShaders = new Dictionary<Material, Shader>();
        private readonly Dictionary<Camera, Color> strippedBackgrounds = new Dictionary<Camera, Color>();
        private readonly Dictionary<Camera, int> strippedMasks = new Dictionary<Camera, int>();
        private GameObject runner;
        private bool active;
        private int heroLayer = -1;

        internal static ShaderStripMod Instance { get; private set; }

        public override string GetVersion()
        {
            return "1.2.0";
        }

        public override void Initialize()
        {
            if (!active)
            {
                active = true;
                Instance = this;
                heroLayer = -1;
                runner = new GameObject("ShaderStripMod.Runner");
                UnityEngine.Object.DontDestroyOnLoad(runner);
                runner.AddComponent<ScanBehaviour>();
                ModHooks.SceneChanged += OnSceneChanged;
                On.HeroController.Start += OnHeroStart;
                Log("ShaderStripMod initialized. Shaders stripped, only hero layer visible, green background. F12 toggles.");
                ApplyStrip();
            }
        }

        public void Unload()
        {
            if (active)
            {
                ModHooks.SceneChanged -= OnSceneChanged;
                On.HeroController.Start -= OnHeroStart;
                if (runner != null)
                {
                    UnityEngine.Object.Destroy(runner);
                    runner = null;
                }

                RestoreShaders();
                RestoreBackgrounds();
                RestoreCameraMasks();
                active = false;
                Instance = null;
                Log("ShaderStripMod unloaded. Original shaders, colors and camera masks restored.");
            }
        }

        internal void Toggle()
        {
            if (active)
            {
                ForceStop();
                return;
            }

            ApplyStrip();
            active = true;
            Log("ShaderStripMod visuals applied.");
        }

        internal void ForceStop()
        {
            if (active)
            {
                RestoreShaders();
                RestoreBackgrounds();
                RestoreCameraMasks();
                active = false;
                Log("ShaderStripMod visuals reverted.");
            }
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
            strippedBackgrounds.Clear();
            strippedMasks.Clear();
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
            if (heroLayer < 0)
            {
                HeroController silentInstance = HeroController.SilentInstance;
                if (silentInstance != null)
                {
                    heroLayer = silentInstance.gameObject.layer;
                }
            }
        }

        private void ApplyCameraMask()
        {
            Camera world = Camera.main != null ? Camera.main : (Camera.allCameras.Length != 0 ? Camera.allCameras[0] : null);
            Camera[] cameras = Camera.allCameras;
            foreach (Camera cam in cameras)
            {
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
                    if (heroLayer >= 0)
                    {
                        cam.cullingMask = 1 << heroLayer;
                    }
                }
                else
                {
                    cam.cullingMask = 0;
                }

                if (!strippedBackgrounds.ContainsKey(cam))
                {
                    strippedBackgrounds[cam] = cam.backgroundColor;
                }

                cam.backgroundColor = new Color(0f, 1f, 0f, 1f);
            }
        }

        private void RestoreCameraMasks()
        {
            foreach (KeyValuePair<Camera, int> strippedMask in strippedMasks)
            {
                if (strippedMask.Key != null)
                {
                    strippedMask.Key.cullingMask = strippedMask.Value;
                }
            }

            strippedMasks.Clear();
        }

        private void RestoreBackgrounds()
        {
            foreach (KeyValuePair<Camera, Color> strippedBackground in strippedBackgrounds)
            {
                if (strippedBackground.Key != null)
                {
                    strippedBackground.Key.backgroundColor = strippedBackground.Value;
                }
            }

            strippedBackgrounds.Clear();
        }

        private void StripAllMaterials()
        {
            Shader basic = Shader.Find("Sprites/Default");
            if (basic == null)
            {
                return;
            }

            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] sharedMaterials = renderer.sharedMaterials;
                bool changed = false;
                foreach (Material material in sharedMaterials)
                {
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
                    renderer.sharedMaterials = sharedMaterials;
                }
            }
        }

        private void RestoreShaders()
        {
            foreach (KeyValuePair<Material, Shader> strippedShader in strippedShaders)
            {
                if (strippedShader.Key != null)
                {
                    strippedShader.Key.shader = strippedShader.Value;
                }
            }

            strippedShaders.Clear();
        }

        public ShaderStripMod()
            : base(null)
        {
        }
    }
}
