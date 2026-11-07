# ShaderStripMod 1.31.1 — testing pre-release

**Testing pre-release / Тестовая сборка.** Full live gameplay verification is still pending. The first launch completed without serious errors, but capture modes, presets, the narrow F9 layout, the media collage, camera transitions, audio and autosave remain unverified in a complete gameplay session.

ShaderStripMod 1.31.1 is a chroma-key and background replacement mod for Hollow Knight. This build adds capture modes, saved capture presets, a full-screen media collage and configurable hotkeys, with stronger persistence safeguards.

### Changes since 1.29.30

- Gameplay isolation and title-menu design are independent toggles; menu media supports images, GIF and MP4 H.264.
- The Panels tab draws a full-screen, zero-gap media collage with eight layouts, Cover/Contain/Stretch placement and per-panel pause, mute and volume. The hero and preserved objects render above the collage.
- Hotkeys are rebindable in F9 → Hotkeys; the browser input gate prevents camera and isolation keys from firing while F9 is open.
- F9 → Settings offers four capture modes: Selected objects, Hero + attacks, Hero only and Full scene. Hero modes suspend the preserve rules without deleting them.
- Capture presets store a snapshot of the preserve rules and the isolation flag in `ShaderStripMod.capture-presets.txt`. Save, Apply and Delete create backups first; a failed preserve save rolls the live rules back.
- `ShaderStripMod.composition.txt` checks `formatVersion` strictly. Unsupported or malformed versions are refused and the file is protected from automatic overwrite.
- Composition autosave reports its state and retries after a failed write instead of silently losing changes.
- Narrow F9 panes wrap buttons instead of clipping them; media advisories warn about multiple animated tiles, videos over 256 MiB and missing audio tracks.
- The read-only Capture status block explains isolation, camera and preserve-scan state; the Menu design toggle and trash confirmations are fixed and localized.

### Important

- **This is a testing pre-release.** Treat camera pin, transitions, death, cutscenes, capture modes and collage behavior as not fully verified. Report issues before relying on it.
- Windows only. The file picker uses Windows APIs; macOS and Linux are untested and the import picker is unavailable.
- Requires the Hollow Knight Modding API.
- Back up `ShaderStripMod.settings.txt`, `ShaderStripMod.preserve.txt`, `ShaderStripMod.composition.txt`, `ShaderStripMod.hotkeys.txt`, custom images and media before updating.

### Install

1. Install the Modding API.
2. Close the game.
3. Extract `ShaderStripMod.dll` into:
   `hollow_knight_Data/Managed/Mods/ShaderStripMod/`
4. Start the game.

The release ZIP contains `ShaderStripMod.dll`, `README.md`, `CHANGELOG.md` and `LICENSE`.

### Verification

- Release build: 0 warnings, 0 errors.
- Eight test projects: 159,612 assertions.
- Installed DLL matches the build: SHA-256 `da741bd04dadc322a9d4857b50279b86b9799b5967113412bdbcc9f7df205183`.
- Not yet listed in ModLinks; install manually.

SHA-256 of `ShaderStripMod-1.31.1.zip`:

```text
29abe6ccc3694fb70ee302525fdeb7b1fde2c8dda1fe64dbc554aab3a2e8ee67
```

Full changelog: https://github.com/flyzeen-hk/ShaderStripMod/blob/main/CHANGELOG.md
