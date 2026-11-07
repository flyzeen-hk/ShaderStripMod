# Changelog

All notable changes to the stable line of ShaderStripMod. The project follows semantic versioning in the `1.29.x`-`1.31.x` series. History before 1.29.28 was not maintained as a public changelog.

## [1.31.1] - 2026-11-02

### Fixed

- Capture preset Apply is transactional. The preserve-file backup is created before any rule changes; the previous rule lines and capture state are snapshotted; if `ShaderStripMod.preserve.txt` cannot be written, the live collections, isolation flag, mode, hero layer and visible mask are restored and the UI reports that no changes were made. Replace and Delete back up the presets file before changing it and restore the previous preset on a failed save.
- Preserve manager actions (remove rule, clean ignored, compact) back up the preserve file before mutation. A failed backup aborts without changes; a failed save rolls the live rule collections back. `SavePreserveList` now returns success/failure and records the error.
- `ShaderStripMod.composition.txt` checks `formatVersion` strictly. Versions 1 and 2 load; unknown, zero, empty or non-numeric versions are refused and the file is marked protected so it is never overwritten by autosave. Reset can replace a protected file only after a successful `.bak_before_reset_*` backup.
- Composition autosave no longer clears the dirty flag on a failed write. It retries after 5 seconds and shows an explicit status: protected, read failure, unsaved changes, save failed or saved. A manual save that succeeds now reports "Saved.".
- Narrow F9 panes use adaptive layouts. Capture-mode buttons switch from 2 columns to 1, the preset name/save row stacks below 240 px, Apply/Delete stacks below 200 px, and composition layout, placement, panel import/clear and per-file assignment controls wrap instead of overflowing at the minimum browser width.
- Media warnings: the collage reports three or more animated tiles, two animated tiles, or a selected video larger than 256 MiB. Video messages distinguish a missing audio track, and both the title-menu and collage settings show "This video has no audio track." when sound is requested. File sizes are cached to avoid per-OnGUI disk access.
- The Panels tab now states that tiles are fixed parts of one full-screen collage, not movable windows.

### Changed

- `ClearAllPreserveRuleCollections` moved from `CapturePresets.cs` to `PreserveRules.cs`, next to the snapshot/restore helpers, so the persistence test covers the rollback path.
- English/Russian localization covers every new success, protection, rollback, autosave, media-advisory and no-audio string.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.31.1`, Assembly/File version `1.31.1.0`.
- Eight test projects: 159,612 assertions (RegressionChecks 4,570, PersistenceChecks 24, MenuMediaChecks 146,058). New checks cover formatVersion 1/2/3/0/non-numeric/empty, defaults for empty input, preserve snapshot restore, failed-save error reporting and the missing-audio-track state.
- Installed DLL matches the build by SHA-256 `da741bd04dadc322a9d4857b50279b86b9799b5967113412bdbcc9f7df205183`; PDB `60ac225e60a90a2162f55dffe938056e4516b70fc4a7a4476610d57bb91987d4`. The pre-install 1.31.0 DLL/PDB were kept as `ShaderStripMod.dll.bak_before_v1.31.1_20261102_002459` and `ShaderStripMod.pdb.bak_before_v1.31.1_20261102_002459`. Pre-change source and installed backups are in `backups-history/backup_20261102_000946_v1.31.0_pre_1.31.1_audit_fixes/` and `backups-history/backup_20261102_000946_v1.31.0_installed_pre_1.31.1/`.
- Live gameplay was **not** run. Transactional rollback, protected presets, autosave retry, narrow-pane layout and no-audio detection are unverified in a real room. 1.31.1 is a local testing candidate and is not published.

## [1.31.0] - 2026-11-01

### Added

- Capture modes in F9 → Settings: **Selected objects**, **Hero + attacks**, **Hero only** and **Full scene**. Selected objects uses the preserve rules; Hero + attacks keeps the hero and attack effects while the preserve rules are temporarily suspended; Hero only also hides attack effects; Full scene shows the original room and disables gameplay isolation. Changing the mode never edits or deletes the saved preserve rules. Switching back to Selected objects re-applies them and rebuilds the hero mask.
- Saved capture presets in the same panel. A preset stores a snapshot of the current preserve rules and the isolation flag in `ShaderStripMod.capture-presets.txt` (`formatVersion=1`, up to 64 presets and 4096 rules per preset). Type a name and press Save preset; each stored preset can be applied or deleted. Replacing or deleting a preset backs up the presets file first. Applying a preset backs up `ShaderStripMod.preserve.txt`, replaces the rule collections and restores the saved isolation state. Masks, imported images, media and collage settings are not included.
- `Source/CapturePresetProfile.cs`: a Unity-free model, policy and codec for modes, presets and the preset file. `Source/CapturePresets.cs`: loading, atomic saving, backups, save/apply/delete actions and the Settings panel. `Tests/CapturePresetChecks.cs`: mode round-trips, name normalization, rule whitelist, profile limits, codec tolerance, version rejection and localization checks.
- Preserve diagnostics now know that rules can be suspended by the capture mode. The preserve scan state reports "suspended" instead of "no rules" or "no matches", and Settings, Performance and Rules explain that the rules are still saved and return with Selected objects.

### Changed

- `Source/PreserveRules.cs`: the preserve-list writer now shares one rule-line builder with preset snapshots, so a preset contains exactly the rules that `ShaderStripMod.preserve.txt` would contain (without the group separator lines).
- `Source/Rendering.cs`, `Source/PreservedRendering.cs`, `Source/ShaderStripMod.cs`: Hero only hides the Attack layer and skips effect promotion; the hero mask is rebuilt on every capture-mode change; enabling isolation selects Selected objects and disabling it selects Full scene.
- New English/Russian strings for the four mode names, the status line, per-mode hints, preset actions, confirmations, errors and the suspended preserve state.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.31.0`, Assembly/File version `1.31.0.0`.
- Eight test projects: 159,598 assertions (RegressionChecks grew to 4,561).
- Installed DLL matches the build by SHA-256 `f8b75cfcb86c19e1301f0a934b8fa79d2e549a19624cad62609265d2154adb4a`; PDB `612d9dfd97905bfdbd6d36f6e1672dcaad394f2ef54a714ecc88fc3fcc861fc6`. `ShaderStripMod.dll.bak_before_v1.31.0_20261101_184805` is the pre-install copy of the installed 1.30.6; the final rebuild replaced an intermediate 1.31.0 build and kept it as `ShaderStripMod.dll.bak_before_v1.31.0_final_20261101_185446`. Pre-change backups are in `backups-history/backup_20261101_183313_v1.30.6_pre_1.31.0_capture_presets/` and `backups-history/backup_20261101_183334_v1.30.6_installed_pre_1.31.0_capture_presets/`.
- Live gameplay was **not** run. Capture-mode switching, attack hiding, preserve suspension and preset apply/delete are unverified in a real room; 1.31.0 is not published.

## [1.30.6] - 2026-11-01

### Added

- A read-only "Capture status" block at the top of F9 → Settings. It shows gameplay isolation and title menu design in one line, the camera state (following, pinned in this room, or the last camera message) and the preserve scan state: paused, no rules, scan pending, no matches, limit reached or healthy. When the draw or particle limit hides matching objects, a hint explains what to change.
- `Source/PreserveLimitPolicy.cs`: a pure evaluator (`PreserveScanState`, `PreserveScanStatus`, `PreserveLimitPolicy.Evaluate`) that turns isolation state, rule count and `PreserveRunStats` into one of six states. `PreserveRunStats.ScanCompleted` records whether a full rebuild has finished, so "scan pending" is accurate even when no object matches.
- The Performance tab now opens with a one-line status ("Paused", "No preserve rules", "Scan pending", "No matches in this room", "Limit reached: draw N, particle M hidden" or "Matched X, drawn Y, blocked Z; no limit") above the existing counters.

### Changed

- The preserve runtime summary in F9 → Rules uses the same state instead of only listing counters. "Last full scan" appears only after a completed rebuild, and the draw-limit note now says "(at draw limit)" instead of the misleading "(draw limit reached)".
- The camera line in Settings is generated by one helper, so the pinned hint and the release hotkey stay in sync with the actual camera state.
- New English/Russian strings for the status block, six scan states, the short Performance status and the two hints.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.30.6`, Assembly/File version `1.30.6.0`.
- Eight test projects: 159,495 assertions (RegressionChecks grew to 4,458).
- Installed DLL matches the build by SHA-256 `e367547541198fe3b53d1fec5930bd1ea355a0cdc9093ebd0a7877711356232a`; PDB `797bda3e3407d8f94d14806b3665185c21a2f6e7a1bf603433734f91980874cf`. Pre-install backups of the installed 1.30.5 are in `backups-history/backup_20261101_180955_v1.30.5_pre_1.30.6_capture_status/` and `backups-history/backup_20261101_181656_v1.30.5_installed_pre_1.30.6_capture_status/`.
- No gameplay, rendering or file-format behavior changed. Live gameplay was **not** run; 1.30.6 is not published.

## [1.30.5] - 2026-11-01

### Fixed

- The "Enable title menu design" toggle in F9 → Menu design now saves and applies immediately, like the "Title menu design" toggle in Settings. Previously it was only applied when another control on the tab changed in the same frame, so clicking it alone looked like it did nothing.
- The Menu design hint that explains the master switch now sits directly below the two toggles instead of at the bottom of the long tab. A second hint appears when title menu design is on but custom menu is off.
- Trash confirmation texts are now localized in Russian in both Settings (images) and Menu design (menu media): the "Click Empty trash again..." prompt and the "Permanently delete N file(s), size?" question.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.30.5`, Assembly/File version `1.30.5.0`.
- Eight test projects: 159,483 assertions.
- Installed DLL matches the build by SHA-256 `721d06cb478b725895f508edb3db24ef5337ac06ff6a205b3d6eed00e236f374`; PDB `c1962b5633683513941e4fd596f9ed243ef0297e5bb103257c24b31e94743fa2`. Pre-install backup of the installed 1.30.4 is in `backups-history/backup_20261101_175821_v1.30.4_installed_pre_1.30.5_menu_toggle/`.
- No gameplay, rendering or file-format behavior changed. Live gameplay was **not** run; 1.30.5 is not published.

## [1.30.4] - 2026-11-01

### Changed

- Usability pass across the whole F9 browser; no gameplay, rendering or file-format behavior changed.
- Browser header now shows a localized window title, a description of the active mode (Loaded, Entities, Nearby, Atlases, Rules, Projectiles, Menu, Dump) and a persistent footer with Esc and the configured browser, camera and isolation hotkeys.
- The search field has a placeholder ("Search by name, kind or details") and a clear button. An empty detail pane explains that an entry must be selected and names the three entry points: Settings for the green screen and camera, Panels for the media collage, Menu design for the title screen.
- Tabs are grouped into entry tabs (Info, Preview, Rules, Export, Mask) and global tabs (Settings, Panels, Menu design, Performance, Hotkeys), so per-sprite actions are no longer mixed with mod-wide settings.
- Settings tab: status messages moved to the top, sections are labeled (interface language, visuals and camera, screen background, imported images, removed images, preserved objects), and destructive Remove/Delete/Empty actions use a red-tinted button.
- Rules tab and preserve manager: bold section headers plus one-line explanations for renderer, asset and path rules; clear guidance when no rule is selected; rule removal is a danger button.
- Panels tab: "How to use" with four steps, a collage summary line (layout, placement, filled tiles, isolation), per-panel empty states, assignment buttons that show which panel holds a file, and a two-click confirmation for Reset composition.
- Menu design tab: section headers (enable, background and sound, logo and buttons, imported media, trash), a note that this tab affects only the title menu, and danger styling for remove/delete/empty actions. Hotkeys tab: an explanation that Change captures the next key and saves immediately.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.30.4`, Assembly/File version `1.30.4.0`.
- Eight test projects: 159,483 assertions (unchanged; the pass only changed UI text and layout).
- Installed DLL matches the build by SHA-256 `12b877be3e69165872ccfc6d49d5f860a9fe03e06ef14fb413b3ca827a73ebf5`; PDB `9c629c84b3bc6363db193eba3b17e9b83bf1f32c0472aeaa6bafbd13956c2d78`. Pre-install backup of the installed 1.30.3 DLL is in `backups-history/backup_20261101_172319_v1.30.3_installed_pre_1.30.4_ux/`.
- Live gameplay was **not** run for this build; the new layout and texts are unverified in the game. 1.30.4 is not published.

## [1.30.3] - 2026-11-01

### Changed

- The composition prototype is now a **full-screen media collage** instead of separate game/media windows. The gameplay camera still clears to the strip background color, a `BeforeForwardOpaque` command buffer draws PNG/JPG/GIF/video tiles over the whole screen, and the hero plus preserved renderers draw above the media on the normal camera. There is no RenderTexture redirect, no owned output camera and no HUD camera requirement, so chroma-key, preserve, behind-hero and in-front rendering keep working.
- All eight layouts now contain media tiles only: full screen, two halves, two rows, three equal columns, three equal rows, main + side, main + bottom and a 2×2 grid. Adjacent tiles share exact integer edges with a zero gap and cover the entire screen; empty tiles show the background color.
- Tiles resize with the window and use a placement mode: Cover (default, preserves aspect ratio and crops overflow like the menu background), Contain (fits the whole media and leaves the background visible) or Stretch.
- Up to four tiles accept media, each with pause, sound, mute and volume. The legacy full-screen gameplay image background is suppressed while the collage is active and returns unchanged when it is disabled.
- `Source/CompositionPolicy.cs` was rewritten around zero-gap slot geometry, placement normalization and layout/inactive-state policy without HUD modes. `Source/Composition.cs` now owns one command buffer attached to the gameplay world camera, four meshes/materials/surfaces and per-tile UV or vertex geometry. The composition camera re-applies its full-screen rect, solid strip background and gameplay mask before culling without attaching the legacy backdrop buffer. The buffer is rebuilt in `Camera.onPreCull` for its camera, so the viewport and matrices restored after the media draw match the camera position after LateUpdate. `Source/CompositionGui.cs` was rebuilt for the collage: layout and placement buttons, four media-tile blocks and the imported-media assignment list.
- `Source/CompositionProfile.cs` writes `formatVersion=2` with `layout`, `placement` and four `panel.N` blocks. The parser ignores the old `gap` key, keeps the user's existing layout and media assignments, and defaults missing placement to Cover. `compositionHudMode` is no longer read or written in `ShaderStripMod.settings.txt`.
- New English/Russian UI strings for the collage, placement modes, empty-tile behavior and the four-tile list.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.30.3`, Assembly/File version `1.30.3.0`.
- Eight test projects: 159,483 assertions. RegressionChecks grew to 4,446 with zero-gap coverage checks for every layout and resolution, placement/profile format-v2 round-trips, legacy-preset migration and inactive-state policy checks.
- Installed DLL matches the build by SHA-256 `459d42f314c95433e21eb1e171664c0a859dcf24b525110a9ff141898d16930f`; PDB `e00ff828e552c97a4dd0832ef5ade5343da5a6917631ad56f51b445dee00e600`.
- Live gameplay was **not** run for this build. Camera-event drawing, tile alignment in rooms and transitions are unverified; 1.30.3 is not published.

## [1.30.2] - 2026-11-01

### Added

- Hotkeys tab (tenth tab in F9) for the five mod actions: open/close the browser, pin/release the camera, dump loaded assets, reload preserve rules and toggle gameplay isolation. Each action has Change, Clear key and Reset default; Reset all defaults restores the whole profile. Changing a key captures the next supported key press; Esc cancels the capture without closing the browser.
- `Source/HotkeyProfile.cs`: a pure hotkey profile with stable action ids, default keys F9/F8/F10/F11/F12, an allowed key list (F1-F12, A-Z, Alpha0-Alpha9, Keypad0-Keypad9, Insert, Home, End, PageUp, PageDown, Delete, Backspace, Space and keypad operators), reserved Esc/Enter/Tab/arrow keys, duplicate validation and deterministic conflict repair. `HotkeyProfileCodec` writes `formatVersion=1` to `ShaderStripMod.hotkeys.txt` and parses comments, unknown actions, empty values and invalid keys safely.
- `Source/Hotkeys.cs` loads and atomically saves the profile, parses stored key names into Unity `KeyCode`, drives the capture loop and generates dynamic shortcut labels. `Source/HotkeysGui.cs` renders the Hotkeys tab.
- `Tests/HotkeyChecks.cs` covers defaults, key policy, codec round-trips, conflict repair, validation, clone isolation and localization.

### Changed

- `F8`-`F12` are no longer hardcoded. `RuntimeLoop` resolves `ModHotkeyPolicy` from the saved profile, and a key capture consumes input for its frame. The browser still ignores camera/dump/reload/isolation actions while it is open.
- Settings, Panels, Menu design, composition notes and preserve diagnostics now show the configured key instead of a fixed one. Generated file headers refer to F9 → Hotkeys rather than naming F9/F10/F11 directly.
- The F9 tab bar has ten tabs; narrow windows use a 3×4 layout and the normal layout uses 5×2. The Hotkeys tab is included in both.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.30.2`, Assembly/File version `1.30.2.0`.
- Eight test projects: 159,072 assertions. RegressionChecks grew to 4,035 with the hotkey profile, key policy, codec, conflict and localization checks.
- Installed DLL matches the build by SHA-256 `9769624558956b68ccbf2a66418e3fe50af8acc4cec117ad4990eba7d2b488ad`; PDB `dcd9101a54a5cf0999ad4ad5de0a12c58598565679565727126040a14b7659ca`.
- Live gameplay was **not** run for this build. Key capture, rebinding and persisted hotkeys after restart are unverified; 1.30.2 is not published.

## [1.30.1] - 2026-11-01

### Added

- Eight preset composition layouts: full screen, two vertical, two horizontal, three columns, three rows, main + side, main + bottom and a 2×2 grid, with a 0-32 px gap between panels. Every layout keeps one panel for the live game and allows up to three media or video panels. Narrow windows fall back to a single panel instead of rendering an invalid layout.
- Panel media can be a PNG, JPG, GIF or video. Each media panel has its own pause, sound, mute and volume controls; changing the audio route recreates the panel `MediaSurface`, while volume and mute apply live. `CompositionPolicy` now exposes layout display names, per-panel names (left/centre/right column, rows, main area, grid cells) and the live game panel index for the Panels tab.
- `Source/CompositionProfile.cs`: a `formatVersion=1` preset file, `ShaderStripMod.composition.txt`, with an invariant-culture writer, a tolerant parser for comments/unknown keys, layout normalization, gap and volume clamping, media-name safety checks and atomic saves. The preset auto-saves 0.5 s after a change and is flushed on mod unload; the Panels tab also has Save preset now, Reload preset and Reset composition.
- One-time migration of the 1.30.0 `compositionLayout`, `compositionLeftFile`, `compositionRightFile`, `compositionLeftPaused` and `compositionRightPaused` settings into the preset file. The settings file keeps only `compositionEnabled` and `compositionHudMode`; old keys are no longer written.
- The Panels tab is rebuilt around presets: layout buttons, gap slider, live game panel label, per-panel blocks with a file picker, clear/import actions and audio controls, plus an imported-media list with Panel 1-3 assignment buttons.

### Changed

- Composition import now accepts supported video extensions in addition to images and GIF; clearing a panel from the media list uses the same path as the in-panel Clear button. Re-selecting the file already loaded by a healthy panel is a no-op.
- Composition profile I/O falls back to `Application.persistentDataPath` when the settings directory is empty, and the profile is marked dirty after load so the runtime rebuilds its panels.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.30.1`, Assembly/File version `1.30.1.0`.
- Eight test projects: 158,682 assertions. RegressionChecks grew to 3,645 with the layout matrix, profile codec, migration and audio-policy checks.
- Installed DLL matches the build by SHA-256 `df5ba29c5229a214601310de9daf5fab24c8dcb291ee65f4099ee522f8bb85fe`; PDB `d5e16b06b7afea252f0de7834239a699798434eb0836f61c7e7e128331011ff9`.
- Live gameplay was **not** run for this build. Render-texture orientation, multi-panel input/audio and window resize are unverified; 1.30.1 is not published.

## [1.30.0] - 2026-11-01

### Added

- Side-panel composition prototype in F9 → Panels. In a gameplay room with gameplay isolation enabled, the world and HUD cameras render into one centre-panel render texture and an owned output camera draws three screen columns from a command buffer. The centre column shows the live game; the left and right columns accept PNG, JPG or GIF images imported through the existing menu-media folder, with per-panel pause and clear controls, cover-fit cropping and a reset action.
- `Source/CompositionPolicy.cs` with pure three-column geometry (no overlap, minimum panel size, 4096-pixel render-texture cap), HUD-mode support policy, media extension checks and an ordered inactive-reason state machine. `Tests/CompositionPolicyChecks.cs` covers the layout matrix, render-texture capping and reason ordering.
- Composition settings (`compositionEnabled`, `compositionLayout`, `compositionHudMode`, left/right files and pause flags) are saved in `ShaderStripMod.settings.txt`.
- Nine-tab F9 layout with the new Panels tab.

### Changed

- Gameplay camera routing, menu-camera routing, background-frame enforcement and the background image path skip the composition output camera and use the redirected world camera while composition is active. The HUD camera keeps its original mask inside the centre panel.
- Composition releases its render texture, cameras, command buffer, materials, meshes and media surfaces on F12, scene change, mod unload, disabled toggle, unsupported state or a rendering exception. An exception disables composition and is reported in F9 and the log.
- The HUD `Hidden` and `OverlayFullScreen` modes remain in the settings format but are rejected at runtime (`HudModeUnsupported`) until they receive their own live testing.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.30.0`.
- Eight test projects: 157,887 assertions. RegressionChecks now includes composition layout and policy checks.
- Live gameplay was **not** run for this build. Render-texture orientation, HUD rendering inside the centre panel and hardware-GPU behaviour are unverified; 1.30.0 is not published.

## [1.29.32] - 2026-11-01

### Added

- Independent toggles for gameplay isolation and title menu design. `F12` now toggles gameplay isolation only; the title menu design switch is in F9 → Settings and in the Menu design tab. Turning gameplay isolation off no longer hides the custom title menu, and turning menu design off leaves gameplay isolation untouched. Both flags are saved in the settings file.
- Opt-in menu video sound in F9 → Menu design: enable, mute, per-video volume and a master menu-media volume. Effective volume is `customMenuVolume * menuMediaMasterVolume`; mute overrides both. Changing the audio route recreates the `VideoPlayer`; volume and mute apply live. Sound remains off by default.
- `Source/MediaAudioPolicy.cs` with pure volume clamping and effective-volume policy, and `Source/MediaSurface.cs` as an ownership wrapper for one media player. `MediaSurface` is the foundation for the multi-panel composition work in 1.30.x.

### Changed

- Menu video-ready messages now distinguish silent, muted and sound-on states. English and Russian localization, F9 controls and documentation were updated.
- `ModHotkeyAction.ToggleVisuals` was renamed to `ToggleGameplayIsolation`; `Toggle()` was removed in favour of `ToggleGameplayVisuals()`.
- Preserve diagnostics report gameplay isolation and title menu design as separate states.

### Verified

- Release build: 0 errors, 0 warnings. Product version `1.29.32`, installed DLL matches the build.
- Eight test projects: 157,611 assertions, including new checks for audio routing, volume/mute policy, `MediaSurface` lifecycle, localization and independent camera toggles.
- Live gameplay was not run for this build in this session. In-game verification is pending and 1.29.32 is not published.

## [1.29.31] - 2026-11-01

### Fixed

- `GetVersion()` and the initialization log no longer report `1.29.29`. The public version is derived from the assembly version, so the in-game mod list and logs match the release.
- F8-F12 and Esc now pass through one hotkey policy. While the F9 browser is open, F10-F12 no longer fire from text fields or other browser controls. F9 closes the browser and Esc still cancels or closes it; F8 remains ignored while F9 is open.

### Verified

- Release build: 0 errors, 0 warnings.
- Eight test projects: 157,592 assertions. The input-routing policy is covered by 12 pure checks; RegressionChecks now has 2,573 assertions.
- Product version is `1.29.31` without a source-revision suffix.
- Live gameplay was not run for this build in this session. In-game F8-F12/F9 checks are pending.

## [1.29.30] - 2026-10-31

### Added

- Trash management for removed background images (F9 → Settings) and menu media (F9 → Menu design). Each list shows file names and sizes, Restore never overwrites an existing file, Delete permanently requires a second confirmation for that file, and Empty trash requires a second confirmation before erasing the listed files. The total size of each trash folder is shown.
- Preserve diagnostics. F9 → Performance now reports how many rule candidates matched, how many renderers were actually drawn, how many were blocked by the draw or particle limits, and a breakdown of skip reasons. F9 → Rules shows a live report for the selected rule: matched, in-scene, drawable and cached counts, the chosen renderer type, path, layer and active rules, and the reason a renderer is not drawn.

### Fixed

- `F8` is ignored while the F9 browser is open. The camera can still be pinned or released from F9 → Settings.
- `F12` disables the visual mode but keeps the camera pin. The pin is still released on room change, death, transitions, cutscenes and mod unload.
- `ApplyStrip` now distinguishes three states: title menu, gameplay with a hero, and gameplay or a transition without a hero. In the third state the menu background and menu camera mask are no longer applied to gameplay cameras; the previous menu state is restored and the mod waits for `HeroController.Start`.
- Preserve diagnostics explain that rules match renderer identity, object path, scene and asset names, not semantic object types. Renderer and particle limits are visible instead of silently dropping objects.
- Preserve diagnostics are invalidated after a scene change, a preserve-rule change or an `F12` toggle. Runtime counters are cleared when visuals are disabled, and the selected-rule report states that its counts are a dry scan until visuals are enabled again.

### Verified

- Release build: 0 errors, 0 warnings.
- Eight test projects: 157,583 assertions, mostly pixel comparisons for GIF and preview code.
- Camera and transition behavior was checked with Unity/game API substitutes. The new build has **not** yet been tested in a live gameplay session; in-game verification is pending.

## [1.29.29] - 2026-10-11

### Added

- Gameplay camera pin: `F8`, or F9 → Settings → camera. The hero keeps moving while the camera holds its world position. The pin is released on room change, death, transitions, cutscenes, `F12` and unload. It is not saved between sessions and is unavailable in the title menu.

### Fixed

- Game text no longer shows through the F9 browser.
- The active tab and selected modes are marked with color and a check mark.
- Rules buttons wrap by panel width without overlapping; actions and information share one scrollable area.
- Camera and pause settings are placed before the long image list.
- Long menu toggles wrap instead of being clipped.
- Loading messages are visible at the top of the menu-design tab.
- Extra empty scroll space in Settings was removed.
- Gameplay PNG/JPG files are validated (up to 64 MiB, 8192 px per side, 8,388,608 pixels total) before import and Unity decoding. A rejected image is not activated.

### Verified

- Release build: 0 errors, 0 warnings.
- Eight test projects: 157,546 assertions, mostly pixel comparisons for GIF and preview code.
- Live run: 42 PASS, 0 FAIL, 14 UI frames at 1360×768 and 800×600. Covered the title menu, PNG, GIF, MP4 H.264, F9 pause, input, settings and Rules.
- The live run did not open a save or a gameplay room. Camera tracking with a moving hero was checked with Unity/game API substitutions, and the run used software DirectX WARP.

## [1.29.28] - 2026-10-10

### Fixed

- Preview clipping bounds and mouse-capture release.
- List-row counter overlay.
- Menu input blocking through a saved `EventSystem`.
- Cumulative text darkening during transitions.
- Menu decoration restore and media release.
- Videos receive a normal absolute Windows path, including Cyrillic paths.
- Settings and preserve lists are saved through `AtomicTextFile`, keeping the previous copy.
- Camera reassignment excludes render-to-texture cameras and selects one gameplay camera for the background and preserved objects.
