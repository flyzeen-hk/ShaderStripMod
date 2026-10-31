# Changelog

All notable changes to the stable line of ShaderStripMod. The project follows semantic versioning in the `1.29.x` series. History before 1.29.28 was not maintained as a public changelog.

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
