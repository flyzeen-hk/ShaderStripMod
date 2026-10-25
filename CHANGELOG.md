# Changelog

All notable changes to the stable line of ShaderStripMod. The project follows semantic versioning in the `1.29.x` series. History before 1.29.28 was not maintained as a public changelog.

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
