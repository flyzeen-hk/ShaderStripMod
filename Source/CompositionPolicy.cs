using System;

namespace ShaderStripMod
{
    internal enum CompositionInactiveReason
    {
        None,
        Disabled,
        GameplayIsolationOff,
        MenuScene,
        HeroUnavailable,
        NotGameplayScene,
        Transition,
        WorldCameraMissing,
        LayoutUnavailable,
        SurfaceUnavailable,
        RuntimeError
    }

    internal struct CompositionRect
    {
        internal int X;
        internal int Y;
        internal int Width;
        internal int Height;

        internal CompositionRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        internal bool IsEmpty { get { return Width <= 0 || Height <= 0; } }

        internal bool Overlaps(CompositionRect other)
        {
            return X < other.X + other.Width && other.X < X + Width &&
                   Y < other.Y + other.Height && other.Y < Y + Height;
        }
    }

    // The collage fills the whole screen: every panel is a media tile, tiles touch
    // exactly (zero gap), and the live game keeps rendering full screen behind them.
    internal struct CompositionLayout
    {
        internal CompositionRect[] Panels;
        internal int ScreenWidth;
        internal int ScreenHeight;
        internal string LayoutName;

        internal int PanelCount { get { return Panels == null ? 0 : Panels.Length; } }

        internal bool IsValid
        {
            get
            {
                if (ScreenWidth < 1 || ScreenHeight < 1) return false;
                if (Panels == null || Panels.Length < 1 || Panels.Length > CompositionPolicy.MaxMediaPanels) return false;
                for (int i = 0; i < Panels.Length; i++)
                {
                    CompositionRect panel = Panels[i];
                    if (panel.IsEmpty) return false;
                    if (panel.Width < CompositionPolicy.MinimumPanelPixels || panel.Height < CompositionPolicy.MinimumPanelPixels) return false;
                    if (panel.X < 0 || panel.Y < 0) return false;
                    if (panel.X + panel.Width > ScreenWidth || panel.Y + panel.Height > ScreenHeight) return false;
                    for (int j = i + 1; j < Panels.Length; j++)
                    {
                        if (panel.Overlaps(Panels[j])) return false;
                    }
                }

                return true;
            }
        }
    }

    internal static class CompositionPolicy
    {
        internal const string LayoutFullScreen = "FullScreen";
        internal const string LayoutTwoVertical = "TwoVertical";
        internal const string LayoutTwoHorizontal = "TwoHorizontal";
        internal const string LayoutThreeColumns = "ThreeColumns";
        internal const string LayoutThreeRows = "ThreeRows";
        internal const string LayoutMainAndSide = "MainAndSide";
        internal const string LayoutMainAndBottom = "MainAndBottom";
        internal const string LayoutGrid2x2 = "Grid2x2";
        internal const string PlacementCover = "Cover";
        internal const string PlacementContain = "Contain";
        internal const string PlacementStretch = "Stretch";
        internal const float MainPanelFraction = 0.58f;
        internal const float MainRowFraction = 0.62f;
        internal const int MinimumPanelPixels = 16;
        internal const int MaxMediaPanels = 4;

        internal static readonly string[] LayoutNames =
        {
            LayoutFullScreen,
            LayoutTwoVertical,
            LayoutTwoHorizontal,
            LayoutThreeColumns,
            LayoutThreeRows,
            LayoutMainAndSide,
            LayoutMainAndBottom,
            LayoutGrid2x2
        };

        internal static readonly string[] PlacementNames =
        {
            PlacementCover,
            PlacementContain,
            PlacementStretch
        };

        internal static string NormalizeLayout(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < LayoutNames.Length; i++)
                {
                    if (string.Equals(value, LayoutNames[i], StringComparison.OrdinalIgnoreCase)) return LayoutNames[i];
                }
            }

            return LayoutThreeColumns;
        }

        internal static string NormalizePlacement(string value)
        {
            if (string.Equals(value, PlacementContain, StringComparison.OrdinalIgnoreCase)) return PlacementContain;
            if (string.Equals(value, PlacementStretch, StringComparison.OrdinalIgnoreCase)) return PlacementStretch;
            return PlacementCover;
        }

        internal static bool IsSupportedMediaExtension(string extension)
        {
            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsSupportedVideoExtension(string extension)
        {
            return string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".webm", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".mov", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".m4v", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".avi", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsSupportedPanelExtension(string extension)
        {
            return IsSupportedMediaExtension(extension) || IsSupportedVideoExtension(extension);
        }

        internal static string GetLayoutDisplayName(string layout)
        {
            switch (NormalizeLayout(layout))
            {
                case LayoutFullScreen:
                    return "Full screen";
                case LayoutTwoVertical:
                    return "Two vertical";
                case LayoutTwoHorizontal:
                    return "Two horizontal";
                case LayoutThreeRows:
                    return "Three rows";
                case LayoutMainAndSide:
                    return "Main + side";
                case LayoutMainAndBottom:
                    return "Main + bottom";
                case LayoutGrid2x2:
                    return "2x2 grid";
                default:
                    return "Three columns";
            }
        }

        internal static string GetPanelName(string layout, int panelIndex)
        {
            switch (NormalizeLayout(layout))
            {
                case LayoutFullScreen:
                    return panelIndex == 0 ? "Full screen" : "";
                case LayoutTwoVertical:
                    return panelIndex == 0 ? "Left half" : (panelIndex == 1 ? "Right half" : "");
                case LayoutTwoHorizontal:
                    return panelIndex == 0 ? "Top half" : (panelIndex == 1 ? "Bottom half" : "");
                case LayoutThreeColumns:
                    return panelIndex == 0 ? "Left column" : (panelIndex == 1 ? "Center column" : (panelIndex == 2 ? "Right column" : ""));
                case LayoutThreeRows:
                    return panelIndex == 0 ? "Top row" : (panelIndex == 1 ? "Middle row" : (panelIndex == 2 ? "Bottom row" : ""));
                case LayoutMainAndSide:
                    return panelIndex == 0 ? "Main area" : (panelIndex == 1 ? "Right top" : (panelIndex == 2 ? "Right bottom" : ""));
                case LayoutMainAndBottom:
                    return panelIndex == 0 ? "Main area" : (panelIndex == 1 ? "Bottom left" : (panelIndex == 2 ? "Bottom right" : ""));
                case LayoutGrid2x2:
                    switch (panelIndex)
                    {
                        case 0: return "Top-left cell";
                        case 1: return "Top-right cell";
                        case 2: return "Bottom-left cell";
                        case 3: return "Bottom-right cell";
                        default: return "";
                    }
                default:
                    return "";
            }
        }

        internal static string GetPanelName(CompositionLayout layout, int panelIndex)
        {
            if (layout.Panels == null || panelIndex < 0 || panelIndex >= layout.Panels.Length)
            {
                return "";
            }

            return GetPanelName(layout.LayoutName, panelIndex);
        }

        internal static CompositionLayout CalculateLayout(string layoutName, int screenWidth, int screenHeight)
        {
            CompositionLayout layout = new CompositionLayout
            {
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight,
                LayoutName = NormalizeLayout(layoutName)
            };
            if (screenWidth < 1 || screenHeight < 1)
            {
                return layout;
            }

            switch (layout.LayoutName)
            {
                case LayoutFullScreen:
                    layout.Panels = new[] { new CompositionRect(0, 0, screenWidth, screenHeight) };
                    break;
                case LayoutTwoVertical:
                {
                    int left = screenWidth / 2;
                    int right = screenWidth - left;
                    layout.Panels = new[]
                    {
                        new CompositionRect(0, 0, left, screenHeight),
                        new CompositionRect(left, 0, right, screenHeight)
                    };
                    break;
                }
                case LayoutTwoHorizontal:
                {
                    int bottom = screenHeight / 2;
                    int top = screenHeight - bottom;
                    layout.Panels = new[]
                    {
                        new CompositionRect(0, bottom, screenWidth, top),
                        new CompositionRect(0, 0, screenWidth, bottom)
                    };
                    break;
                }
                case LayoutThreeColumns:
                {
                    int first = Split(screenWidth, 1, 3);
                    int second = Split(screenWidth, 2, 3);
                    layout.Panels = new[]
                    {
                        new CompositionRect(0, 0, first, screenHeight),
                        new CompositionRect(first, 0, second - first, screenHeight),
                        new CompositionRect(second, 0, screenWidth - second, screenHeight)
                    };
                    break;
                }
                case LayoutThreeRows:
                {
                    int first = Split(screenHeight, 2, 3);
                    int second = Split(screenHeight, 1, 3);
                    layout.Panels = new[]
                    {
                        new CompositionRect(0, first, screenWidth, screenHeight - first),
                        new CompositionRect(0, second, screenWidth, first - second),
                        new CompositionRect(0, 0, screenWidth, second)
                    };
                    break;
                }
                case LayoutMainAndSide:
                {
                    int mainWidth = ClampFraction(screenWidth, MainPanelFraction);
                    int sideWidth = screenWidth - mainWidth;
                    int bottomHeight = screenHeight / 2;
                    int topHeight = screenHeight - bottomHeight;
                    layout.Panels = new[]
                    {
                        new CompositionRect(0, 0, mainWidth, screenHeight),
                        new CompositionRect(mainWidth, bottomHeight, sideWidth, topHeight),
                        new CompositionRect(mainWidth, 0, sideWidth, bottomHeight)
                    };
                    break;
                }
                case LayoutMainAndBottom:
                {
                    int mainHeight = ClampFraction(screenHeight, MainRowFraction);
                    int bottomHeight = screenHeight - mainHeight;
                    int leftWidth = screenWidth / 2;
                    int rightWidth = screenWidth - leftWidth;
                    layout.Panels = new[]
                    {
                        new CompositionRect(0, bottomHeight, screenWidth, mainHeight),
                        new CompositionRect(0, 0, leftWidth, bottomHeight),
                        new CompositionRect(leftWidth, 0, rightWidth, bottomHeight)
                    };
                    break;
                }
                case LayoutGrid2x2:
                {
                    int leftWidth = screenWidth / 2;
                    int rightWidth = screenWidth - leftWidth;
                    int bottomHeight = screenHeight / 2;
                    int topHeight = screenHeight - bottomHeight;
                    layout.Panels = new[]
                    {
                        new CompositionRect(0, bottomHeight, leftWidth, topHeight),
                        new CompositionRect(leftWidth, bottomHeight, rightWidth, topHeight),
                        new CompositionRect(0, 0, leftWidth, bottomHeight),
                        new CompositionRect(leftWidth, 0, rightWidth, bottomHeight)
                    };
                    break;
                }
            }

            if (layout.Panels != null)
            {
                for (int i = 0; i < layout.Panels.Length; i++)
                {
                    CompositionRect panel = layout.Panels[i];
                    if (panel.Width < MinimumPanelPixels || panel.Height < MinimumPanelPixels)
                    {
                        layout.Panels = null;
                        break;
                    }
                }
            }

            return layout;
        }

        private static int Split(int total, int index, int count)
        {
            return (int)((long)total * index / count);
        }

        private static int ClampFraction(int total, float fraction)
        {
            int value = (int)Math.Round(total * fraction);
            int max = total - MinimumPanelPixels;
            if (max < MinimumPanelPixels)
            {
                return total / 2;
            }

            if (value < MinimumPanelPixels) return MinimumPanelPixels;
            if (value > max) return max;
            return value;
        }

        internal static CompositionInactiveReason GetInactiveReason(bool compositionEnabled, bool gameplayIsolationEnabled,
            bool menuScene, bool heroAvailable, bool gameplayScene, bool transition, bool worldCameraAvailable)
        {
            if (!compositionEnabled) return CompositionInactiveReason.Disabled;
            if (!gameplayIsolationEnabled) return CompositionInactiveReason.GameplayIsolationOff;
            if (menuScene) return CompositionInactiveReason.MenuScene;
            if (!heroAvailable) return CompositionInactiveReason.HeroUnavailable;
            if (!gameplayScene) return CompositionInactiveReason.NotGameplayScene;
            if (transition) return CompositionInactiveReason.Transition;
            if (!worldCameraAvailable) return CompositionInactiveReason.WorldCameraMissing;
            return CompositionInactiveReason.None;
        }
    }
}
