using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ShaderStripMod.Runtime;
using UnityEngine;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private const string Part0RuntimeGateMarkerFileName = "ShaderStripMod.part0-runtime-gate.txt";
        private const float Part0RuntimeGateWarmupSeconds = 5f;
        private const float Part0RuntimeGateSampleSeconds = 60f;
        private const float Part0RuntimeGateTintSeconds = 1.5f;
        private const float Part0RuntimeGateRestoreDelaySeconds = 0.5f;

        private enum Part0RuntimeGatePhase
        {
            Warmup,
            Sampling,
            Tinted,
            Restored
        }

        private sealed class Part0RuntimeGateState
        {
            internal string RunId = "";
            internal string MarkerPath = "";
            internal string ReportPath = "";
            internal Part0RuntimeGatePhase Phase;
            internal float PhaseStartedAt;
            internal string SceneName = "";
            internal string RendererPath = "";
            internal string RendererType = "";
            internal int MaterialIndex = -1;
            internal SpriteRenderer Renderer;
            internal Material OriginalMaterial;
            internal Material OwnedMaterial;
            internal Shader OwnedShaderBeforeApplyStrip;
            internal Color OriginalColor;
            internal Color ExpectedTintColor;
            internal ExactMaterialOverrideLease Lease;
            internal int OwnedMaterialInstanceId;
            internal bool OwnedRegistered;
            internal bool TintApplied;
            internal bool BindingOwnedAfterTint;
            internal bool TintColorVerified;
            internal bool StripGuardVerified;
            internal bool BindingOwnedAfterApplyStrip;
            internal bool ShaderUnchangedAfterApplyStrip;
            internal bool TintUnchangedAfterApplyStrip;
            internal bool RestoreCalledThroughRestoreAll;
            internal bool RestoreReturnedTrue;
            internal bool OriginalBindingRestored;
            internal bool OriginalColorUnchanged;
            internal bool OwnedDestroyedAfterDelay;
            internal string RestoreReason = "";
            internal int SampleFrames;
            internal double SampleDeltaSeconds;
            internal double MaxFrameDeltaSeconds;
            internal long ManagedMemoryStart;
            internal long ManagedMemoryEnd;
            internal int Gen0Start;
            internal int Gen1Start;
            internal int Gen2Start;
            internal int Gen0End;
            internal int Gen1End;
            internal int Gen2End;
            internal double ApplyStripMsBefore;
            internal double ApplyStripMsAfter;
        }

        private Part0RuntimeGateState part0RuntimeGate;
        private HashSet<int> visualOverrideOwnedMaterialIds;

        private void InitializePart0RuntimeGate()
        {
            string directory = GetPart0RuntimeGateDirectory();
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            string markerPath = Path.Combine(directory, Part0RuntimeGateMarkerFileName);
            if (!File.Exists(markerPath))
            {
                return;
            }

            try
            {
                string runId = SanitizePart0RuntimeGateRunId(File.ReadAllText(markerPath, Encoding.UTF8));
                if (string.IsNullOrEmpty(runId))
                {
                    runId = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                }

                string reportPath = Path.Combine(directory, "ShaderStripMod.part0-runtime-gate." + runId + ".report.txt");
                if (File.Exists(reportPath))
                {
                    Log("ShaderStripMod Part 0 runtime gate skipped: report already exists for " + runId + ".");
                    return;
                }

                visualOverrideOwnedMaterialIds = new HashSet<int>();
                part0RuntimeGate = new Part0RuntimeGateState
                {
                    RunId = runId,
                    MarkerPath = markerPath,
                    ReportPath = reportPath,
                    Phase = Part0RuntimeGatePhase.Warmup,
                    PhaseStartedAt = Time.realtimeSinceStartup
                };
                Log("ShaderStripMod Part 0 runtime gate " + runId + " enabled. Warmup started; normal behavior is unchanged until the diagnostic Tint step.");
            }
            catch (Exception ex)
            {
                part0RuntimeGate = null;
                visualOverrideOwnedMaterialIds = null;
                Log("ShaderStripMod Part 0 runtime gate initialization failed: " + ex.Message);
            }
        }

        private void UpdatePart0RuntimeGate()
        {
            Part0RuntimeGateState state = part0RuntimeGate;
            if (state == null)
            {
                return;
            }

            try
            {
                float now = Time.realtimeSinceStartup;
                if (state.Phase == Part0RuntimeGatePhase.Warmup)
                {
                    if (now - state.PhaseStartedAt < Part0RuntimeGateWarmupSeconds)
                    {
                        return;
                    }

                    state.Phase = Part0RuntimeGatePhase.Sampling;
                    state.PhaseStartedAt = now;
                    state.ManagedMemoryStart = GC.GetTotalMemory(false);
                    state.Gen0Start = GC.CollectionCount(0);
                    state.Gen1Start = GC.CollectionCount(1);
                    state.Gen2Start = GC.CollectionCount(2);
                    state.ApplyStripMsBefore = lastApplyStripMs;
                    Log("ShaderStripMod Part 0 runtime gate " + state.RunId + " sampling frame/GC baseline for 60 seconds.");
                    return;
                }

                if (state.Phase == Part0RuntimeGatePhase.Sampling)
                {
                    double frameDelta = Time.unscaledDeltaTime;
                    state.SampleFrames++;
                    state.SampleDeltaSeconds += frameDelta;
                    if (frameDelta > state.MaxFrameDeltaSeconds)
                    {
                        state.MaxFrameDeltaSeconds = frameDelta;
                    }

                    if (now - state.PhaseStartedAt < Part0RuntimeGateSampleSeconds)
                    {
                        return;
                    }

                    state.ManagedMemoryEnd = GC.GetTotalMemory(false);
                    state.Gen0End = GC.CollectionCount(0);
                    state.Gen1End = GC.CollectionCount(1);
                    state.Gen2End = GC.CollectionCount(2);
                    BeginPart0RuntimeGateTint(state, now);
                    return;
                }

                if (state.Phase == Part0RuntimeGatePhase.Tinted)
                {
                    if (now - state.PhaseStartedAt < Part0RuntimeGateTintSeconds)
                    {
                        return;
                    }

                    VerifyPart0RuntimeGateApplyStripAndRestore(state);
                    return;
                }

                if (state.Phase == Part0RuntimeGatePhase.Restored &&
                    now - state.PhaseStartedAt >= Part0RuntimeGateRestoreDelaySeconds)
                {
                    CompletePart0RuntimeGate(state);
                }
            }
            catch (Exception ex)
            {
                FailPart0RuntimeGate(state, "Unhandled gate exception: " + ex);
            }
        }

        private void BeginPart0RuntimeGateTint(Part0RuntimeGateState state, float now)
        {
            int materialIndex;
            SpriteRenderer renderer = FindPart0RuntimeGateRenderer(out materialIndex);
            if (renderer == null)
            {
                FailPart0RuntimeGate(state, "No active SpriteRenderer with a _Color material was found after baseline sampling.");
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            Material original = materials[materialIndex];
            Color originalColor = original.color;
            ExactMaterialOverrideLease lease;
            string error;
            if (!ExactMaterialOverrideLease.TryCreate(renderer, materialIndex, out lease, out error))
            {
                FailPart0RuntimeGate(state, "Exact material clone failed: " + error);
                return;
            }

            Material owned = lease.Material;
            state.SceneName = GetActiveSceneName();
            state.RendererPath = GetObjectPath(renderer.gameObject);
            state.RendererType = renderer.GetType().Name;
            state.MaterialIndex = materialIndex;
            state.Renderer = renderer;
            state.OriginalMaterial = original;
            state.OwnedMaterial = owned;
            state.OwnedShaderBeforeApplyStrip = owned != null ? owned.shader : null;
            state.OriginalColor = originalColor;
            state.Lease = lease;
            state.OwnedMaterialInstanceId = owned != null ? owned.GetInstanceID() : 0;
            RegisterVisualOverrideOwnedMaterial(owned);
            state.OwnedRegistered = IsVisualOverrideOwnedMaterial(owned);

            Color tint = new Color(1f, 0.2f, 0.85f, 1f);
            const float intensity = 0.75f;
            Color multiplied = originalColor * tint;
            multiplied.a = originalColor.a;
            state.ExpectedTintColor = Color.Lerp(originalColor, multiplied, intensity);
            state.TintApplied = lease.TryApplyTint(tint, intensity, true, out error);
            if (!state.TintApplied)
            {
                FailPart0RuntimeGate(state, "Tint application failed: " + error);
                return;
            }

            Material[] after = renderer.sharedMaterials;
            state.BindingOwnedAfterTint = after != null && materialIndex < after.Length && ReferenceEquals(after[materialIndex], owned);
            state.TintColorVerified = owned != null && ColorsApproximatelyEqual(owned.color, state.ExpectedTintColor);
            Shader basic = Shader.Find("Sprites/Default");
            state.StripGuardVerified = state.OwnedRegistered && ShouldSkipMaterialDuringStrip(renderer, owned, basic);
            state.Phase = Part0RuntimeGatePhase.Tinted;
            state.PhaseStartedAt = now;
            Log("ShaderStripMod Part 0 runtime gate tinted " + state.SceneName + "/" + state.RendererPath + " for visual inspection.");
        }

        private void VerifyPart0RuntimeGateApplyStripAndRestore(Part0RuntimeGateState state)
        {
            ApplyStrip();
            state.ApplyStripMsAfter = lastApplyStripMs;

            Material[] afterApply = state.Renderer != null ? state.Renderer.sharedMaterials : null;
            Material current = afterApply != null && state.MaterialIndex >= 0 && state.MaterialIndex < afterApply.Length
                ? afterApply[state.MaterialIndex]
                : null;
            state.BindingOwnedAfterApplyStrip = ReferenceEquals(current, state.OwnedMaterial);
            state.ShaderUnchangedAfterApplyStrip = state.OwnedMaterial != null &&
                ReferenceEquals(state.OwnedMaterial.shader, state.OwnedShaderBeforeApplyStrip);
            state.TintUnchangedAfterApplyStrip = state.OwnedMaterial != null &&
                ColorsApproximatelyEqual(state.OwnedMaterial.color, state.ExpectedTintColor);

            state.RestoreCalledThroughRestoreAll = true;
            RestoreAll();
        }

        private void RestorePart0RuntimeGate(string reason)
        {
            Part0RuntimeGateState state = part0RuntimeGate;
            if (state == null || state.Lease == null)
            {
                return;
            }

            bool expectedRestore = state.RestoreCalledThroughRestoreAll &&
                string.Equals(reason, "RestoreAll", StringComparison.Ordinal);
            Material owned = state.OwnedMaterial;
            string restoreReason;
            state.RestoreReturnedTrue = state.Lease.TryRestore(out restoreReason);
            state.RestoreReason = restoreReason;
            UnregisterVisualOverrideOwnedMaterial(state.OwnedMaterialInstanceId);
            state.Lease = null;

            Material[] afterRestore = state.Renderer != null ? state.Renderer.sharedMaterials : null;
            Material current = afterRestore != null && state.MaterialIndex >= 0 && state.MaterialIndex < afterRestore.Length
                ? afterRestore[state.MaterialIndex]
                : null;
            state.OriginalBindingRestored = ReferenceEquals(current, state.OriginalMaterial);
            state.OriginalColorUnchanged = state.OriginalMaterial != null &&
                ColorsApproximatelyEqual(state.OriginalMaterial.color, state.OriginalColor);

            if (expectedRestore)
            {
                state.Phase = Part0RuntimeGatePhase.Restored;
                state.PhaseStartedAt = Time.realtimeSinceStartup;
                return;
            }

            state.OwnedDestroyedAfterDelay = owned == null;
            WritePart0RuntimeGateReport(state, false, "Gate was interrupted by " + reason + ".");
            part0RuntimeGate = null;
            visualOverrideOwnedMaterialIds = null;
        }

        private void CompletePart0RuntimeGate(Part0RuntimeGateState state)
        {
            state.OwnedDestroyedAfterDelay = state.OwnedMaterial == null;
            bool passed = state.OwnedRegistered &&
                state.TintApplied &&
                state.BindingOwnedAfterTint &&
                state.TintColorVerified &&
                state.StripGuardVerified &&
                state.BindingOwnedAfterApplyStrip &&
                state.ShaderUnchangedAfterApplyStrip &&
                state.TintUnchangedAfterApplyStrip &&
                state.RestoreCalledThroughRestoreAll &&
                state.RestoreReturnedTrue &&
                state.OriginalBindingRestored &&
                state.OriginalColorUnchanged &&
                state.OwnedDestroyedAfterDelay;
            WritePart0RuntimeGateReport(state, passed, passed ? "All runtime gate assertions passed." : "One or more runtime gate assertions failed.");
            part0RuntimeGate = null;
            visualOverrideOwnedMaterialIds = null;
        }

        private void FailPart0RuntimeGate(Part0RuntimeGateState state, string message)
        {
            if (state != null && state.Lease != null)
            {
                string restoreReason;
                state.RestoreReturnedTrue = state.Lease.TryRestore(out restoreReason);
                state.RestoreReason = restoreReason;
                UnregisterVisualOverrideOwnedMaterial(state.OwnedMaterialInstanceId);
                state.Lease = null;
            }

            WritePart0RuntimeGateReport(state, false, message);
            part0RuntimeGate = null;
            visualOverrideOwnedMaterialIds = null;
        }

        private void WritePart0RuntimeGateReport(Part0RuntimeGateState state, bool passed, string message)
        {
            if (state == null)
            {
                Log("ShaderStripMod Part 0 runtime gate failed before state creation: " + message);
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("ShaderStripMod Part 0 runtime gate report");
            AppendPart0GateValue(builder, "result", passed ? "PASS" : "FAIL");
            AppendPart0GateValue(builder, "message", message);
            AppendPart0GateValue(builder, "runId", state.RunId);
            AppendPart0GateValue(builder, "timestamp", DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "scene", state.SceneName);
            AppendPart0GateValue(builder, "rendererPath", state.RendererPath);
            AppendPart0GateValue(builder, "rendererType", state.RendererType);
            AppendPart0GateValue(builder, "materialIndex", state.MaterialIndex.ToString(CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "ownedRegistered", FormatPart0GateBool(state.OwnedRegistered));
            AppendPart0GateValue(builder, "tintApplied", FormatPart0GateBool(state.TintApplied));
            AppendPart0GateValue(builder, "bindingOwnedAfterTint", FormatPart0GateBool(state.BindingOwnedAfterTint));
            AppendPart0GateValue(builder, "tintColorVerified", FormatPart0GateBool(state.TintColorVerified));
            AppendPart0GateValue(builder, "stripGuardVerified", FormatPart0GateBool(state.StripGuardVerified));
            AppendPart0GateValue(builder, "bindingOwnedAfterApplyStrip", FormatPart0GateBool(state.BindingOwnedAfterApplyStrip));
            AppendPart0GateValue(builder, "shaderUnchangedAfterApplyStrip", FormatPart0GateBool(state.ShaderUnchangedAfterApplyStrip));
            AppendPart0GateValue(builder, "tintUnchangedAfterApplyStrip", FormatPart0GateBool(state.TintUnchangedAfterApplyStrip));
            AppendPart0GateValue(builder, "restoreCalledThroughRestoreAll", FormatPart0GateBool(state.RestoreCalledThroughRestoreAll));
            AppendPart0GateValue(builder, "restoreReturnedTrue", FormatPart0GateBool(state.RestoreReturnedTrue));
            AppendPart0GateValue(builder, "restoreReason", state.RestoreReason);
            AppendPart0GateValue(builder, "originalBindingRestored", FormatPart0GateBool(state.OriginalBindingRestored));
            AppendPart0GateValue(builder, "originalColorUnchanged", FormatPart0GateBool(state.OriginalColorUnchanged));
            AppendPart0GateValue(builder, "ownedDestroyedAfterDelay", FormatPart0GateBool(state.OwnedDestroyedAfterDelay));
            AppendPart0GateValue(builder, "sampleSeconds", Part0RuntimeGateSampleSeconds.ToString("0.###", CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "sampleFrames", state.SampleFrames.ToString(CultureInfo.InvariantCulture));
            double meanFrameMs = state.SampleFrames > 0 ? state.SampleDeltaSeconds * 1000.0 / state.SampleFrames : 0.0;
            AppendPart0GateValue(builder, "meanFrameDeltaMs", meanFrameMs.ToString("0.###", CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "maxFrameDeltaMs", (state.MaxFrameDeltaSeconds * 1000.0).ToString("0.###", CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "managedMemoryStart", state.ManagedMemoryStart.ToString(CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "managedMemoryEnd", state.ManagedMemoryEnd.ToString(CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "managedMemoryDelta", (state.ManagedMemoryEnd - state.ManagedMemoryStart).ToString(CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "gen0Collections", (state.Gen0End - state.Gen0Start).ToString(CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "gen1Collections", (state.Gen1End - state.Gen1Start).ToString(CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "gen2Collections", (state.Gen2End - state.Gen2Start).ToString(CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "lastApplyStripMsBefore", state.ApplyStripMsBefore.ToString("0.###", CultureInfo.InvariantCulture));
            AppendPart0GateValue(builder, "lastApplyStripMsAfter", state.ApplyStripMsAfter.ToString("0.###", CultureInfo.InvariantCulture));

            try
            {
                File.WriteAllText(state.ReportPath, builder.ToString(), new UTF8Encoding(false));
                Log("ShaderStripMod Part 0 runtime gate " + state.RunId + " result: " + (passed ? "PASS" : "FAIL") + ". Report: " + state.ReportPath);
            }
            catch (Exception ex)
            {
                Log("ShaderStripMod Part 0 runtime gate report write failed: " + ex.Message + ". Result was " + (passed ? "PASS" : "FAIL") + ": " + message);
            }
        }

        private SpriteRenderer FindPart0RuntimeGateRenderer(out int materialIndex)
        {
            materialIndex = -1;
            HeroController hero = HeroController.SilentInstance;
            if (hero != null)
            {
                SpriteRenderer[] heroRenderers = hero.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < heroRenderers.Length; i++)
                {
                    if (TryGetPart0RuntimeGateMaterialIndex(heroRenderers[i], out materialIndex))
                    {
                        return heroRenderers[i];
                    }
                }
            }

            SpriteRenderer fallback = null;
            int fallbackIndex = -1;
            SpriteRenderer[] renderers = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                int candidateIndex;
                if (!TryGetPart0RuntimeGateMaterialIndex(renderers[i], out candidateIndex))
                {
                    continue;
                }

                if (renderers[i].isVisible)
                {
                    materialIndex = candidateIndex;
                    return renderers[i];
                }

                if (fallback == null)
                {
                    fallback = renderers[i];
                    fallbackIndex = candidateIndex;
                }
            }

            materialIndex = fallbackIndex;
            return fallback;
        }

        private bool TryGetPart0RuntimeGateMaterialIndex(SpriteRenderer renderer, out int materialIndex)
        {
            materialIndex = -1;
            if (renderer == null || renderer.gameObject == null || !renderer.gameObject.activeInHierarchy || !renderer.enabled || IsProxyRenderer(renderer))
            {
                return false;
            }

            UnityEngine.SceneManagement.Scene scene = renderer.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].HasProperty("_Color"))
                {
                    materialIndex = i;
                    return true;
                }
            }

            return false;
        }

        private void RegisterVisualOverrideOwnedMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (visualOverrideOwnedMaterialIds == null)
            {
                visualOverrideOwnedMaterialIds = new HashSet<int>();
            }

            visualOverrideOwnedMaterialIds.Add(material.GetInstanceID());
        }

        private void UnregisterVisualOverrideOwnedMaterial(int instanceId)
        {
            if (visualOverrideOwnedMaterialIds != null && instanceId != 0)
            {
                visualOverrideOwnedMaterialIds.Remove(instanceId);
            }
        }

        private bool IsVisualOverrideOwnedMaterial(Material material)
        {
            return material != null &&
                visualOverrideOwnedMaterialIds != null &&
                visualOverrideOwnedMaterialIds.Contains(material.GetInstanceID());
        }

        private string GetPart0RuntimeGateDirectory()
        {
            try
            {
                if (!string.IsNullOrEmpty(settingsFilePath))
                {
                    return Path.GetDirectoryName(settingsFilePath);
                }

                if (!string.IsNullOrEmpty(preserveFilePath))
                {
                    return Path.GetDirectoryName(preserveFilePath);
                }
            }
            catch
            {
            }

            return "";
        }

        private string SanitizePart0RuntimeGateRunId(string value)
        {
            StringBuilder builder = new StringBuilder();
            string source = (value ?? "").Trim();
            for (int i = 0; i < source.Length && builder.Length < 80; i++)
            {
                char current = source[i];
                if (char.IsLetterOrDigit(current) || current == '-' || current == '_' || current == '.')
                {
                    builder.Append(current);
                }
            }

            return builder.ToString();
        }

        private bool ColorsApproximatelyEqual(Color left, Color right)
        {
            const float tolerance = 0.002f;
            return Mathf.Abs(left.r - right.r) <= tolerance &&
                Mathf.Abs(left.g - right.g) <= tolerance &&
                Mathf.Abs(left.b - right.b) <= tolerance &&
                Mathf.Abs(left.a - right.a) <= tolerance;
        }

        private static void AppendPart0GateValue(StringBuilder builder, string key, string value)
        {
            builder.Append(key).Append('=').AppendLine((value ?? "").Replace("\r", " ").Replace("\n", " "));
        }

        private static string FormatPart0GateBool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
