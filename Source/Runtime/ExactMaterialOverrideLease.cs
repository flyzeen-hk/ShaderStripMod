using System;
using UnityEngine;

namespace ShaderStripMod.Runtime
{
    internal sealed class ExactMaterialOverrideLease : IDisposable
    {
        private readonly Renderer renderer;
        private readonly int materialIndex;
        private readonly OwnedBinding<Material> binding;
        private bool released;

        private ExactMaterialOverrideLease(Renderer renderer, int materialIndex, Material original, Material owned)
        {
            this.renderer = renderer;
            this.materialIndex = materialIndex;
            binding = new OwnedBinding<Material>(original, owned);
        }

        internal Material Material
        {
            get { return binding.Owned; }
        }

        internal static bool TryCreate(Renderer renderer, int materialIndex, out ExactMaterialOverrideLease lease, out string error)
        {
            lease = null;
            error = "";
            if (renderer == null)
            {
                error = "Renderer is unavailable.";
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materialIndex < 0 || materialIndex >= materials.Length || materials[materialIndex] == null)
            {
                error = "The requested material slot is unavailable.";
                return false;
            }

            Material original = materials[materialIndex];
            Material owned = null;
            try
            {
                owned = new Material(original);
                owned.name = original.name + " [ShaderStripMod VisualOverride]";
                materials[materialIndex] = owned;
                renderer.sharedMaterials = materials;
                lease = new ExactMaterialOverrideLease(renderer, materialIndex, original, owned);
                return true;
            }
            catch (Exception ex)
            {
                if (owned != null)
                {
                    UnityEngine.Object.Destroy(owned);
                }

                error = "Material clone failed: " + ex.Message;
                return false;
            }
        }

        internal bool TryApplyTint(Color tint, float intensity, bool preserveAlpha, out string error)
        {
            error = "";
            if (released || binding.Owned == null)
            {
                error = "Material override lease is no longer active.";
                return false;
            }

            if (!binding.Owned.HasProperty("_Color"))
            {
                error = "The cloned material has no _Color property.";
                return false;
            }

            float blend = Mathf.Clamp01(intensity);
            Color original = binding.Original != null && binding.Original.HasProperty("_Color")
                ? binding.Original.color
                : Color.white;
            Color multiplied = original * tint;
            if (preserveAlpha)
            {
                multiplied.a = original.a;
            }

            binding.Owned.color = Color.Lerp(original, multiplied, blend);
            return true;
        }

        internal bool TryRestore(out string reason)
        {
            if (released)
            {
                reason = "Material override was already released.";
                return true;
            }

            released = true;
            bool restored = false;
            if (renderer != null)
            {
                Material[] current = renderer.sharedMaterials;
                if (current != null && materialIndex >= 0 && materialIndex < current.Length)
                {
                    Material restore;
                    if (binding.TryGetRestoreValue(current[materialIndex], out restore))
                    {
                        current[materialIndex] = restore;
                        renderer.sharedMaterials = current;
                        restored = true;
                    }
                }
            }

            if (binding.Owned != null)
            {
                UnityEngine.Object.Destroy(binding.Owned);
            }

            reason = restored
                ? "Original material binding restored."
                : "Owned binding was replaced externally; restore did not overwrite the newer binding.";
            return restored;
        }

        public void Dispose()
        {
            string ignored;
            TryRestore(out ignored);
        }
    }
}
