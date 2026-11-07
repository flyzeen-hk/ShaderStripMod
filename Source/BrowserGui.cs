using System.Collections.Generic;
using System.IO;
using System.Text;
using Modding;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private GUIStyle browserInfoWrappedStyle;
        private string WrapBrowserInfo(string text)
        {
            if(browserInfoWrappedStyle==null) browserInfoWrappedStyle=new GUIStyle(GUI.skin.label) { wordWrap=true };
            return (text??"").Replace("\\","\\\u200b").Replace("/","/\u200b").Replace("=","=\u200b").Replace("_","_\u200b");
        }
        private int browserGuiReleaseHotControl;
        internal void DrawTextureBrowserGui()
        {
            ReleasePendingMaskEditorPointer();
            if (!textureBrowserVisible)
            {
                return;
            }

            if (textureBrowserDetailTab != TextureBrowserHotkeysTab) CancelHotkeyCapture();

            Event closeEvent=Event.current;
            bool closeEscape=!IsHotkeyCaptureActive()&&hotkeyCaptureEscapeFrame!=Time.frameCount&&closeEvent!=null&&closeEvent.type==EventType.KeyDown&&closeEvent.keyCode==KeyCode.Escape;
            if(closeEvent!=null && BrowserInputPolicy.ShouldClose(textureBrowserVisible,closeEscape))
            { SetTextureBrowserVisible(false);closeEvent.Use();return; }
            UpdateTextureBrowserCursor();
            bool fullMask=textureBrowserDetailTab==TextureBrowserMaskTab&&!textureBrowserPreserveMode;
            float browserScale = fullMask ? Mathf.Min(1f,GetTextureBrowserScale()) : GetTextureBrowserScale();
            ClampTextureBrowserWindow(browserScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            bool previousEnabled=GUI.enabled;Color previousColor=GUI.color,previousBackground=GUI.backgroundColor;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(browserScale, browserScale, 1f));
            try
            {
                if(fullMask)
                {
                    var region=MaskWorkspaceLayout.Window(Screen.width,Screen.height,browserScale);
                    var window=new Rect(region.X,region.Y,region.Width,region.Height);
                    DrawTextureBrowserBackdrop(window);
                    GUI.Window(TextureBrowserWindowId,window,DrawTextureBrowserWindow,"ShaderStripMod");
                }
                else
                {
                    DrawTextureBrowserBackdrop(textureBrowserWindowRect);
                    textureBrowserWindowRect = GUI.Window(TextureBrowserWindowId, textureBrowserWindowRect, DrawTextureBrowserWindow, UiText("ShaderStripMod Texture Browser"));
                }
            }
            finally
            {
                GUI.matrix = previousMatrix;GUI.enabled=previousEnabled;GUI.color=previousColor;GUI.backgroundColor=previousBackground;
            }

            Event currentEvent = Event.current;
            if (currentEvent != null && currentEvent.type != EventType.Layout && currentEvent.type != EventType.Repaint)
            {
                currentEvent.Use();
            }
        }

        private void DrawTextureBrowserBackdrop(Rect rect)
        {
            Color previous=GUI.color;
            try{GUI.color=new Color(.065f,.075f,.095f,1);GUI.DrawTexture(rect,Texture2D.whiteTexture);}
            finally{GUI.color=previous;}
        }

        private float GetTextureBrowserScale()
        {
            float screenWidth = Mathf.Max(1f, Screen.width - 20f);
            float screenHeight = Mathf.Max(1f, Screen.height - 20f);
            float fitScale = Mathf.Min(screenWidth / TextureBrowserBaseWidth, screenHeight / TextureBrowserBaseHeight);
            return Mathf.Clamp(fitScale, TextureBrowserMinScale, TextureBrowserMaxScale);
        }

        private void ClampTextureBrowserWindow(float scale)
        {
            scale = Mathf.Max(0.01f, scale);
            float virtualScreenWidth = Mathf.Max(1f, Screen.width / scale);
            float virtualScreenHeight = Mathf.Max(1f, Screen.height / scale);
            float maxWidth = Mathf.Max(420f, virtualScreenWidth - 20f);
            float maxHeight = Mathf.Max(360f, virtualScreenHeight - 20f);
            textureBrowserWindowRect.width = Mathf.Min(TextureBrowserBaseWidth, maxWidth);
            textureBrowserWindowRect.height = Mathf.Min(TextureBrowserBaseHeight, maxHeight);
            textureBrowserWindowRect.x = Mathf.Clamp(textureBrowserWindowRect.x, 0f, Mathf.Max(0f, virtualScreenWidth - textureBrowserWindowRect.width));
            textureBrowserWindowRect.y = Mathf.Clamp(textureBrowserWindowRect.y, 0f, Mathf.Max(0f, virtualScreenHeight - textureBrowserWindowRect.height));
        }

        private void DrawTextureBrowserWindow(int windowId)
        {
            float width = textureBrowserWindowRect.width;
            float height = textureBrowserWindowRect.height;

            if (textureBrowserDetailTab == TextureBrowserMaskTab && !textureBrowserPreserveMode)
            {
                var bounds=MaskWorkspaceLayout.Window(Screen.width,Screen.height,Mathf.Min(1f,GetTextureBrowserScale()));width=bounds.Width;height=bounds.Height;
                if (UiButton(new Rect(12,28,170,28), "Back to browser")) SelectTextureBrowserDetailTab(TextureBrowserPreviewTab);
                var selected = selectedTextureBrowserIndex>=0&&selectedTextureBrowserIndex<textureBrowserEntries.Count ? textureBrowserEntries[selectedTextureBrowserIndex] : null;
                UiLabel(new Rect(194,30,Mathf.Max(1,width-300),24),UiText("Mask selection")+(selected!=null ? " | "+selected.Name : ""));
                if (UiButton(new Rect(width-94,28,82,28), "Close")) SetTextureBrowserVisible(false);
                if (textureBrowserVisible && textureBrowserDetailTab == TextureBrowserMaskTab)
                {
                    var entry = selectedTextureBrowserIndex >= 0 && selectedTextureBrowserIndex < textureBrowserEntries.Count ? textureBrowserEntries[selectedTextureBrowserIndex] : null;
                    var region = MaskWorkspaceLayout.Content(width,height);
                    DrawSpriteMaskTab(entry,new Rect(region.X,region.Y,region.Width,region.Height));
                }
                GUI.DragWindow(new Rect(0,0,width,24));
                return;
            }
            DrawTextureBrowserToolbar(width);
            DrawTextureBrowserModeHint(width);

            float listWidth = Mathf.Clamp(width * 0.36f, 180f, 320f);
            if (width < 620f)
            {
                listWidth = Mathf.Clamp(width * 0.44f, 160f, 220f);
            }

            Rect listRect = new Rect(12f, 108f, listWidth, height - 154f);
            Rect detailRect = new Rect(listRect.xMax + 12f, 108f, Mathf.Max(160f, width - listRect.xMax - 24f), height - 154f);
            DrawTextureBrowserList(listRect);
            DrawTextureBrowserDetails(detailRect);
            DrawTextureBrowserFooter(width, height);

            GUI.DragWindow(new Rect(0f, 0f, width, 24f));
        }

        private void DrawTextureBrowserFooter(float width, float height)
        {
            Rect footer = new Rect(12f, height - 34f, Mathf.Max(1f, width - 24f), 22f);
            string hints = "Esc " + UiText("Close") + "   " +
                HotkeyLabel(HotkeyActions.ToggleBrowser) + " " + UiText("Browser") + "   " +
                HotkeyLabel(HotkeyActions.ToggleCameraPin) + " " + UiText("Camera") + "   " +
                HotkeyLabel(HotkeyActions.ToggleGameplayIsolation) + " " + UiText("Isolation");
            Color previous = GUI.color;
            try
            {
                GUI.color = new Color(1f, 1f, 1f, .7f);
                UiLabel(footer, hints);
            }
            finally
            {
                GUI.color = previous;
            }
        }

        private void DrawTextureBrowserToolbar(float width)
        {
            float gap = 6f;
            float buttonHeight = 24f;
            float searchY = 28f;
            float closeWidth = 82f;
            float clearWidth = 26f;
            Rect searchRect = new Rect(12f, searchY, Mathf.Max(120f, width - closeWidth - clearWidth - 36f), buttonHeight);
            Rect clearRect = new Rect(width - closeWidth - clearWidth - 18f, searchY, clearWidth, buttonHeight);
            Rect closeRect = new Rect(width - closeWidth - 12f, searchY, closeWidth, buttonHeight);

            textureBrowserSearch = GUI.TextField(searchRect, textureBrowserSearch);
            if (string.IsNullOrEmpty(textureBrowserSearch))
            {
                Color previous = GUI.color;
                try
                {
                    GUI.color = new Color(1f, 1f, 1f, .45f);
                    GUI.Label(new Rect(searchRect.x + 6f, searchRect.y + 3f, Mathf.Max(1f, searchRect.width - 12f), searchRect.height - 4f), UiText("Search by name, kind or details"));
                }
                finally
                {
                    GUI.color = previous;
                }
            }
            else if (UiButton(clearRect, "×"))
            {
                textureBrowserSearch = "";
            }

            if (UiButton(closeRect, "Close"))
            {
                SetTextureBrowserVisible(false);
            }

            float navY = searchY + buttonHeight + 6f;
            float x = 12f;
            float availableWidth = Mathf.Max(120f, width - 24f);
            float buttonWidth = Mathf.Max(24f, (availableWidth - gap * 7f) / 8f);

            if (DrawModeButton(new Rect(x, navY, buttonWidth, buttonHeight), "Loaded", !textureBrowserEntityMode && !textureBrowserNearbyMode && !textureBrowserAtlasMode && !textureBrowserPreserveMode && !textureBrowserProjectileMode && !textureBrowserMenuMode))
            {
                SelectTextureBrowserDetailTab(TextureBrowserInfoTab);
                RefreshTextureBrowserEntries();
            }

            x += buttonWidth + gap;
            if (DrawModeButton(new Rect(x, navY, buttonWidth, buttonHeight), "Entities", textureBrowserEntityMode))
            {
                SelectTextureBrowserDetailTab(TextureBrowserInfoTab);
                RefreshEntityBrowserEntries();
            }

            x += buttonWidth + gap;
            if (DrawModeButton(new Rect(x, navY, buttonWidth, buttonHeight), "Nearby", textureBrowserNearbyMode))
            {
                SelectTextureBrowserDetailTab(TextureBrowserInfoTab);
                RefreshNearbyTextureBrowserEntries();
            }

            x += buttonWidth + gap;
            if (DrawModeButton(new Rect(x, navY, buttonWidth, buttonHeight), "Atlases", textureBrowserAtlasMode))
            {
                SelectTextureBrowserDetailTab(TextureBrowserInfoTab);
                RefreshAtlasBrowserEntries();
            }

            x += buttonWidth + gap;
            if (DrawModeButton(new Rect(x, navY, buttonWidth, buttonHeight), "Rules", textureBrowserPreserveMode))
            {
                SelectTextureBrowserDetailTab(TextureBrowserInfoTab);
                RefreshPreserveManagerEntries();
            }

            x += buttonWidth + gap;
            if (DrawModeButton(new Rect(x, navY, buttonWidth, buttonHeight), "Projectiles", textureBrowserProjectileMode))
            {
                SelectTextureBrowserDetailTab(TextureBrowserInfoTab);
                RefreshRecentProjectileBrowserEntries();
            }

            x += buttonWidth + gap;
            if (DrawModeButton(new Rect(x, navY, buttonWidth, buttonHeight), "Menu", textureBrowserMenuMode))
            {
                SelectTextureBrowserDetailTab(TextureBrowserInfoTab);
                RefreshMenuTextureBrowserEntries();
            }

            x += buttonWidth + gap;
            if (UiButton(new Rect(x, navY, buttonWidth, buttonHeight), "Dump"))
            {
                DumpLoadedSpritesAndTextures();
            }
        }

        private string GetTextureBrowserModeDescription()
        {
            if (textureBrowserMenuMode) return "Title-menu sprites and backgrounds.";
            if (textureBrowserPreserveMode) return "Manage preserve rules and remove ignored ones.";
            if (textureBrowserProjectileMode) return "Recently captured projectile sprites.";
            if (textureBrowserAtlasMode) return "Full texture atlases.";
            if (textureBrowserEntityMode) return "Objects and sprites in the current room.";
            if (textureBrowserNearbyMode) return "Sprites near the hero.";
            return "All loaded sprites and textures.";
        }

        private void DrawTextureBrowserModeHint(float width)
        {
            Color previous = GUI.color;
            try
            {
                GUI.color = new Color(1f, 1f, 1f, .65f);
                UiLabel(new Rect(12f, 84f, Mathf.Max(1f, width - 24f), 18f), GetTextureBrowserModeDescription());
            }
            finally
            {
                GUI.color = previous;
            }
        }

        private bool DrawModeButton(Rect rect, string label, bool active)
        {
            Color previous=GUI.backgroundColor;
            try
            {
                if(active)GUI.backgroundColor=new Color(.35f,.85f,1f,1f);
                string localized=UiText(label);
                return UiButton(rect,active ? "✓ "+localized : localized);
            }
            finally{GUI.backgroundColor=previous;}
        }

        private void DrawTextureBrowserList(Rect listRect)
        {
            UiBox(listRect, "");
            List<int> indexes = GetFilteredTextureBrowserIndexes();
            float rowHeight = 24f;
            var area=BrowserListLayout.Viewport(listRect.x,listRect.y,listRect.width,listRect.height);
            Rect viewport=new Rect(area.X,area.Y,area.Width,area.Height);
            Rect contentRect = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, indexes.Count * rowHeight));
            textureBrowserListScroll = GUI.BeginScrollView(viewport, textureBrowserListScroll, contentRect);

            int overscanRows = Mathf.Max(0, browserListOverscanRows);
            int firstRow = virtualizedBrowserList ? Mathf.Max(0, (int)(textureBrowserListScroll.y / rowHeight) - overscanRows) : 0;
            int lastRow = virtualizedBrowserList ? Mathf.Min(indexes.Count - 1, (int)((textureBrowserListScroll.y + viewport.height) / rowHeight) + overscanRows) : indexes.Count - 1;
            for (int rowIndex = firstRow; rowIndex <= lastRow; rowIndex++)
            {
                int entryIndex = indexes[rowIndex];
                TextureBrowserEntry entry = textureBrowserEntries[entryIndex];
                Rect rowRect = new Rect(4f, rowIndex * rowHeight, contentRect.width - 8f, rowHeight - 2f);
                string preserveMark = entry.PreserveManagerRule ? "" : (IsTextureBrowserEntryPreserved(entry) ? "[P] " : "");
                string distanceMark = entry.HasDistance ? entry.Distance.ToString("0.00").PadLeft(6) + "  " : "";
                string label = preserveMark + distanceMark + entry.Kind + "  " + entry.Name;

                if (entryIndex == selectedTextureBrowserIndex)
                {
                    UiBox(rowRect, "");
                }

                if (UiButton(rowRect, label, GUI.skin.label))
                {
                    selectedTextureBrowserIndex = entryIndex;
                    textureBrowserDetailScroll = Vector2.zero;
                    ResetTextureBrowserPreviewView();
                }
            }

            GUI.EndScrollView();

            var footer=BrowserListLayout.Footer(listRect.x,listRect.y,listRect.width,listRect.height);
            Rect countRect = new Rect(footer.X,footer.Y,footer.Width,footer.Height);
            string modeLabel = textureBrowserMenuMode ? "Menu" : textureBrowserPreserveMode ? "Rules" : (textureBrowserProjectileMode ? "Projectiles" : (textureBrowserAtlasMode ? "Atlases" : (textureBrowserEntityMode ? "Entities" : (textureBrowserNearbyMode ? "Nearby" : "Loaded"))));
            UiLabel(countRect, UiText(modeLabel) + "  " + indexes.Count + " / " + textureBrowserEntries.Count);
        }

        private List<int> GetFilteredTextureBrowserIndexes()
        {
            filteredTextureBrowserIndexes.Clear();
            string search = textureBrowserSearch != null ? textureBrowserSearch.Trim() : "";

            for (int i = 0; i < textureBrowserEntries.Count; i++)
            {
                TextureBrowserEntry entry = textureBrowserEntries[i];
                if (MenuResourcePolicy.Matches(search, entry.Name, entry.Kind, entry.Details))
                {
                    filteredTextureBrowserIndexes.Add(i);
                    if (maxVisibleBrowserEntries > 0 && filteredTextureBrowserIndexes.Count >= maxVisibleBrowserEntries)
                    {
                        break;
                    }
                }
            }

            return filteredTextureBrowserIndexes;
        }

        private void DrawTextureBrowserDetails(Rect detailRect)
        {
            if (textureBrowserPreserveMode)
            {
                DrawPreserveManagerDetails(detailRect);
                return;
            }

            UiBox(detailRect, "");
            bool hasEntry = selectedTextureBrowserIndex >= 0 && selectedTextureBrowserIndex < textureBrowserEntries.Count;
            TextureBrowserEntry entry = hasEntry ? textureBrowserEntries[selectedTextureBrowserIndex] : null;
            Rect local = new Rect(detailRect.x + 12f, detailRect.y + 10f, detailRect.width - 24f, detailRect.height - 20f);

            UiLabel(new Rect(local.x, local.y, local.width, 24f), hasEntry ? entry.Kind + ": " + entry.Name : "Texture Browser");
            float tabsHeight = GetTextureBrowserDetailTabsHeight(local.width);
            DrawTextureBrowserDetailTabs(new Rect(local.x, local.y + 30f, local.width, tabsHeight));

            Rect content = new Rect(local.x, local.y + 40f + tabsHeight, local.width, Mathf.Max(40f, local.height - 40f - tabsHeight));
            if (textureBrowserDetailTab == TextureBrowserMenuDesignTab)
            {
                DrawCustomMenuTab(content);
            }
            else if (textureBrowserDetailTab == TextureBrowserCompositionTab)
            {
                DrawCompositionTab(content);
            }
            else if (textureBrowserDetailTab == TextureBrowserHotkeysTab)
            {
                DrawHotkeysTab(content);
            }
            else if (textureBrowserDetailTab == TextureBrowserSettingsTab)
            {
                DrawTextureBrowserSettingsTab(content);
            }
            else if (textureBrowserDetailTab == TextureBrowserPerformanceTab)
            {
                DrawTextureBrowserPerformanceTab(content);
            }
            else if (textureBrowserDetailTab == TextureBrowserMaskTab)
            {
                DrawSpriteMaskTab(entry, content);
            }
            else if (!hasEntry)
            {
                DrawTextureBrowserEmptyState(content);
            }
            else if (textureBrowserDetailTab == TextureBrowserPreviewTab)
            {
                DrawTextureBrowserPreviewTab(entry, content);
            }
            else if (textureBrowserDetailTab == TextureBrowserRulesTab)
            {
                DrawTextureBrowserRulesTab(entry, content);
            }
            else if (textureBrowserDetailTab == TextureBrowserExportTab)
            {
                DrawTextureBrowserExportTab(entry, content);
            }
            else
            {
                DrawTextureBrowserInfoTab(entry, content);
            }
        }

        private void DrawTextureBrowserEmptyState(Rect content)
        {
            if (browserInfoWrappedStyle == null) browserInfoWrappedStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            string text = UiText("Select an entry from the list on the left, or use Search to filter.");
            string tip = UiText("Settings controls the green screen and camera. Panels builds the media collage. Menu design changes the title screen.");
            float width = Mathf.Max(1f, content.width - 20f);
            float height = browserInfoWrappedStyle.CalcHeight(new GUIContent(text), width);
            UiLabel(new Rect(content.x + 10f, content.y + 12f, width, height), text, browserInfoWrappedStyle);
            float tipHeight = browserInfoWrappedStyle.CalcHeight(new GUIContent(tip), width);
            Color previous = GUI.color;
            try
            {
                GUI.color = new Color(1f, 1f, 1f, .7f);
                UiLabel(new Rect(content.x + 10f, content.y + 18f + height, width, tipHeight), tip, browserInfoWrappedStyle);
            }
            finally
            {
                GUI.color = previous;
            }
        }

        private void DrawTextureBrowserDetailTabs(Rect rect)
        {
            float gap = 6f;
            int columns = rect.width < 380f ? 3 : 5;
            int rows = columns == 3 ? 4 : 2;
            float rowHeight = (rect.height - gap * (rows - 1)) / rows;
            float tabWidth = Mathf.Max(42f, (rect.width - gap * (columns - 1)) / columns);
            DrawTextureBrowserDetailTabAt(rect, 0, columns, tabWidth, rowHeight, gap, "Info", TextureBrowserInfoTab);
            DrawTextureBrowserDetailTabAt(rect, 1, columns, tabWidth, rowHeight, gap, "Preview", TextureBrowserPreviewTab);
            DrawTextureBrowserDetailTabAt(rect, 2, columns, tabWidth, rowHeight, gap, "Rules", TextureBrowserRulesTab);
            DrawTextureBrowserDetailTabAt(rect, 3, columns, tabWidth, rowHeight, gap, "Export", TextureBrowserExportTab);
            DrawTextureBrowserDetailTabAt(rect, 4, columns, tabWidth, rowHeight, gap, "Mask", TextureBrowserMaskTab);
            DrawTextureBrowserDetailTabAt(rect, 5, columns, tabWidth, rowHeight, gap, "Settings", TextureBrowserSettingsTab);
            DrawTextureBrowserDetailTabAt(rect, 6, columns, tabWidth, rowHeight, gap, "Panels", TextureBrowserCompositionTab);
            DrawTextureBrowserDetailTabAt(rect, 7, columns, tabWidth, rowHeight, gap, "Menu design", TextureBrowserMenuDesignTab);
            DrawTextureBrowserDetailTabAt(rect, 8, columns, tabWidth, rowHeight, gap, "Performance", TextureBrowserPerformanceTab);
            DrawTextureBrowserDetailTabAt(rect, 9, columns, tabWidth, rowHeight, gap, "Hotkeys", TextureBrowserHotkeysTab);
        }

        private float GetTextureBrowserDetailTabsHeight(float width)
        {
            return width < 380f ? 114f : 54f;
        }

        private void DrawTextureBrowserDetailTabAt(Rect rect, int index, int columns, float tabWidth, float rowHeight, float gap, string label, int tab)
        {
            int column = index % columns;
            int row = index / columns;
            Rect tabRect = new Rect(rect.x + column * (tabWidth + gap), rect.y + row * (rowHeight + gap), tabWidth, rowHeight);
            if (DrawDetailTabButton(tabRect, label, tab))
            {
                SelectTextureBrowserDetailTab(tab);
            }
        }

        private bool DrawDetailTabButton(Rect rect, string label, int tab)
        {
            return DrawModeButton(rect,label,textureBrowserDetailTab==tab);
        }

        private void SelectTextureBrowserDetailTab(int tab)
        {
            if (textureBrowserDetailTab == tab)
            {
                return;
            }

            CancelMaskEditorPointer();CancelTexturePreviewPointer();
            textureBrowserDetailTab = tab;
            textureBrowserDetailScroll = Vector2.zero;
        }

        private void DrawTextureBrowserInfoTab(TextureBrowserEntry entry, Rect content)
        {
            UiBox(content, "");
            Rect infoRect = new Rect(content.x + 10f, content.y + 10f, content.width - 20f, content.height - 20f);
            string info = WrapBrowserInfo(BuildTextureBrowserInfo(entry));
            float infoContentHeight = Mathf.Max(infoRect.height, browserInfoWrappedStyle.CalcHeight(new GUIContent(info), infoRect.width - 20f) + 8f);
            Rect contentRect = new Rect(0f, 0f, infoRect.width - 20f, infoContentHeight);
            textureBrowserDetailScroll = GUI.BeginScrollView(infoRect, textureBrowserDetailScroll, contentRect);
            UiLabel(new Rect(0f, 0f, contentRect.width, contentRect.height), info,browserInfoWrappedStyle);
            GUI.EndScrollView();
        }

        private void DrawTextureBrowserPreviewTab(TextureBrowserEntry entry, Rect content)
        {
            DrawTexturePreviewControls(new Rect(content.x, content.y, content.width, 24f));
            float offset=34;
            if(entry.MenuResource)
            {
                if(UiButton(new Rect(content.x,content.y+30,content.width,26),menuShowAtlas ? "Show object artwork" : "Show full atlas")){menuShowAtlas=!menuShowAtlas;}
                offset=64;
            }
            Rect previewOuter = new Rect(content.x, content.y + offset, content.width, Mathf.Max(80f, content.height - offset));
            UiBox(previewOuter, "");
            DrawTexturePreview(entry, previewOuter);
        }

        private void DrawTextureBrowserRulesTab(TextureBrowserEntry entry, Rect content)
        {
            UiBox(content, "");
            if (browserInfoWrappedStyle == null) browserInfoWrappedStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            Rect inner = new Rect(content.x + 10f, content.y + 10f, content.width - 20f, content.height - 20f);
            if (entry.MenuResource)
            {
                string text = WrapBrowserInfo(UiText("Menu resources support Info, Preview and Export. Visibility rules and masks currently apply to room objects.") + "\n\n" + BuildTextureBrowserInfo(entry));
                float h = Mathf.Max(inner.height,browserInfoWrappedStyle.CalcHeight(new GUIContent(text),inner.width-20));
                textureBrowserDetailScroll = GUI.BeginScrollView(inner,textureBrowserDetailScroll,new Rect(0,0,inner.width-20,h));
                UiLabel(new Rect(0,0,inner.width-20,h),text,browserInfoWrappedStyle);GUI.EndScrollView();
                return;
            }
            string info=WrapBrowserInfo(BuildTextureBrowserInfo(entry));
            float width=Mathf.Max(1,inner.width-20);
            float infoHeight=browserInfoWrappedStyle.CalcHeight(new GUIContent(info),width);
            float rendererHeight=RuleControlsLayout.Height(width,4),assetHeight=RuleControlsLayout.Height(width,4),pathHeight=RuleControlsLayout.Height(width,3);
            float rendererHelp=RulesHelpHeight(width,"Preserve one renderer without preserving the whole texture.");
            float assetHelp=RulesHelpHeight(width,"Preserve every renderer that uses this sprite, texture or material.");
            float pathHelp=RulesHelpHeight(width,"Preserve objects by hierarchy path.");
            float fullHeight=rendererHeight+assetHeight+pathHeight+infoHeight+rendererHelp+assetHelp+pathHelp+210;
            textureBrowserDetailScroll=GUI.BeginScrollView(inner,textureBrowserDetailScroll,new Rect(0,0,width,Mathf.Max(inner.height,fullHeight)));
            float y=0;
            UiSectionLabel(ref y,width,"Renderer rules");
            DrawRulesHelp(ref y,width,"Preserve one renderer without preserving the whole texture.");
            DrawRendererPreserveControls(entry,new Rect(0,y,width,rendererHeight));y+=rendererHeight+8;
            UiSectionLabel(ref y,width,"Asset rules");
            DrawRulesHelp(ref y,width,"Preserve every renderer that uses this sprite, texture or material.");
            DrawAssetPreserveControls(entry,new Rect(0,y,width,assetHeight));y+=assetHeight+8;
            UiSectionLabel(ref y,width,"Path rules");
            DrawRulesHelp(ref y,width,"Preserve objects by hierarchy path.");
            DrawPathPreserveControls(entry,new Rect(0,y,width,pathHeight));y+=pathHeight+8;
            UiLabel(new Rect(0,y,width,infoHeight),info,browserInfoWrappedStyle);
            GUI.EndScrollView();
        }

        private float RulesHelpHeight(float width, string text)
        {
            if (browserInfoWrappedStyle == null) browserInfoWrappedStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            return browserInfoWrappedStyle.CalcHeight(new GUIContent(UiText(text)), Mathf.Max(1f, width));
        }

        private void DrawRulesHelp(ref float y, float width, string text)
        {
            string localized = UiText(text);
            float height = RulesHelpHeight(width, text);
            UiLabel(new Rect(0f, y, width, height), localized, browserInfoWrappedStyle);
            y += height + 8f;
        }

        private void DrawTextureBrowserExportTab(TextureBrowserEntry entry, Rect content)
        {
            UiBox(content, "");
            Rect inner = new Rect(content.x + 10f, content.y + 10f, content.width - 20f, content.height - 20f);
            if (UiButton(new Rect(inner.x, inner.y, 140f, 24f), "Export PNG"))
            {
                ExportTextureBrowserEntry(entry, false);
            }

            if (UiButton(new Rect(inner.x + 148f, inner.y, 146f, 24f), "Export Atlas"))
            {
                ExportTextureBrowserEntry(entry, true);
            }

            DrawTextureExportControls(new Rect(inner.x, inner.y + 38f, inner.width, 24f));
            Rect infoRect = new Rect(inner.x, inner.y + 76f, inner.width, inner.height - 76f);
            string info = WrapBrowserInfo(BuildTextureBrowserInfo(entry));
            float infoContentHeight = Mathf.Max(infoRect.height, browserInfoWrappedStyle.CalcHeight(new GUIContent(info), infoRect.width - 20f) + 8f);
            Rect contentRect = new Rect(0f, 0f, infoRect.width - 20f, infoContentHeight);
            textureBrowserDetailScroll = GUI.BeginScrollView(infoRect, textureBrowserDetailScroll, contentRect);
            UiLabel(new Rect(0f, 0f, contentRect.width, contentRect.height), info,browserInfoWrappedStyle);
            GUI.EndScrollView();
        }

        private GUIStyle backgroundSettingsLabelStyle;
        private GUIStyle backgroundSettingsButtonStyle;
        private GUIStyle backgroundSettingsToggleStyle;

        private void DrawTextureBrowserSettingsTab(Rect content)
        {
            RefreshBackgroundImageFiles();
            if (backgroundSettingsLabelStyle == null) backgroundSettingsLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            if (backgroundSettingsButtonStyle == null) backgroundSettingsButtonStyle = new GUIStyle(GUI.skin.button) { wordWrap = true };
            UiBox(content, "");
            Rect viewport = new Rect(content.x + 10f, content.y + 10f, content.width - 20f, content.height - 20f);
            float width = Mathf.Max(100f, viewport.width - 20f);
            float height = Mathf.Max(viewport.height,backgroundSettingsContentHeight);
            Rect view = new Rect(0, 0, width, Mathf.Max(viewport.height, height));
            textureBrowserDetailScroll = GUI.BeginScrollView(viewport, textureBrowserDetailScroll, view);
            float y = 0f;
            DrawBackgroundSettingsLabel(ref y,width,"Settings are applied immediately and saved automatically.");
            if(!string.IsNullOrEmpty(backgroundImageMessage))DrawBackgroundSettingsLabel(ref y,width,"Status: "+backgroundImageMessage);
            if(!string.IsNullOrEmpty(lastSettingsMessage))DrawBackgroundSettingsLabel(ref y,width,"Status: "+lastSettingsMessage);
            UiSectionLabel(ref y,width,"Capture status");
            DrawBackgroundSettingsLabel(ref y,width,BuildCaptureToggleStatusText());
            DrawBackgroundSettingsLabel(ref y,width,BuildCameraStatusText());
            DrawBackgroundSettingsLabel(ref y,width,BuildPreserveScanStatusText());
            string captureHint=BuildPreserveScanHint();
            if(!string.IsNullOrEmpty(captureHint))DrawBackgroundSettingsLabel(ref y,width,captureHint);
            if(compositionEnabled)DrawBackgroundSettingsLabel(ref y,width,DescribeCompositionState());
            DrawCapturePresetPanel(ref y,width);
            UiSectionLabel(ref y,width,"Interface language");
            float languageWidth=(width-6)/2;
            bool english=DrawModeButton(new Rect(0,y,languageWidth,28),"English",uiLanguage=="English");
            bool russian=DrawModeButton(new Rect(languageWidth+6,y,languageWidth,28),"Русский",uiLanguage=="Russian");y+=36;
            UiSectionLabel(ref y,width,"Visuals and camera");
            bool gameplayToggle=DrawWrappedToggle(ref y,width,gameplayIsolationEnabled,HotkeyText("Gameplay isolation{keySuffix}",HotkeyActions.ToggleGameplayIsolation));
            bool menuToggle=DrawWrappedToggle(ref y,width,menuDesignEnabled,"Title menu design");
            DrawBackgroundSettingsLabel(ref y,width,"Gameplay isolation hides enemies and effects with the strip shader. Title menu design controls the custom title background and logo.");
            DrawBackgroundSettingsLabel(ref y,width,HotkeyText("Gameplay camera{keySuffix}",HotkeyActions.ToggleCameraPin));
            DrawBackgroundSettingsLabel(ref y,width,BuildCameraStatusText());
            bool previousEnabled=GUI.enabled;
            GUI.enabled=previousEnabled&&(cameraPin.Active||GetPinnableCamera()!=null);
            bool toggleCamera=UiButton(new Rect(0,y,width,30),cameraPin.Active ? HotkeyText("Release camera{keySuffix}",HotkeyActions.ToggleCameraPin) : HotkeyText("Pin camera here{keySuffix}",HotkeyActions.ToggleCameraPin),backgroundSettingsButtonStyle);
            GUI.enabled=previousEnabled;y+=36;
            DrawBackgroundSettingsLabel(ref y,width,"Pin the current view; the hero can move independently. Released automatically on scene changes, death and cutscenes. Pinning is not saved.");
            if(!cameraPin.Active&&GetPinnableCamera()==null)DrawBackgroundSettingsLabel(ref y,width,"Enter a room to pin the camera. Transitions and cutscenes must finish first.");
            bool pauseRequested=DrawWrappedToggle(ref y,width,pauseGameWhileBrowserOpen,HotkeyText("Pause time and sound while the browser is open{keySuffix}",HotkeyActions.ToggleBrowser));
            DrawBackgroundSettingsLabel(ref y,width,"Menu design controls the title screen background, logo and buttons.");
            UiSectionLabel(ref y, width, "Screen background color and image");
            DrawBackgroundSettingsLabel(ref y, width, "The background fills the whole screen behind the game world. The green color is used by gameplay isolation.");
            DrawBackgroundColorControls(new Rect(0, y, width, 94));
            y += 108f;
            DrawBackgroundSettingsLabel(ref y, width, IsImageBackgroundEnabled() ? "Image: " + backgroundImageRelativePath : "Color: " + backgroundColorName);
            bool chooseImage = UiButton(new Rect(0, y, Mathf.Min(180f, width), 28), "Choose image...");
            y += 36f;
            DrawBackgroundSettingsLabel(ref y, width, "Image placement");
            DrawBackgroundLayoutChoice(ref y, width, "Fill screen (crop edges)", BackgroundImageLayoutCover);
            DrawBackgroundLayoutChoice(ref y, width, "Fit entire image (keep proportions)", BackgroundImageLayoutContain);
            DrawBackgroundLayoutChoice(ref y, width, "Stretch to screen", BackgroundImageLayoutStretch);
            y += 6f;
            UiSectionLabel(ref y, width, "Imported images");
            string selectImage = null;
            string removeImage = null;
            if (backgroundImageFiles.Count == 0) DrawBackgroundSettingsLabel(ref y, width, "No imported images. Use Choose image... to select a file.");
            for (int i = 0; i < backgroundImageFiles.Count; i++)
            {
                string name = backgroundImageFiles[i];
                float buttonWidth = Mathf.Max(40f, width - 90f);
                float rowHeight = Mathf.Max(26f, backgroundSettingsButtonStyle.CalcHeight(new GUIContent(name), buttonWidth));
                Color previous = GUI.backgroundColor;
                if (IsImageBackgroundEnabled() && string.Equals(name, backgroundImageRelativePath, System.StringComparison.OrdinalIgnoreCase)) GUI.backgroundColor = Color.green;
                if (UiButton(new Rect(0, y, buttonWidth, rowHeight), name, backgroundSettingsButtonStyle)) selectImage = name;
                GUI.backgroundColor = previous;
                if (UiDangerButton(new Rect(buttonWidth + 6f, y, 84f, rowHeight), "Remove")) removeImage = name;
                y += rowHeight + 6f;
            }
            DrawBackgroundSettingsLabel(ref y, width, "Remove clears an image from the list. A recoverable copy stays in background-images/removed.");
            RefreshBackgroundTrashFiles();
            UiSectionLabel(ref y, width, "Removed images (trash)");
            DrawBackgroundSettingsLabel(ref y, width, backgroundTrashFiles.Count == 0
                ? "Image trash is empty."
                : "Trash occupies " + MediaTrashPolicy.FormatSize(backgroundTrashBytes) + " in " + backgroundTrashFiles.Count + " file(s). Restore never overwrites an existing image.");
            string restoreTrashImage = null;
            string requestDeleteTrashImage = null;
            string deleteTrashImage = null;
            bool cancelDeleteTrashImage = false;
            bool emptyTrashImages = false;
            for (int i = 0; i < backgroundTrashFiles.Count; i++)
            {
                RemovedMediaFile item = backgroundTrashFiles[i];
                DrawBackgroundSettingsLabel(ref y, width, item.DisplayName + "  (" + MediaTrashPolicy.FormatSize(item.Size) + ")");
                float trashHalf = Mathf.Max(40f, (width - 6f) / 2f);
                if (string.Equals(pendingDeleteBackgroundTrash, item.Path, System.StringComparison.OrdinalIgnoreCase))
                {
                    DrawBackgroundSettingsLabel(ref y, width, "Click Delete permanently again to confirm. This cannot be undone.");
                    if (UiDangerButton(new Rect(0, y, trashHalf, 28f), "Delete permanently")) deleteTrashImage = item.Path;
                    if (UiButton(new Rect(trashHalf + 6f, y, trashHalf, 28f), "Cancel")) cancelDeleteTrashImage = true;
                    y += 36f;
                    continue;
                }

                if (UiButton(new Rect(0, y, trashHalf, 28f), "Restore")) restoreTrashImage = item.Path;
                if (UiDangerButton(new Rect(trashHalf + 6f, y, trashHalf, 28f), "Delete permanently")) requestDeleteTrashImage = item.Path;
                y += 36f;
            }

            if (backgroundTrashFiles.Count > 0)
            {
                if (!confirmEmptyBackgroundTrash)
                {
                    if (UiDangerButton(new Rect(0, y, Mathf.Min(180f, width), 28f), "Empty image trash..."))
                    {
                        confirmEmptyBackgroundTrash = true;
                        backgroundImageMessage = TrashEmptyPromptMessage(backgroundTrashFiles.Count);
                    }

                    y += 36f;
                }
                else
                {
                    DrawBackgroundSettingsLabel(ref y, width, TrashDeleteConfirmMessage(backgroundTrashFiles.Count, backgroundTrashBytes));
                    float confirmHalf = Mathf.Max(40f, (width - 6f) / 2f);
                    if (UiDangerButton(new Rect(0, y, confirmHalf, 28f), "Empty trash")) emptyTrashImages = true;
                    if (UiButton(new Rect(confirmHalf + 6f, y, confirmHalf, 28f), "Cancel")) confirmEmptyBackgroundTrash = false;
                    y += 36f;
                }
            }

            UiSectionLabel(ref y, width, "Preserved objects relative to the hero");
            bool orderBehind = DrawModeButton(new Rect(0, y, width, 26), "Behind hero", preservedObjectOrder == "BehindHero");
            y += 30f;
            bool orderFront = DrawModeButton(new Rect(0, y, width, 26), "In front of hero", preservedObjectOrder == "InFront");
            y += 34f;
            GUI.EndScrollView();
            backgroundSettingsContentHeight=y+16;
            if(english) SetUiLanguage("English");
            if(russian) SetUiLanguage("Russian");
            if(gameplayToggle!=gameplayIsolationEnabled) SetGameplayIsolationEnabled(gameplayToggle);
            if(menuToggle!=menuDesignEnabled) SetMenuDesignEnabled(menuToggle);
            if(toggleCamera)ToggleCameraPin();
            // Keep the list stable until the current IMGUI pass has finished.
            if (pauseRequested != pauseGameWhileBrowserOpen) SetPauseGameWhileBrowserOpen(pauseRequested);
            if (selectImage != null) SetBackgroundImage(selectImage);
            if (removeImage != null) RemoveBackgroundImage(removeImage);
            if (restoreTrashImage != null) RestoreBackgroundTrashFile(restoreTrashImage);
            if (cancelDeleteTrashImage) pendingDeleteBackgroundTrash = null;
            else if (requestDeleteTrashImage != null) pendingDeleteBackgroundTrash = requestDeleteTrashImage;
            else if (deleteTrashImage != null) DeleteBackgroundTrashFile(deleteTrashImage);
            if (emptyTrashImages) EmptyBackgroundTrash();
            if (chooseImage) ChooseBackgroundImage();
            if (orderBehind) SetPreservedObjectOrder("BehindHero");
            if (orderFront) SetPreservedObjectOrder("InFront");
        }

        private string BuildCaptureToggleStatusText()
        {
            return UiText("Gameplay isolation: {isolation}. Title menu design: {menu}.")
                .Replace("{isolation}",UiText(gameplayIsolationEnabled ? "On" : "Off"))
                .Replace("{menu}",UiText(menuDesignEnabled ? "On" : "Off"));
        }

        private string BuildCameraStatusText()
        {
            string cameraStatus=!string.IsNullOrEmpty(cameraPinMessage) ? cameraPinMessage : cameraPin.Active ? "Camera pinned in this room." : "Camera follows the hero.";
            if(cameraPin.Active)cameraStatus=cameraStatus+" "+HotkeyText("Press {key} again to release.",HotkeyActions.ToggleCameraPin);
            return cameraStatus;
        }

        private void DrawBackgroundLayoutChoice(ref float y, float width, string text, string layout)
        {
            text=UiText(text);float height = Mathf.Max(26f, backgroundSettingsButtonStyle.CalcHeight(new GUIContent(text), width));
            Color previous = GUI.backgroundColor;
            if (backgroundImageLayout == layout) GUI.backgroundColor = Color.green;
            bool selected = UiButton(new Rect(0, y, width, height), text, backgroundSettingsButtonStyle);
            GUI.backgroundColor = previous;
            y += height + 4f;
            if (selected) SetBackgroundImageLayout(layout);
        }
        private void DrawBackgroundSettingsLabel(ref float y, float width, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            text=UiText(text);
            float height = backgroundSettingsLabelStyle.CalcHeight(new GUIContent(text), width);
            UiLabel(new Rect(0, y, width, height), text, backgroundSettingsLabelStyle);
            y += height + 8f;
        }
        private bool DrawWrappedToggle(ref float y,float width,bool value,string text)
        {
            string translated=UiText(text);float height=Mathf.Max(26,backgroundSettingsLabelStyle.CalcHeight(new GUIContent(translated),Mathf.Max(1,width-28)));
            if(backgroundSettingsToggleStyle==null)backgroundSettingsToggleStyle=new GUIStyle(GUI.skin.toggle){wordWrap=true};
            bool result=GUI.Toggle(new Rect(0,y,width,height),value,new GUIContent(translated),backgroundSettingsToggleStyle);y+=height+8;return result;
        }
        private void DrawBackgroundColorControls(Rect rect)
        {
            float gap = 8f;
            float rowHeight = 26f;
            float columnWidth = Mathf.Max(1f, (rect.width - gap * 2f) / 3f);
            for (int i = 0; i < BackgroundColorNames.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                Rect buttonRect = new Rect(rect.x + column * (columnWidth + gap), rect.y + row * (rowHeight + gap), columnWidth, rowHeight);
                DrawBackgroundColorButton(buttonRect, BackgroundColorNames[i]);
            }
        }

        private void DrawBackgroundColorButton(Rect rect, string colorName)
        {
            string normalized = NormalizeBackgroundColorName(colorName);
            Rect swatchRect = new Rect(rect.x, rect.y + 2f, 22f, rect.height - 4f);
            DrawColorSwatch(swatchRect, GetStripBackgroundColor(normalized));
            Rect buttonRect = new Rect(rect.x + 28f, rect.y, rect.width - 28f, rect.height);
            bool active = !IsImageBackgroundEnabled() && string.Equals(backgroundColorName, normalized, System.StringComparison.OrdinalIgnoreCase);
            if (DrawModeButton(buttonRect, normalized, active))
            {
                SetStripBackgroundColor(normalized);
            }
        }

        private void DrawColorSwatch(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
            UiBox(rect, "");
        }

        private void DrawTextureBrowserPerformanceTab(Rect content)
        {
            UiBox(content, "");
            Rect viewport = new Rect(content.x + 10f, content.y + 10f, content.width - 20f, content.height - 20f);
            Rect view = new Rect(0f, 0f, Mathf.Max(300f, viewport.width - 20f), Mathf.Max(viewport.height, 2130f));
            textureBrowserDetailScroll = GUI.BeginScrollView(viewport, textureBrowserDetailScroll, view);

            Rect inner = new Rect(0f, 0f, view.width, view.height);
            bool custom = string.Equals(performanceProfile, PerformanceProfileCustom, System.StringComparison.OrdinalIgnoreCase);
            float y = inner.y;
            UiLabel(new Rect(inner.x, y, inner.width, 22f), "Choose a profile. Custom unlocks the values below.");
            y += 28f;
            DrawPerformanceProfileControls(new Rect(inner.x, y, inner.width, 202f));
            y += 210f;

            DrawPerformanceRuntimeControls(new Rect(inner.x, y, inner.width, 330f), custom);
            y += 338f;

            DrawProjectileCaptureControls(new Rect(inner.x, y, inner.width, 152f), custom);
            y += 160f;

            DrawProjectilePerformanceControls(new Rect(inner.x, y, inner.width, 414f));
            y += 422f;

            DrawPerformanceCacheControls(new Rect(inner.x, y, inner.width, 262f), custom);
            y += 270f;

            DrawPerformanceCounterControls(new Rect(inner.x, y, inner.width, 196f), custom);
            y += 204f;

            DrawPerformanceCounters(new Rect(inner.x, y, inner.width, 340f));
            y += 348f;

            if (!string.IsNullOrEmpty(lastPerformanceMessage))
            {
                UiLabel(new Rect(inner.x, y, inner.width, 24f), lastPerformanceMessage);
            }

            GUI.EndScrollView();
        }

        private void DrawPerformanceProfileControls(Rect rect)
        {
            UiLabel(new Rect(rect.x, rect.y, rect.width, 22f), "Profile");
            float gap = 6f;
            float buttonHeight = 24f;
            int columns = rect.width < 380f ? 2 : 5;
            float modeWidth = Mathf.Max(70f, (rect.width - gap * (columns - 1)) / columns);
            DrawPerformanceProfileButtonAt(rect, 0, columns, modeWidth, buttonHeight, gap, PerformanceProfileQuality);
            DrawPerformanceProfileButtonAt(rect, 1, columns, modeWidth, buttonHeight, gap, PerformanceProfileBalanced);
            DrawPerformanceProfileButtonAt(rect, 2, columns, modeWidth, buttonHeight, gap, PerformanceProfilePerformance);
            DrawPerformanceProfileButtonAt(rect, 3, columns, modeWidth, buttonHeight, gap, PerformanceProfileUltra);
            DrawPerformanceProfileButtonAt(rect, 4, columns, modeWidth, buttonHeight, gap, PerformanceProfileCustom);

            float actionsY = columns == 2 ? rect.y + 92f : rect.y + 62f;
            float actionWidth = Mathf.Max(44f, (rect.width - gap * 2f) / 3f);
            if (UiButton(new Rect(rect.x, actionsY, actionWidth, 24f), "Reset"))
            {
                ResetPerformanceToBalanced();
            }

            if (UiButton(new Rect(rect.x + actionWidth + gap, actionsY, actionWidth, 24f), "Q->Custom"))
            {
                CopyPerformanceProfileToCustom(PerformanceProfileQuality);
            }

            if (UiButton(new Rect(rect.x + (actionWidth + gap) * 2f, actionsY, actionWidth, 24f), "B->Custom"))
            {
                CopyPerformanceProfileToCustom(PerformanceProfileBalanced);
            }

            float actionsY2 = actionsY + 30f;
            if (UiButton(new Rect(rect.x, actionsY2, actionWidth, 24f), "P->Custom"))
            {
                CopyPerformanceProfileToCustom(PerformanceProfilePerformance);
            }

            if (UiButton(new Rect(rect.x + actionWidth + gap, actionsY2, actionWidth, 24f), "U->Custom"))
            {
                CopyPerformanceProfileToCustom(PerformanceProfileUltra);
            }

            UiLabel(new Rect(rect.x, actionsY2 + 32f, rect.width, 20f), "Profile: " + performanceProfile + " | scheduler: " + (staggerPerformanceTasks ? "on" : "off"));
            UiLabel(new Rect(rect.x, actionsY2 + 54f, rect.width, 20f), "Projectile capture: " + NormalizeProjectileCaptureMode(projectileCaptureMode));
        }

        private void DrawPerformanceProfileButtonAt(Rect rect, int index, int columns, float width, float height, float gap, string profile)
        {
            int column = index % columns;
            int row = index / columns;
            Rect buttonRect = new Rect(rect.x + column * (width + gap), rect.y + 28f + row * (height + gap), width, height);
            DrawPerformanceProfileButton(buttonRect, profile);
        }

        private void DrawPerformanceProfileButton(Rect rect, string profile)
        {
            string normalized = NormalizePerformanceProfile(profile);
            bool active = string.Equals(performanceProfile, normalized, System.StringComparison.OrdinalIgnoreCase);
            string label = string.Equals(normalized, PerformanceProfilePerformance, System.StringComparison.OrdinalIgnoreCase) ? "Perf" : normalized;
            if (DrawModeButton(rect, label, active))
            {
                SetPerformanceProfile(normalized);
            }
        }

        private void DrawPerformanceRuntimeControls(Rect rect, bool editable)
        {
            UiLabel(new Rect(rect.x, rect.y, rect.width, 22f), editable ? "Core Runtime - Custom values" : "Core Runtime - values from " + performanceProfile);
            float y = rect.y + 28f;
            DrawPerformanceInput(new Rect(rect.x, y, rect.width, 24f), "ApplyStrip interval", ref applyStripIntervalInput, "seconds", editable);
            y += 28f;
            DrawPerformanceInput(new Rect(rect.x, y, rect.width, 24f), "Proxy update interval", ref proxyUpdateIntervalInput, "seconds", editable);
            y += 28f;
            DrawPerformanceInput(new Rect(rect.x, y, rect.width, 24f), "Browser refresh", ref textureBrowserRefreshIntervalInput, "seconds", editable);
            y += 28f;
            DrawPerformanceInput(new Rect(rect.x, y, rect.width, 24f), "Frame budget", ref heavyWorkBudgetInput, "ms", editable);
            y += 28f;
            DrawPerformanceInput(new Rect(rect.x, y, rect.width, 24f), "GC budget", ref gcBudgetInput, "KB/frame", editable);
            y += 28f;
            DrawPerformanceInput(new Rect(rect.x, y, rect.width, 24f), "Visible entries", ref maxVisibleBrowserEntriesInput, "", editable);
            y += 28f;
            DrawPerformanceInput(new Rect(rect.x, y, rect.width, 24f), "List overscan rows", ref browserListOverscanRowsInput, "", editable);

            y += 34f;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = editable;
            if (UiButton(new Rect(rect.x, y, 112f, 24f), "Apply Custom"))
            {
                ApplyCustomPerformanceSettings();
            }

            GUI.enabled = previousEnabled;
            y += 30f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "0 interval = every frame.");
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Hard caps are applied to risky values.");
        }

        private void DrawProjectileCaptureControls(Rect rect, bool editable)
        {
            UiLabel(new Rect(rect.x, rect.y, rect.width, 22f), "Projectile Capture");
            float gap = 6f;
            float y = rect.y + 28f;
            int columns = rect.width < 360f ? 2 : 4;
            float width = Mathf.Max(72f, (rect.width - gap * (columns - 1)) / columns);
            DrawProjectileCaptureModeButtonAt(rect, 0, columns, width, 24f, gap, ProjectileCaptureOnlyProjectilesTab, "Only Tab", editable);
            DrawProjectileCaptureModeButtonAt(rect, 1, columns, width, 24f, gap, ProjectileCaptureBrowserOpen, "Browser", editable);
            DrawProjectileCaptureModeButtonAt(rect, 2, columns, width, 24f, gap, ProjectileCaptureAlways, "Always", editable);
            DrawProjectileCaptureModeButtonAt(rect, 3, columns, width, 24f, gap, ProjectileCaptureManual, "Manual", editable);

            y += columns == 2 ? 64f : 34f;
            if (UiButton(new Rect(rect.x, y, 112f, 24f), "Capture Now"))
            {
                CaptureRecentProjectileRenderers(true);
                RefreshRecentProjectileBrowserEntries();
                lastPerformanceMessage = "Projectile capture refreshed.";
            }

            DrawPerformanceToggle(new Rect(rect.x, y + 30f, rect.width, 24f), "Use unscaled time", ref projectileUseUnscaledTime, editable);
        }

        private void DrawProjectileCaptureModeButtonAt(Rect rect, int index, int columns, float width, float height, float gap, string mode, string label, bool editable)
        {
            int column = index % columns;
            int row = index / columns;
            Rect buttonRect = new Rect(rect.x + column * (width + gap), rect.y + 28f + row * (height + gap), width, height);
            DrawProjectileCaptureModeButton(buttonRect, mode, label, editable);
        }

        private void DrawProjectileCaptureModeButton(Rect rect, string mode, string label, bool editable)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = editable;
            bool active = string.Equals(projectileCaptureMode, mode, System.StringComparison.OrdinalIgnoreCase);
            if (DrawModeButton(rect, label, active))
            {
                SetPerformanceDirtyFromToggle();
                projectileCaptureMode = NormalizeProjectileCaptureMode(mode);
                SaveExportSettings();
                lastPerformanceMessage = "Projectile capture mode set to " + projectileCaptureMode + ".";
            }

            GUI.enabled = previousEnabled;
        }

        private void DrawPerformanceCacheControls(Rect rect, bool editable)
        {
            UiLabel(new Rect(rect.x, rect.y, rect.width, 22f), "Caching");
            float y = rect.y + 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Renderer path cache", ref rendererPathCacheEnabled, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Preserve cache flag", ref preserveMatchCacheEnabled, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Projectile cache flag", ref projectileCandidateCacheEnabled, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Browser snapshot flag", ref browserSnapshotCacheEnabled, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Draw source cache", ref drawSourceCacheEnabled, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Virtualized list", ref virtualizedBrowserList, editable);
            y += 28f;
            DrawPerformanceInput(new Rect(rect.x, y, rect.width, 24f), "Max path cache", ref maxCachedRendererPathsInput, "", editable);
            y += 30f;
            if (UiButton(new Rect(rect.x, y, Mathf.Min(120f, rect.width), 24f), "Clear Caches"))
            {
                ClearRuntimeCaches();
                lastPerformanceMessage = "Runtime caches cleared.";
            }
        }

        private void DrawPerformanceCounterControls(Rect rect, bool editable)
        {
            UiLabel(new Rect(rect.x, rect.y, rect.width, 22f), "Diagnostics");
            float y = rect.y + 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Show counters", ref showPerformanceCounters, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Show cache stats", ref showCacheStats, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Detailed metrics", ref collectDetailedMetrics, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "GC heap delta", ref showGcAllocStats, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Stagger scheduler", ref staggerPerformanceTasks, editable);
            y += 28f;
            DrawPerformanceToggle(new Rect(rect.x, y, rect.width, 24f), "Scene warmup flag", ref cacheWarmupAfterSceneChange, editable);
        }

        private void DrawPerformanceCounters(Rect rect)
        {
            if (!showPerformanceCounters && !showCacheStats)
            {
                UiLabel(new Rect(rect.x, rect.y, rect.width, 22f), "Counters disabled.");
                return;
            }

            UiLabel(new Rect(rect.x, rect.y, rect.width, 22f), "Status: " + BuildPreserveScanStatusShort());
            float y = rect.y + 26f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Current cost");
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "ApplyStrip: " + FormatMetric(lastApplyStripMs, lastApplyStripGcBytes));
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Draw sync: " + FormatMetric(lastDrawSyncMs, lastDrawSyncGcBytes));
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Projectile scan: " + FormatMetric(lastProjectileScanMs, lastProjectileScanGcBytes));
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Proxy update: " + FormatMetric(lastProxyUpdateMs, lastProxyUpdateGcBytes));
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Draw sources: " + preservedRendererDrawSources.Count + " / " + maxPreservedRenderers + " | proxies: " + preservedRendererProxies.Count);
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Preserve scan: matched " + preserveRunStats.CandidateMatched + " | drawn " + preserveRunStats.Drawn + " | blocked " + preserveRunStats.Stale);
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Limit skips: draw " + preserveRunStats.SkippedByDrawLimit + " / " + maxPreservedRenderers + " | particle " + preserveRunStats.SkippedByParticleLimit + " / " + maxProjectileParticleRenderers);
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Skip reasons: disabled " + preserveRunStats.SkippedDisabled + " | inactive " + preserveRunStats.SkippedInactive + " | visible " + preserveRunStats.SkippedAlreadyVisible);
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "            particle " + preserveRunStats.SkippedExpensiveParticle + " | scene " + preserveRunStats.SkippedOtherScene + " | no rule " + preserveRunStats.SkippedNoRule + " | no mat " + preserveRunStats.SkippedNoMaterials);
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Deferred tasks: " + performanceTasksDeferred);
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Path cache: " + objectPathCache.Count + " / " + maxCachedRendererPaths);
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Hits: " + objectPathCacheHits + " | misses: " + objectPathCacheMisses + " | evicted: " + objectPathCacheEvictions);
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Browser entries: " + textureBrowserEntries.Count + " | cap: " + maxVisibleBrowserEntries);
            y += 22f;
            UiLabel(new Rect(rect.x, y, rect.width, 20f), "Virtualized list: " + FormatBool(virtualizedBrowserList));
        }

        private string FormatMetric(double ms, long gcBytes)
        {
            if (!showGcAllocStats)
            {
                return ms.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " ms";
            }

            double kb = gcBytes / 1024.0;
            return ms.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " ms / " + kb.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " KB";
        }

        private void DrawPerformanceToggle(Rect rect, string label, ref bool value, bool editable)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = editable;
            bool next = UiToggle(rect, value, label);
            GUI.enabled = previousEnabled;
            if (next != value)
            {
                value = next;
                SetPerformanceDirtyFromToggle();
                SaveExportSettings();
                if (label.IndexOf("cache", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ClearRuntimeCaches();
                }
            }
        }

        private void DrawPerformanceInput(Rect rect, string label, ref string value, string suffix, bool editable)
        {
            bool hasSuffix = !string.IsNullOrEmpty(suffix);
            float labelWidth = Mathf.Clamp(rect.width * 0.45f, 80f, 154f);
            float inputX = rect.x + labelWidth + 6f;
            float suffixReserve = hasSuffix ? 70f : 6f;
            float inputWidth = Mathf.Clamp(rect.width - labelWidth - suffixReserve - 12f, 48f, 78f);
            inputWidth = Mathf.Min(inputWidth, Mathf.Max(32f, rect.xMax - inputX));
            UiLabel(new Rect(rect.x, rect.y + 2f, labelWidth, rect.height), label);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = editable;
            value = GUI.TextField(new Rect(inputX, rect.y, inputWidth, rect.height), value);
            GUI.enabled = previousEnabled;
            if (hasSuffix)
            {
                float suffixX = inputX + inputWidth + 6f;
                if (suffixX < rect.xMax)
                {
                    UiLabel(new Rect(suffixX, rect.y + 2f, rect.xMax - suffixX, rect.height), suffix);
                }
            }
        }

        private void DrawProjectilePerformanceControls(Rect rect)
        {
            UiLabel(new Rect(rect.x, rect.y, rect.width, 24f), "Projectile Performance");

            float gap = 6f;
            float modeY = rect.y + 30f;
            float buttonHeight = 24f;
            int columns = rect.width < 360f ? 2 : 4;
            float modeWidth = Mathf.Max(74f, (rect.width - gap * (columns - 1)) / columns);
            DrawProjectilePerformanceModeButtonAt(rect, 0, columns, modeWidth, buttonHeight, gap, ProjectilePerformanceSafe);
            DrawProjectilePerformanceModeButtonAt(rect, 1, columns, modeWidth, buttonHeight, gap, ProjectilePerformanceBalanced);
            DrawProjectilePerformanceModeButtonAt(rect, 2, columns, modeWidth, buttonHeight, gap, ProjectilePerformanceAggressive);
            DrawProjectilePerformanceModeButtonAt(rect, 3, columns, modeWidth, buttonHeight, gap, ProjectilePerformanceCustom);

            bool custom = string.Equals(performanceProfile, PerformanceProfileCustom, System.StringComparison.OrdinalIgnoreCase);
            float fieldY = modeY + (columns == 2 ? 66f : 36f);
            UiLabel(new Rect(rect.x, fieldY, rect.width, 22f), custom ? "Custom values 0..999" : "Values from " + performanceProfile + " profile");
            fieldY += 26f;
            DrawProjectilePerformanceInput(new Rect(rect.x, fieldY, rect.width, 24f), "Capture interval", ref projectileCaptureIntervalInput, "seconds", custom);
            fieldY += 28f;
            DrawProjectilePerformanceInput(new Rect(rect.x, fieldY, rect.width, 24f), "Recent lifetime", ref recentProjectileLifetimeInput, "seconds", custom);
            fieldY += 28f;
            DrawProjectilePerformanceInput(new Rect(rect.x, fieldY, rect.width, 24f), "Preserved sync", ref preservedSyncIntervalInput, "seconds", custom);
            fieldY += 28f;
            DrawProjectilePerformanceInput(new Rect(rect.x, fieldY, rect.width, 24f), "Max renderers", ref maxPreservedRenderersInput, "", custom);
            fieldY += 28f;
            DrawProjectilePerformanceInput(new Rect(rect.x, fieldY, rect.width, 24f), "Max particle renderers", ref maxProjectileParticleRenderersInput, "", custom);

            fieldY += 34f;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = custom;
            float actionWidth = Mathf.Min(112f, rect.width);
            if (UiButton(new Rect(rect.x, fieldY, actionWidth, 24f), "Apply Custom"))
            {
                ApplyCustomProjectilePerformanceSettings();
            }

            GUI.enabled = previousEnabled;
            if (rect.width >= 250f)
            {
                if (UiButton(new Rect(rect.x + 120f, fieldY, 116f, 24f), "Reset Defaults"))
                {
                    ResetProjectilePerformanceDefaults();
                }
            }
            else
            {
                fieldY += 30f;
                if (UiButton(new Rect(rect.x, fieldY, actionWidth, 24f), "Reset Defaults"))
                {
                    ResetProjectilePerformanceDefaults();
                }
            }

            fieldY += 30f;
            UiLabel(new Rect(rect.x, fieldY, rect.width, 22f), "Current: " + projectilePerformanceMode +
                " | capture " + FormatSettingsFloat(projectileCaptureInterval) +
                "s | lifetime " + FormatSettingsFloat(recentProjectileLifetime) + "s");
            fieldY += 24f;
            UiLabel(new Rect(rect.x, fieldY, rect.width, 22f), "Sync " + FormatSettingsFloat(preservedSyncIntervalSetting) +
                "s | renderers " + maxPreservedRenderers +
                " | particles " + maxProjectileParticleRenderers);
            fieldY += 24f;
            UiLabel(new Rect(rect.x, fieldY, rect.width, 22f), "0 intervals = every frame. High limits can cause severe lag.");
        }

        private void DrawProjectilePerformanceModeButtonAt(Rect rect, int index, int columns, float width, float height, float gap, string mode)
        {
            int column = index % columns;
            int row = index / columns;
            Rect buttonRect = new Rect(rect.x + column * (width + gap), rect.y + 30f + row * (height + gap), width, height);
            DrawProjectilePerformanceModeButton(buttonRect, mode);
        }

        private void DrawProjectilePerformanceModeButton(Rect rect, string mode)
        {
            string normalized = NormalizeProjectilePerformanceMode(mode);
            bool active = string.Equals(projectilePerformanceMode, normalized, System.StringComparison.OrdinalIgnoreCase);
            if (DrawModeButton(rect, normalized, active))
            {
                SetProjectilePerformanceMode(normalized);
            }
        }

        private void DrawProjectilePerformanceInput(Rect rect, string label, ref string value, string suffix, bool editable)
        {
            bool hasSuffix = !string.IsNullOrEmpty(suffix);
            float labelWidth = Mathf.Clamp(rect.width * 0.45f, 80f, 154f);
            float inputX = rect.x + labelWidth + 6f;
            float suffixReserve = hasSuffix ? 70f : 6f;
            float inputWidth = Mathf.Clamp(rect.width - labelWidth - suffixReserve - 12f, 48f, 78f);
            inputWidth = Mathf.Min(inputWidth, Mathf.Max(32f, rect.xMax - inputX));
            UiLabel(new Rect(rect.x, rect.y + 2f, labelWidth, rect.height), label);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = editable;
            value = GUI.TextField(new Rect(inputX, rect.y, inputWidth, rect.height), value);
            GUI.enabled = previousEnabled;
            if (hasSuffix)
            {
                float suffixX = inputX + inputWidth + 6f;
                if (suffixX < rect.xMax)
                {
                    UiLabel(new Rect(suffixX, rect.y + 2f, rect.xMax - suffixX, rect.height), suffix);
                }
            }
        }

    }
}





