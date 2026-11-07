using System.Collections.Generic;
using System.Text;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private static void RunCapturePresetChecks()
        {
            CapturePresetState selected = CapturePresetPolicy.Evaluate(CapturePresetMode.SelectedObjects);
            Check(selected.IsolationEnabled && !selected.SuspendPreserveRules && selected.ShowAttacks, "Selected objects keeps isolation, rules and attacks");
            CapturePresetState heroAttacks = CapturePresetPolicy.Evaluate(CapturePresetMode.HeroAndAttacks);
            Check(heroAttacks.IsolationEnabled && heroAttacks.SuspendPreserveRules && heroAttacks.ShowAttacks, "Hero + attacks suspends only preserve rules");
            CapturePresetState heroOnly = CapturePresetPolicy.Evaluate(CapturePresetMode.HeroOnly);
            Check(heroOnly.IsolationEnabled && heroOnly.SuspendPreserveRules && !heroOnly.ShowAttacks, "Hero only suspends rules and hides attacks");
            CapturePresetState fullScene = CapturePresetPolicy.Evaluate(CapturePresetMode.FullScene);
            Check(!fullScene.IsolationEnabled && !fullScene.SuspendPreserveRules && fullScene.ShowAttacks, "Full scene disables gameplay isolation");
            Check(CapturePresetPolicy.Evaluate((CapturePresetMode)99).Mode == CapturePresetMode.SelectedObjects, "Unknown capture mode falls back to selected objects");

            Check(CapturePresetPolicy.Resolve(true, false, true) == CapturePresetMode.SelectedObjects, "Resolve selected objects");
            Check(CapturePresetPolicy.Resolve(true, true, true) == CapturePresetMode.HeroAndAttacks, "Resolve hero + attacks");
            Check(CapturePresetPolicy.Resolve(true, true, false) == CapturePresetMode.HeroOnly, "Resolve hero only");
            Check(CapturePresetPolicy.Resolve(false, false, true) == CapturePresetMode.FullScene, "Resolve full scene");
            Check(CapturePresetPolicy.Resolve(false, true, false) == CapturePresetMode.FullScene, "Isolation off always resolves to full scene");
            foreach (CapturePresetMode mode in new[] { CapturePresetMode.SelectedObjects, CapturePresetMode.HeroAndAttacks, CapturePresetMode.HeroOnly, CapturePresetMode.FullScene })
            {
                CapturePresetState state = CapturePresetPolicy.Evaluate(mode);
                Check(CapturePresetPolicy.Resolve(state.IsolationEnabled, state.SuspendPreserveRules, state.ShowAttacks) == mode, "Capture mode round-trips through resolve: " + mode);
                Check(CapturePresetPolicy.GetEnglishName(mode).Length > 0, "Capture mode has an English name: " + mode);
            }

            Check(CapturePresetPolicy.GetEnglishName(CapturePresetMode.SelectedObjects) == "Selected objects", "Selected objects English name");
            Check(CapturePresetPolicy.GetEnglishName(CapturePresetMode.HeroAndAttacks) == "Hero + attacks", "Hero + attacks English name");
            Check(CapturePresetPolicy.GetEnglishName(CapturePresetMode.HeroOnly) == "Hero only", "Hero only English name");
            Check(CapturePresetPolicy.GetEnglishName(CapturePresetMode.FullScene) == "Full scene", "Full scene English name");

            Check(CapturePresetPolicy.NormalizeName("  Boss room  ") == "Boss room", "Preset name is trimmed");
            Check(CapturePresetPolicy.NormalizeName("a\nb\tc") == "a b c", "Preset name control characters become spaces");
            Check(CapturePresetPolicy.NormalizeName("   ").Length == 0, "Whitespace-only preset name becomes empty");
            Check(CapturePresetPolicy.NormalizeName(null).Length == 0, "Null preset name becomes empty");
            Check(CapturePresetPolicy.NormalizeName(new string('x', 50)).Length == 40, "Preset name is limited to 40 characters");
            Check(CapturePresetPolicy.NormalizeName("A=B") == "A=B", "Preset name keeps equals signs");

            Check(CapturePresetPolicy.IsSupportedRuleLine("path=Scene/Object"), "path rule is supported");
            Check(CapturePresetPolicy.IsSupportedRuleLine("groupPath=Scene/Group"), "groupPath rule is supported");
            Check(CapturePresetPolicy.IsSupportedRuleLine("projectilePath=Scene/Projectile"), "projectilePath rule is supported");
            Check(CapturePresetPolicy.IsSupportedRuleLine("rendererPath=Scene/Object\trendererType=SpriteRenderer\tscene=Scene"), "rendererPath rule with tabs is supported");
            Check(CapturePresetPolicy.IsSupportedRuleLine("projectileRendererPath=Scene/Object\trendererType=ParticleSystemRenderer"), "projectileRendererPath rule is supported");
            Check(CapturePresetPolicy.IsSupportedRuleLine("sprite=Hero_Sprite"), "sprite rule is supported");
            Check(CapturePresetPolicy.IsSupportedRuleLine("texture=Hero_Texture"), "texture rule is supported");
            Check(CapturePresetPolicy.IsSupportedRuleLine("material=Hero_Material"), "material rule is supported");
            Check(CapturePresetPolicy.IsSupportedRuleLine("asset=Hero_Asset"), "legacy asset rule is supported");
            Check(!CapturePresetPolicy.IsSupportedRuleLine(""), "Empty rule line is rejected");
            Check(!CapturePresetPolicy.IsSupportedRuleLine("path="), "Rule with an empty value is rejected");
            Check(!CapturePresetPolicy.IsSupportedRuleLine("path"), "Rule without a separator is rejected");
            Check(!CapturePresetPolicy.IsSupportedRuleLine("unknown=Value"), "Unknown rule key is rejected");
            Check(!CapturePresetPolicy.IsSupportedRuleLine("path=Scene/Object\npath=Other"), "Rule with a newline is rejected");
            Check(!CapturePresetPolicy.IsSupportedRuleLine("=Scene/Object"), "Rule with an empty key is rejected");

            CapturePresetProfile profile = new CapturePresetProfile();
            CapturePreset first = new CapturePreset();
            first.Name = "  Boss room  ";
            first.RuleLines.Add("path=Scene/Boss");
            first.RuleLines.Add("unknown=Ignored");
            bool replaced = profile.Upsert(first);
            Check(!replaced, "First insert is not a replacement");
            Check(profile.Presets.Count == 1, "First preset is stored");
            CapturePreset stored = profile.Find("boss ROOM");
            Check(stored != null && stored.Name == "Boss room", "Preset lookup is case-insensitive and trimmed");
            Check(stored.RuleLines.Count == 1 && stored.RuleLines[0] == "path=Scene/Boss", "Unsupported rule lines are not stored");

            CapturePreset replacement = new CapturePreset();
            replacement.Name = "BOSS ROOM";
            replacement.IsolationEnabled = false;
            replacement.RuleLines.Add("sprite=Hero");
            Check(profile.Upsert(replacement), "Duplicate preset is replaced");
            Check(profile.Presets.Count == 1, "Replacement does not add another preset");
            stored = profile.Find("Boss room");
            Check(stored != null && !stored.IsolationEnabled && stored.RuleLines.Count == 1 && stored.RuleLines[0] == "sprite=Hero", "Replacement updates isolation and rules");

            CapturePreset unnamed = new CapturePreset();
            unnamed.Name = "   ";
            Check(!profile.Upsert(unnamed), "Unnamed preset is rejected");
            Check(profile.Presets.Count == 1, "Unnamed preset is not stored");

            CapturePreset copied = new CapturePreset();
            copied.Name = "Boss room";
            copied.RuleLines.Add("path=Scene/Boss");
            replacement.RuleLines.Add("path=Scene/Late");
            Check(profile.Upsert(copied), "Second duplicate replacement succeeds");
            stored = profile.Find("Boss room");
            Check(stored.RuleLines.Count == 1 && stored.RuleLines[0] == "path=Scene/Boss", "Profile copies rule lines instead of aliasing the caller");
            CapturePreset storedCopy = stored.Clone();
            storedCopy.RuleLines.Add("path=Scene/CloneOnly");
            storedCopy.IsolationEnabled = false;
            Check(stored.RuleLines.Count == 1 && stored.IsolationEnabled, "Clone is independent from the original preset");
            Check(storedCopy.Name == "Boss room" && storedCopy.RuleLines.Count == 2, "Clone keeps the preset name and rules");

            Check(profile.Remove("BOSS ROOM"), "Preset removal is case-insensitive");
            Check(!profile.Remove("Boss room"), "Removing a missing preset returns false");
            Check(profile.Presets.Count == 0, "Profile is empty after removal");

            CapturePresetProfile limitProfile = new CapturePresetProfile();
            for (int i = 0; i < CapturePresetProfile.MaxPresets; i++)
            {
                CapturePreset preset = new CapturePreset();
                preset.Name = "Preset " + i;
                limitProfile.Upsert(preset);
            }

            Check(limitProfile.Presets.Count == CapturePresetProfile.MaxPresets, "Profile accepts the maximum number of presets");
            CapturePreset overflow = new CapturePreset();
            overflow.Name = "Overflow";
            limitProfile.Upsert(overflow);
            Check(limitProfile.Find("Overflow") == null, "Profile rejects presets above the limit");

            CapturePreset manyRules = new CapturePreset();
            manyRules.Name = "Many rules";
            for (int i = 0; i < CapturePresetProfile.MaxRulesPerPreset + 10; i++)
            {
                manyRules.RuleLines.Add("path=Scene/Object" + i);
            }

            CapturePresetProfile ruleProfile = new CapturePresetProfile();
            ruleProfile.Upsert(manyRules);
            Check(ruleProfile.Find("Many rules").RuleLines.Count == CapturePresetProfile.MaxRulesPerPreset, "Preset rules are truncated at the limit");

            CapturePresetProfile writtenProfile = new CapturePresetProfile();
            CapturePreset writtenFirst = new CapturePreset();
            writtenFirst.Name = "Room A";
            writtenFirst.RuleLines.Add("rendererPath=Scene/A\t" + "rendererType=SpriteRenderer\tscene=Scene");
            writtenFirst.RuleLines.Add("path=Scene/A");
            writtenFirst.RuleLines.Add("groupPath=Scene/A/Group");
            writtenFirst.RuleLines.Add("projectilePath=Scene/A/Projectile");
            writtenFirst.RuleLines.Add("sprite=Hero");
            writtenFirst.RuleLines.Add("texture=HeroTexture");
            writtenFirst.RuleLines.Add("material=HeroMaterial");
            writtenFirst.RuleLines.Add("asset=HeroAsset");
            writtenProfile.Upsert(writtenFirst);
            CapturePreset writtenSecond = new CapturePreset();
            writtenSecond.Name = "Room B";
            writtenSecond.IsolationEnabled = false;
            writtenSecond.RuleLines.Add("path=Scene/B");
            writtenProfile.Upsert(writtenSecond);
            string text = CapturePresetCodec.Write(writtenProfile);
            Check(text.Contains("formatVersion=1"), "Written profile declares format version 1");
            Check(text.Contains("preset=Room A") && text.Contains("preset=Room B"), "Written profile contains both preset names");
            Check(text.Contains("isolation=Off"), "Written profile stores the isolation flag");
            CapturePresetProfile parsed = CapturePresetCodec.Parse(text);
            Check(parsed != null && parsed.Presets.Count == 2, "Written profile parses back");
            CapturePreset parsedFirst = parsed.Find("room a");
            Check(parsedFirst != null && parsedFirst.RuleLines.Count == 8, "Written rules parse back");
            Check(parsedFirst.RuleLines[0] == "rendererPath=Scene/A\trendererType=SpriteRenderer\tscene=Scene", "Renderer rule tabs survive a round-trip");
            Check(parsed.Find("Room B").IsolationEnabled == false, "Isolation flag survives a round-trip");
            Check(CapturePresetCodec.Parse("").Presets.Count == 0, "Empty text parses to an empty profile");
            Check(CapturePresetCodec.Parse(null).Presets.Count == 0, "Null text parses to an empty profile");

            CapturePresetProfile tolerant = CapturePresetCodec.Parse(
                "# comment\r\n" +
                "// another comment\r\n" +
                "\r\n" +
                "FORMATVERSION=1\r\n" +
                "PRESET=  Tolerant  \r\n" +
                "ISOLATION=off\r\n" +
                "RULE=path=Scene/A\r\n" +
                "unknown=value\r\n" +
                "rule=unknown=value\r\n" +
                "preset=\r\n" +
                "preset=Second\r\n" +
                "isolation=yes\r\n" +
                "rule=path=Scene/B\r\n");
            Check(tolerant.Presets.Count == 2, "Parser skips empty preset names and keeps valid blocks");
            Check(tolerant.Find("Tolerant") != null && !tolerant.Find("Tolerant").IsolationEnabled, "Parser accepts lowercase isolation values");
            Check(tolerant.Find("Tolerant").RuleLines.Count == 1, "Parser ignores unknown and unsupported rules");
            Check(tolerant.Find("Second") != null && tolerant.Find("Second").IsolationEnabled, "Parser keeps default isolation on unknown toggle values");

            CapturePresetProfile duplicateText = CapturePresetCodec.Parse("formatVersion=1\npreset=Same\nrule=path=A\npreset=Same\nrule=path=B\n");
            Check(duplicateText.Presets.Count == 1, "Duplicate preset blocks collapse into one");
            Check(duplicateText.Find("Same").RuleLines.Count == 1 && duplicateText.Find("Same").RuleLines[0] == "path=B", "Last duplicate preset block wins");

            Check(CapturePresetCodec.Parse("formatVersion=2\npreset=Future\n") == null, "Newer format version is rejected");
            Check(CapturePresetCodec.Parse("formatVersion=0\npreset=Past\n") == null, "Older format version is rejected");
            Check(CapturePresetCodec.Parse("formatVersion=abc\npreset=Broken\n") == null, "Malformed format version is rejected");
            Check(CapturePresetCodec.Parse("preset=No version\nrule=path=A\n") != null, "Missing format version is tolerated");

            CapturePresetProfile noRules = CapturePresetCodec.Parse("formatVersion=1\npreset=Empty\nisolation=Off\n");
            Check(noRules.Presets.Count == 1 && noRules.Find("Empty").RuleLines.Count == 0, "Preset with no rules parses back");
            CapturePresetProfile equalName = CapturePresetCodec.Parse("formatVersion=1\npreset=A=B\nrule=path=X\n");
            Check(equalName.Find("A=B") != null, "Preset names may contain equals signs");

            StringBuilder manyPresets = new StringBuilder("formatVersion=1\n");
            for (int i = 0; i < CapturePresetProfile.MaxPresets + 5; i++)
            {
                manyPresets.Append("preset=P").Append(i).Append("\nrule=path=Scene/").Append(i).Append('\n');
            }

            Check(CapturePresetCodec.Parse(manyPresets.ToString()).Presets.Count == CapturePresetProfile.MaxPresets, "Parsed profile enforces the preset limit");

            StringBuilder manyRuleLines = new StringBuilder("formatVersion=1\npreset=Many\n");
            for (int i = 0; i < CapturePresetProfile.MaxRulesPerPreset + 5; i++)
            {
                manyRuleLines.Append("rule=path=Scene/").Append(i).Append('\n');
            }

            Check(CapturePresetCodec.Parse(manyRuleLines.ToString()).Find("Many").RuleLines.Count == CapturePresetProfile.MaxRulesPerPreset, "Parsed profile enforces the rule limit");

            PreserveRunStats suspendedStats = new PreserveRunStats();
            suspendedStats.ScanCompleted = true;
            suspendedStats.CandidateMatched = 4;
            PreserveScanStatus suspended = PreserveLimitPolicy.Evaluate(true, 3, true, suspendedStats);
            Check(suspended.State == PreserveScanState.Suspended, "Preserve status reports suspended capture rules");
            Check(PreserveLimitPolicy.Evaluate(true, 0, true, suspendedStats).State == PreserveScanState.NoRules, "No rules wins over suspension");
            Check(PreserveLimitPolicy.Evaluate(false, 3, true, suspendedStats).State == PreserveScanState.IsolationOff, "Isolation off wins over suspension");
            Check(PreserveLimitPolicy.Evaluate(true, 3, false, suspendedStats).State == PreserveScanState.Healthy, "Existing overload keeps rules active");

            Check(UiLocalization.Text("Capture mode", "Russian") == "Режим захвата", "Capture mode label is localized");
            Check(UiLocalization.Text("Selected objects", "Russian") == "Выбранные объекты", "Selected objects mode is localized");
            Check(UiLocalization.Text("Hero + attacks", "Russian") == "Персонаж и атаки", "Hero + attacks mode is localized");
            Check(UiLocalization.Text("Hero only", "Russian") == "Только персонаж", "Hero only mode is localized");
            Check(UiLocalization.Text("Full scene", "Russian") == "Вся сцена", "Full scene mode is localized");
            Check(UiLocalization.Text("Saved capture presets", "Russian") == "Сохранённые пресеты захвата", "Saved presets label is localized");
            Check(UiLocalization.Text("Save preset", "Russian") == "Сохранить пресет", "Save preset button is localized");
            Check(UiLocalization.Text("Apply preset", "Russian") == "Применить пресет", "Apply preset button is localized");
            Check(UiLocalization.Text("Delete preset", "Russian") == "Удалить пресет", "Delete preset button is localized");
            Check(UiLocalization.Text("Capture mode: {mode}. Gameplay isolation: {isolation}.", "Russian").Contains("Режим захвата: {mode}"), "Capture mode status template is localized");
            Check(UiLocalization.Text("Preset: {name} ({rules} rules, isolation {isolation}).", "Russian").Contains("правил: {rules}"), "Preset summary template is localized");
            Check(UiLocalization.Text("Preset applied: {name} ({rules} rules, isolation {isolation}). Preserve rules were backed up: {backup}.", "Russian").StartsWith("Пресет применён:"), "Preset applied message is localized");
            Check(UiLocalization.Text("Preserve rules are suspended by the current capture mode. They stay saved; switch to Selected objects to use them.", "Russian").StartsWith("Preserve-правила приостановлены"), "Suspended preserve status is localized");
            Check(UiLocalization.Text("Switch Capture mode to Selected objects to use your preserve rules again.", "Russian").StartsWith("Переключите режим захвата"), "Suspended preserve hint is localized");
        }
    }
}
