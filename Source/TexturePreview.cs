using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private void DrawTexturePreview(TextureBrowserEntry entry, Rect previewOuter)
        {
            Rect previewRect = new Rect(previewOuter.x + 10f, previewOuter.y + 10f, previewOuter.width - 20f, previewOuter.height - 20f);
            HandleTexturePreviewInput(previewRect);
            GUI.BeginGroup(previewRect);
            try
            {
                // All zoomed and panned artwork stays inside its checkerboard viewport.
                previewRect = new Rect(0, 0, previewRect.width, previewRect.height);
                DrawPreviewBackground(previewRect);
                if(entry.MenuResource&&!menuShowAtlas)
                {
                    var artwork=GetMenuObjectPreview(entry);
                    if(artwork!=null){DrawTextureSafe(FitPreviewRect(previewRect,artwork.width,artwork.height),artwork);return;}
                }

                if (entry.Sprite != null && entry.Sprite.texture != null && (!entry.MenuResource||!menuShowAtlas))
                {
                    Rect texCoords;
                    string note;
                    if (TryGetSpriteTexCoords(entry.Sprite, out texCoords, out note))
                    {
                        Rect fitted = FitPreviewRect(previewRect, entry.Sprite.rect.width, entry.Sprite.rect.height);
                        if (!DrawTextureWithTexCoordsSafe(fitted, entry.Sprite.texture, texCoords))
                        {
                            DrawPreviewNote(previewRect, "Preview unavailable for this sprite region.");
                        }

                        return;
                    }

                    Rect atlasRect = FitPreviewRect(previewRect, entry.Sprite.texture.width, entry.Sprite.texture.height);
                    if (DrawTextureSafe(atlasRect, entry.Sprite.texture))
                    {
                        DrawPreviewNote(previewRect, note);
                    }
                    else
                    {
                        DrawPreviewNote(previewRect, "Preview unavailable for this sprite atlas.");
                    }

                    return;
                }

                if (entry.Texture != null)
                {
                    Rect fitted = FitPreviewRect(previewRect, entry.Texture.width, entry.Texture.height);
                    if (!DrawTextureSafe(fitted, entry.Texture))
                    {
                        DrawPreviewNote(previewRect, "Preview unavailable for texture type: " + entry.Texture.GetType().Name);
                    }

                    return;
                }

                UiLabel(previewRect, "No preview texture.");
            }
            finally { GUI.EndGroup(); }
        }

        private void DrawTexturePreviewControls(Rect rect)
        {
            if (UiButton(new Rect(rect.x, rect.y, 30f, rect.height), "-"))
            {
                textureBrowserPreviewZoom = Mathf.Max(0.25f, textureBrowserPreviewZoom * 0.8f);
            }

            if (UiButton(new Rect(rect.x + 34f, rect.y, 54f, rect.height), "100%"))
            {
                ResetTextureBrowserPreviewZoom();
            }

            if (UiButton(new Rect(rect.x + 92f, rect.y, 30f, rect.height), "+"))
            {
                textureBrowserPreviewZoom = Mathf.Min(16f, textureBrowserPreviewZoom * 1.25f);
            }

            UiLabel(new Rect(rect.x + 132f, rect.y + 2f, 54f, rect.height), "Zoom");
            float newZoom = GUI.HorizontalSlider(new Rect(rect.x + 184f, rect.y + 5f, Mathf.Max(80f, rect.width - 286f), rect.height), textureBrowserPreviewZoom, 0.25f, 16f);
            textureBrowserPreviewZoom = Mathf.Clamp(newZoom, 0.25f, 16f);
            UiLabel(new Rect(rect.xMax - 92f, rect.y + 2f, 92f, rect.height), Mathf.RoundToInt(textureBrowserPreviewZoom * 100f) + "%");
        }

        private void ResetTextureBrowserPreviewView()
        {
            ReleaseMenuObjectPreview();menuShowAtlas=false;
            ResetTextureBrowserPreviewZoom();
        }

        private void ResetTextureBrowserPreviewZoom()
        {
            textureBrowserPreviewZoom = 1f;
            textureBrowserPreviewPan = Vector2.zero;
            CancelTexturePreviewPointer();
        }

        private void DrawPreviewBackground(Rect rect)
        {
            Color oldColor = GUI.color;
            int cellSize = 24;
            int xCells = Mathf.CeilToInt(rect.width / cellSize);
            int yCells = Mathf.CeilToInt(rect.height / cellSize);

            try
            {
            for (int y = 0; y < yCells; y++)
            {
                for (int x = 0; x < xCells; x++)
                {
                    GUI.color = ((x + y) & 1) == 0 ? new Color(0.32f, 0.32f, 0.32f, 1f) : new Color(0.52f, 0.52f, 0.52f, 1f);
                    Rect cell = new Rect(rect.x + x * cellSize, rect.y + y * cellSize,
                        Mathf.Min(cellSize, rect.width - x * cellSize), Mathf.Min(cellSize, rect.height - y * cellSize));
                    GUI.DrawTexture(cell, Texture2D.whiteTexture);
                }
            }

            }
            finally { GUI.color = oldColor; }
        }

        private bool DrawTextureSafe(Rect rect, Texture texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return false;
            }

            try
            {
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool DrawTextureWithTexCoordsSafe(Rect rect, Texture texture, Rect texCoords)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return false;
            }

            try
            {
                GUI.DrawTextureWithTexCoords(rect, texture, texCoords, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void DrawPreviewNote(Rect previewRect, string note)
        {
            if (string.IsNullOrEmpty(note))
            {
                return;
            }

            Rect noteRect = new Rect(previewRect.x + 8f, previewRect.yMax - 44f, previewRect.width - 16f, 40f);
            UiBox(noteRect, "");
            UiLabel(new Rect(noteRect.x + 6f, noteRect.y + 4f, noteRect.width - 12f, noteRect.height - 8f), note);
        }

        private Rect FitRect(Rect container, float sourceWidth, float sourceHeight)
        {
            if (sourceWidth <= 0f || sourceHeight <= 0f)
            {
                return container;
            }

            float scale = Mathf.Min(container.width / sourceWidth, container.height / sourceHeight);
            float width = sourceWidth * scale;
            float height = sourceHeight * scale;
            return new Rect(container.x + (container.width - width) * 0.5f, container.y + (container.height - height) * 0.5f, width, height);
        }

        private Rect FitPreviewRect(Rect container, float sourceWidth, float sourceHeight)
        {
            Rect fitted = FitRect(container, sourceWidth, sourceHeight);
            float zoom = Mathf.Clamp(textureBrowserPreviewZoom, 0.25f, 16f);
            float width = fitted.width * zoom;
            float height = fitted.height * zoom;
            return new Rect(
                container.x + (container.width - width) * 0.5f + textureBrowserPreviewPan.x,
                container.y + (container.height - height) * 0.5f + textureBrowserPreviewPan.y,
                width,
                height);
        }

        private bool TryGetSpriteTexCoords(Sprite sprite, out Rect texCoords, out string note)
        {
            texCoords = new Rect(0f, 0f, 1f, 1f);
            note = "";
            if (sprite == null || sprite.texture == null)
            {
                note = "Sprite has no texture.";
                return false;
            }

            Rect rect;
            try
            {
                rect = sprite.textureRect;
            }
            catch
            {
                note = "Packed/tight sprite: showing full atlas.";
                return false;
            }

            if (rect.width <= 0f || rect.height <= 0f || sprite.texture.width <= 0 || sprite.texture.height <= 0)
            {
                note = "Sprite has invalid preview bounds.";
                return false;
            }

            texCoords = new Rect(rect.x / sprite.texture.width, rect.y / sprite.texture.height, rect.width / sprite.texture.width, rect.height / sprite.texture.height);
            return true;
        }

    }
}
