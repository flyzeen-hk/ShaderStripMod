using System;
using System.IO;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        static int ExpectedPanelCount(string layout)
        {
            switch (layout)
            {
                case CompositionPolicy.LayoutFullScreen: return 1;
                case CompositionPolicy.LayoutTwoVertical:
                case CompositionPolicy.LayoutTwoHorizontal: return 2;
                case CompositionPolicy.LayoutMainAndSide:
                case CompositionPolicy.LayoutMainAndBottom:
                case CompositionPolicy.LayoutThreeColumns:
                case CompositionPolicy.LayoutThreeRows: return 3;
                default: return 4;
            }
        }

        static void CheckLayoutTiles(string name, int screenW, int screenH)
        {
            CompositionLayout layout = CompositionPolicy.CalculateLayout(name, screenW, screenH);
            string label = name + " at " + screenW + "x" + screenH;
            Check(layout.IsValid, label + " is valid");
            Check(layout.LayoutName == name, label + " keeps its layout name");
            Check(layout.ScreenWidth == screenW && layout.ScreenHeight == screenH, label + " keeps the screen size");
            Check(layout.PanelCount == ExpectedPanelCount(name), label + " has the expected media tile count");
            Check(layout.PanelCount <= CompositionPolicy.MaxMediaPanels, label + " respects the media tile cap");
            long covered = 0;
            for (int i = 0; i < layout.PanelCount; i++)
            {
                CompositionRect panel = layout.Panels[i];
                Check(!panel.IsEmpty, label + " tile " + i + " is non-empty");
                Check(panel.Width >= CompositionPolicy.MinimumPanelPixels && panel.Height >= CompositionPolicy.MinimumPanelPixels,
                    label + " tile " + i + " is large enough");
                Check(panel.X >= 0 && panel.Y >= 0 && panel.X + panel.Width <= screenW && panel.Y + panel.Height <= screenH,
                    label + " tile " + i + " stays inside the screen");
                Check(!string.IsNullOrEmpty(CompositionPolicy.GetPanelName(layout, i)), label + " tile " + i + " has a name");
                covered += (long)panel.Width * panel.Height;
                for (int j = i + 1; j < layout.PanelCount; j++)
                {
                    Check(!panel.Overlaps(layout.Panels[j]), label + " tiles " + i + " and " + j + " never overlap");
                }
            }

            Check(covered == (long)screenW * screenH, label + " tiles cover the whole screen with no gap");
        }

        static void RunCompositionPolicyChecks()
        {
            foreach (string name in CompositionPolicy.LayoutNames)
            foreach (int[] size in new[]
            {
                new[] { 640, 480 },
                new[] { 800, 600 },
                new[] { 1280, 720 },
                new[] { 1366, 768 },
                new[] { 1920, 1080 },
                new[] { 2560, 1440 },
                new[] { 3440, 1440 },
                new[] { 3840, 2160 },
                new[] { 1921, 1081 }
            })
            {
                CheckLayoutTiles(name, size[0], size[1]);
            }

            CompositionLayout three = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutThreeColumns, 1920, 1080);
            Check(three.Panels[0].Width == 640 && three.Panels[1].Width == 640 && three.Panels[2].Width == 640,
                "Three equal columns share the width exactly");
            Check(three.Panels[0].X == 0 && three.Panels[1].X == 640 && three.Panels[2].X == 1280,
                "Three columns touch edge to edge with no gap");
            Check(three.Panels[0].Height == 1080 && three.Panels[1].Height == 1080 && three.Panels[2].Height == 1080,
                "Three columns fill the screen height");
            Check(CompositionPolicy.GetPanelName(three, 0) == "Left column" &&
                  CompositionPolicy.GetPanelName(three, 1) == "Center column" &&
                  CompositionPolicy.GetPanelName(three, 2) == "Right column", "Three-column tiles have readable names");

            CompositionLayout twoVertical = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutTwoVertical, 1921, 1081);
            Check(twoVertical.Panels[0].X == 0 && twoVertical.Panels[0].Width == 960 &&
                  twoVertical.Panels[1].X == 960 && twoVertical.Panels[1].Width == 961,
                "Odd screen width splits between two tiles without a gap");
            Check(twoVertical.Panels[0].Height == 1081 && twoVertical.Panels[1].Height == 1081, "Two vertical tiles fill the height");

            CompositionLayout twoHorizontal = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutTwoHorizontal, 1920, 1080);
            Check(twoHorizontal.Panels[0].Y == 540 && twoHorizontal.Panels[0].Height == 540 &&
                  twoHorizontal.Panels[1].Y == 0 && twoHorizontal.Panels[1].Height == 540,
                "Two horizontal tiles stack top over bottom without a gap");
            Check(twoHorizontal.Panels[0].Y == twoHorizontal.Panels[1].Y + twoHorizontal.Panels[1].Height,
                "Horizontal tiles share the middle edge");

            CompositionLayout mainSide = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutMainAndSide, 1920, 1080);
            Check(mainSide.Panels[0].X == 0 && mainSide.Panels[0].Height == 1080, "Main + side puts the main tile on the left");
            Check(mainSide.Panels[0].Width > mainSide.Panels[1].Width, "Main + side makes the first tile wider");
            Check(mainSide.Panels[0].Width == mainSide.Panels[1].X, "Main + side tiles touch without a gap");
            Check(mainSide.Panels[1].Y == mainSide.Panels[2].Y + mainSide.Panels[2].Height &&
                  mainSide.Panels[1].Height == mainSide.Panels[2].Height, "Main + side stacks equal right tiles");

            CompositionLayout mainBottom = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutMainAndBottom, 1920, 1080);
            Check(mainBottom.Panels[0].X == 0 && mainBottom.Panels[0].Width == 1920, "Main + bottom stretches the main tile across the top");
            Check(mainBottom.Panels[0].Y == mainBottom.Panels[1].Height, "Main + bottom tiles touch without a gap");
            Check(mainBottom.Panels[1].X == 0 && mainBottom.Panels[2].X == mainBottom.Panels[1].Width,
                "Main + bottom splits the bottom edge between two tiles");

            CompositionLayout grid = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutGrid2x2, 1920, 1080);
            Check(grid.PanelCount == 4 && grid.Panels[0].X == 0 && grid.Panels[0].Y == 540,
                "2x2 grid starts with the top-left cell");
            Check(grid.Panels[1].X == grid.Panels[0].Width && grid.Panels[2].Y == 0,
                "2x2 cells meet at the centre without a gap");
            Check(CompositionPolicy.GetPanelName(grid, 0) == "Top-left cell" &&
                  CompositionPolicy.GetPanelName(grid, 3) == "Bottom-right cell", "2x2 panel names follow the cell order");

            CompositionLayout fullScreen = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutFullScreen, 1920, 1080);
            Check(fullScreen.IsValid && fullScreen.PanelCount == 1 && fullScreen.Panels[0].Width == 1920 &&
                  fullScreen.Panels[0].Height == 1080, "Full screen layout contains one screen-sized tile");
            Check(CompositionPolicy.GetPanelName(fullScreen, 0) == "Full screen", "Full screen tile is named");

            CompositionLayout tiny = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutThreeColumns, 40, 40);
            Check(!tiny.IsValid && tiny.PanelCount == 0, "Too small screen rejects the layout instead of shrinking tiles");
            CompositionLayout emptyScreen = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutThreeColumns, 0, 0);
            Check(!emptyScreen.IsValid && emptyScreen.PanelCount == 0, "Zero screen size is rejected");
            CompositionLayout smallGrid = CompositionPolicy.CalculateLayout(CompositionPolicy.LayoutGrid2x2, 64, 64);
            Check(smallGrid.IsValid && smallGrid.PanelCount == 4, "A small but usable screen keeps the grid valid");

            Check(!new CompositionRect(0, 0, 10, 10).Overlaps(new CompositionRect(10, 0, 10, 10)), "Edge-adjacent rectangles do not overlap");
            Check(new CompositionRect(0, 0, 10, 10).Overlaps(new CompositionRect(9, 9, 10, 10)), "Corner intersection counts as overlap");

            foreach (string name in CompositionPolicy.LayoutNames)
            {
                Check(CompositionPolicy.NormalizeLayout(name) == name, name + " round-trips through normalization");
                Check(CompositionPolicy.NormalizeLayout(name.ToLowerInvariant()) == name, name + " normalization ignores case");
            }

            Check(CompositionPolicy.NormalizeLayout("anything") == CompositionPolicy.LayoutThreeColumns, "Unknown layout falls back to three columns");
            Check(CompositionPolicy.NormalizeLayout("") == CompositionPolicy.LayoutThreeColumns, "Empty layout falls back to three columns");
            Check(CompositionPolicy.NormalizeLayout(null) == CompositionPolicy.LayoutThreeColumns, "Missing layout falls back to three columns");
            Check(CompositionPolicy.GetLayoutDisplayName(CompositionPolicy.LayoutGrid2x2) == "2x2 grid", "Grid layout has a readable display name");
            Check(CompositionPolicy.GetLayoutDisplayName(CompositionPolicy.LayoutMainAndSide) == "Main + side", "Main layout has a readable display name");

            Check(CompositionPolicy.NormalizePlacement("contain") == CompositionPolicy.PlacementContain, "Placement normalization ignores case");
            Check(CompositionPolicy.NormalizePlacement("STRETCH") == CompositionPolicy.PlacementStretch, "Stretch placement round-trips");
            Check(CompositionPolicy.NormalizePlacement("Cover") == CompositionPolicy.PlacementCover, "Cover placement round-trips");
            Check(CompositionPolicy.NormalizePlacement("garbage") == CompositionPolicy.PlacementCover, "Unknown placement falls back to Cover");
            Check(CompositionPolicy.NormalizePlacement(null) == CompositionPolicy.PlacementCover, "Missing placement falls back to Cover");

            Check(CompositionPolicy.IsSupportedMediaExtension(".png"), "PNG panels are supported");
            Check(CompositionPolicy.IsSupportedMediaExtension(".PNG"), "Capitalized extension is supported");
            Check(CompositionPolicy.IsSupportedMediaExtension(".jpg") && CompositionPolicy.IsSupportedMediaExtension(".jpeg") && CompositionPolicy.IsSupportedMediaExtension(".gif"), "JPG and GIF panels are supported");
            Check(!CompositionPolicy.IsSupportedMediaExtension(".mp4"), "Video is not reported as a still image");
            Check(CompositionPolicy.IsSupportedVideoExtension(".mp4") && CompositionPolicy.IsSupportedVideoExtension(".WEBM") &&
                  CompositionPolicy.IsSupportedVideoExtension(".mov") && CompositionPolicy.IsSupportedVideoExtension(".m4v") &&
                  CompositionPolicy.IsSupportedVideoExtension(".avi"), "Video panel formats are supported");
            Check(!CompositionPolicy.IsSupportedVideoExtension(".png"), "Images are not reported as video");
            Check(CompositionPolicy.IsSupportedPanelExtension(".png") && CompositionPolicy.IsSupportedPanelExtension(".mp4") &&
                  CompositionPolicy.IsSupportedPanelExtension(".GIF"), "Panel media accepts images, GIF and video");
            Check(!CompositionPolicy.IsSupportedPanelExtension(".webp"), "Unsupported image formats are rejected");
            Check(!CompositionPolicy.IsSupportedPanelExtension(""), "Empty extension is rejected");

            Check(CompositionPolicy.GetInactiveReason(false, false, true, true, true, true, true) == CompositionInactiveReason.Disabled, "Disabled composition wins over every other state");
            Check(CompositionPolicy.GetInactiveReason(true, false, true, true, true, true, true) == CompositionInactiveReason.GameplayIsolationOff, "Composition requires gameplay isolation");
            Check(CompositionPolicy.GetInactiveReason(true, true, true, false, false, true, false) == CompositionInactiveReason.MenuScene, "Menu scene wins over missing cameras");
            Check(CompositionPolicy.GetInactiveReason(true, true, false, false, true, true, true) == CompositionInactiveReason.HeroUnavailable, "Missing hero blocks composition");
            Check(CompositionPolicy.GetInactiveReason(true, true, false, true, false, true, true) == CompositionInactiveReason.NotGameplayScene, "Non-gameplay scene blocks composition");
            Check(CompositionPolicy.GetInactiveReason(true, true, false, true, true, true, true) == CompositionInactiveReason.Transition, "Transition blocks composition");
            Check(CompositionPolicy.GetInactiveReason(true, true, false, true, true, false, false) == CompositionInactiveReason.WorldCameraMissing, "Missing world camera blocks composition");
            Check(CompositionPolicy.GetInactiveReason(true, true, false, true, true, false, true) == CompositionInactiveReason.None, "Full gameplay state activates the collage without a HUD camera");

            RunCompositionProfileChecks();
        }

        static void RunCompositionProfileChecks()
        {
            CompositionProfile defaults = CompositionProfile.CreateDefault();
            Check(defaults.Layout == CompositionPolicy.LayoutThreeColumns && defaults.Placement == CompositionPolicy.PlacementCover,
                "Default composition profile uses three columns and Cover placement");
            Check(defaults.Panels.Length == CompositionPolicy.MaxMediaPanels && defaults.Panels.Length == 4,
                "Default composition profile has four media tile slots");
            for (int i = 0; i < defaults.Panels.Length; i++)
            {
                Check(defaults.Panels[i].File == "" && !defaults.Panels[i].Paused && !defaults.Panels[i].SoundEnabled &&
                      !defaults.Panels[i].Muted && Math.Abs(defaults.Panels[i].Volume - MediaAudioPolicy.DefaultVolume) < 0.0001f, "Default media tile " + i + " is empty and silent");
            }

            string serialized = CompositionProfileCodec.Serialize(defaults);
            Check(serialized.Contains("formatVersion=2") && serialized.Contains("layout=ThreeColumns") &&
                  serialized.Contains("placement=Cover"), "Serialized profile declares format version 2, layout and placement");
            Check(serialized.Contains("panel.0.file=") && serialized.Contains("panel.3.muted=false"), "Serialized profile contains all four media tile slots");
            Check(!serialized.Contains("gap="), "Serialized profile has no gap field");

            CompositionProfile custom = CompositionProfile.CreateDefault();
            custom.Layout = CompositionPolicy.LayoutGrid2x2;
            custom.Placement = CompositionPolicy.PlacementContain;
            custom.Panels[0].File = "фон меню.png";
            custom.Panels[0].Paused = true;
            custom.Panels[0].SoundEnabled = true;
            custom.Panels[0].Volume = 0.75f;
            custom.Panels[0].Muted = true;
            custom.Panels[2].File = "clip.MP4";
            custom.Panels[2].Volume = 0.25f;
            custom.Panels[3].File = "final.gif";
            CompositionProfile parsed = CompositionProfileCodec.Parse(CompositionProfileCodec.Serialize(custom));
            Check(parsed.Layout == CompositionPolicy.LayoutGrid2x2 && parsed.Placement == CompositionPolicy.PlacementContain, "Profile round-trips layout and placement");
            Check(parsed.Panels[0].File == "фон меню.png" && parsed.Panels[0].Paused && parsed.Panels[0].SoundEnabled &&
                  parsed.Panels[0].Muted && Math.Abs(parsed.Panels[0].Volume - 0.75f) < 0.0001f, "Profile round-trips a full media tile");
            Check(parsed.Panels[2].File == "clip.MP4" && Math.Abs(parsed.Panels[2].Volume - 0.25f) < 0.0001f, "Profile round-trips a video tile");
            Check(parsed.Panels[3].File == "final.gif", "Profile round-trips the fourth media tile");
            Check(parsed.Panels[1].File == "" && !parsed.Panels[1].SoundEnabled, "Unused tile stays empty after parsing");

            CompositionProfile legacy = CompositionProfileCodec.Parse(
                "# old preset\r\nformatVersion=1\r\ngap=6\r\nlayout=Grid2x2\r\npanel.0.file=Безымянный.png\r\npanel.1.file=0215.mp4\r\npanel.2.file=maxresdefault.jpg\r\n");
            Check(legacy.Layout == CompositionPolicy.LayoutGrid2x2 && legacy.Placement == CompositionPolicy.PlacementCover, "Legacy preset keeps its layout and gains Cover placement");
            Check(legacy.Panels[0].File == "Безымянный.png" && legacy.Panels[1].File == "0215.mp4" && legacy.Panels[2].File == "maxresdefault.jpg",
                "Legacy preset keeps the user's media assignments");
            Check(legacy.Panels[3].File == "", "Legacy preset leaves the fourth tile empty for the background color");
            Check(!CompositionProfileCodec.Serialize(legacy).Contains("gap="), "Saving a legacy preset drops the old gap field");

            CompositionProfile versioned;
            int parsedVersion;
            string versionError;
            Check(CompositionProfileCodec.TryParse(CompositionProfileCodec.Serialize(custom), out versioned, out parsedVersion, out versionError) &&
                  parsedVersion == CompositionProfileCodec.FormatVersion && versionError.Length == 0 && versioned.Layout == CompositionPolicy.LayoutGrid2x2,
                "Current format version parses with its version reported");
            Check(CompositionProfileCodec.TryParse("formatVersion=1\nlayout=Grid2x2\npanel.0.file=old.png\n", out versioned, out parsedVersion, out versionError) &&
                  parsedVersion == 1 && versionError.Length == 0 && versioned.Layout == CompositionPolicy.LayoutGrid2x2 && versioned.Panels[0].File == "old.png",
                "Legacy format version 1 remains supported");
            Check(!CompositionProfileCodec.TryParse("formatVersion=3\nlayout=Grid2x2\n", out versioned, out parsedVersion, out versionError) &&
                  parsedVersion == 3 && versionError.Contains("not supported"),
                "Unknown future format version is refused");
            Check(!CompositionProfileCodec.TryParse("formatVersion=0\nlayout=Grid2x2\n", out versioned, out parsedVersion, out versionError) &&
                  parsedVersion == 0 && versionError.Contains("not supported"),
                "Version zero is refused");
            Check(!CompositionProfileCodec.TryParse("formatVersion=future\nlayout=Grid2x2\n", out versioned, out parsedVersion, out versionError) &&
                  parsedVersion == -1 && versionError.Contains("not a number"),
                "Non-numeric format version is refused");
            Check(!CompositionProfileCodec.TryParse("formatVersion=\nlayout=Grid2x2\n", out versioned, out parsedVersion, out versionError) &&
                  parsedVersion == -1,
                "Empty format version is refused");
            Check(CompositionProfileCodec.TryParse("", out versioned, out parsedVersion, out versionError) &&
                  parsedVersion == CompositionProfileCodec.MinimumSupportedFormatVersion && versioned.Layout == CompositionPolicy.LayoutThreeColumns,
                "Empty preset text is accepted as defaults");
            Check(CompositionProfileCodec.Parse("formatVersion=99\nlayout=Grid2x2\n").Layout == CompositionPolicy.LayoutThreeColumns,
                "Parse falls back to defaults instead of loading an unsupported version");
            Check(CompositionProfileCodec.Parse("formatVersion=99\nlayout=Grid2x2\npanel.0.file=unsafe.png\n").Panels[0].File == "",
                "Unsupported preset contents are not applied");

            CompositionProfile unknown = CompositionProfileCodec.Parse(
                "\r\n# comment\r\nlayout=mainandbottom\r\nplacement=STRETCH\r\ngap=-5\r\nfutureKey=1\r\npanel.0.file=../evil.png\r\npanel.1.file=image.webp\r\npanel.2.file=Clip.MOV\r\npanel.2.sound=yes\r\npanel.2.volume=2\r\npanel.3.file=poster.png\r\npanel.9.file=ignored.png\r\n");
            Check(unknown.Layout == CompositionPolicy.LayoutMainAndBottom, "Profile parsing normalizes layout names");
            Check(unknown.Placement == CompositionPolicy.PlacementStretch, "Profile parsing normalizes placement names");
            Check(unknown.Panels[0].File == "" && unknown.Panels[1].File == "", "Unsafe or unsupported tile files are dropped");
            Check(unknown.Panels[2].File == "Clip.MOV" && unknown.Panels[2].SoundEnabled && Math.Abs(unknown.Panels[2].Volume - 1f) < 0.0001f, "Profile parsing accepts video files, yes booleans and clamps volume");
            Check(unknown.Panels[3].File == "poster.png", "Profile parsing accepts the fourth tile");
            Check(unknown.Panels[0].File != "ignored.png", "Out-of-range tile indexes are ignored");

            CompositionProfile garbage = CompositionProfileCodec.Parse("layout=garbage\nplacement=garbage\npanel.0.volume=NaN\npanel.1.paused=1\n");
            Check(garbage.Layout == CompositionPolicy.LayoutThreeColumns, "Unknown profile layout falls back to three columns");
            Check(garbage.Placement == CompositionPolicy.PlacementCover, "Unknown profile placement falls back to Cover");
            Check(Math.Abs(garbage.Panels[0].Volume - MediaAudioPolicy.DefaultVolume) < 0.0001f, "Invalid profile volume keeps the default");
            Check(garbage.Panels[1].Paused, "Profile parses numeric booleans");

            Check(CompositionProfileCodec.Parse(null).Layout == CompositionPolicy.LayoutThreeColumns, "Empty profile text returns defaults");
            Check(CompositionProfileCodec.IsSafeMediaName("моё меню.gif"), "Unicode media names are safe");
            Check(CompositionProfileCodec.IsSafeMediaName("clip with spaces.mp4"), "Spaces in media names are safe");
            foreach (string name in new[] { "", null, ".", "..", "../a.png", "..\\a.png", "C:\\a.png", "a:b.png", "a/b.png", "a\n.png", "a\r.png" })
            {
                Check(!CompositionProfileCodec.IsSafeMediaName(name), "Unsafe media name is rejected: " + (name ?? "<null>"));
            }

            CompositionProfile clamped = CompositionProfile.CreateDefault();
            clamped.Panels[0].Volume = -2f;
            clamped.Panels[1].Volume = 5f;
            CompositionProfile reParsed = CompositionProfileCodec.Parse(CompositionProfileCodec.Serialize(clamped));
            Check(Math.Abs(reParsed.Panels[0].Volume) < 0.0001f && Math.Abs(reParsed.Panels[1].Volume - 1f) < 0.0001f, "Serialization clamps panel volume");

            CompositionProfile clone = custom.Clone();
            clone.Panels[0].File = "changed.png";
            clone.Layout = CompositionPolicy.LayoutTwoVertical;
            clone.Placement = CompositionPolicy.PlacementStretch;
            Check(custom.Panels[0].File == "фон меню.png" && custom.Layout == CompositionPolicy.LayoutGrid2x2 &&
                  custom.Placement == CompositionPolicy.PlacementContain, "Cloning a profile does not mutate the source");
            Check(CompositionProfileCodec.Serialize(null).Contains("layout=ThreeColumns"), "Serializing a missing profile uses defaults");

            Check(MediaAudioPolicy.EffectiveVolume(true, 0.5f, false, 0.5f) > 0.24f &&
                  MediaAudioPolicy.EffectiveVolume(false, 1f, false, 1f) == 0f &&
                  MediaAudioPolicy.EffectiveVolume(true, 1f, true, 1f) == 0f, "Panel audio follows enable, mute and volume rules");
        }
    }
}
