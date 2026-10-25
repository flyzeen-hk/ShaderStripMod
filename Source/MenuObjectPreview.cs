using System;
using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private TextureBrowserEntry menuObjectPreviewEntry;
        private Texture2D menuObjectPreviewTexture;
        private bool menuShowAtlas;
        private void ReleaseMenuObjectPreview()
        {
            if(menuObjectPreviewTexture!=null) UnityEngine.Object.Destroy(menuObjectPreviewTexture);
            menuObjectPreviewTexture=null;menuObjectPreviewEntry=null;
        }
        private Texture2D GetMenuObjectPreview(TextureBrowserEntry entry)
        {
            if(entry==null||!entry.MenuResource||entry.Sprite!=null||entry.Renderer==null)return null;
            if(menuObjectPreviewEntry==entry)return menuObjectPreviewTexture;
            ReleaseMenuObjectPreview();menuObjectPreviewEntry=entry;
            try
            {
                var source=ReadMaskSource(entry.Renderer);
                // Preview the resource even while the menu fades or its page is inactive.
                for(int i=0;i<source.Vertices.Length;i++)
                {
                    var v=source.Vertices[i];source.Vertices[i]=new SpriteMaskGeometry.Vertex(v.X,v.Y,v.Z,v.U,v.V);
                }
                menuObjectPreviewTexture=RenderMaskPreview(source,false);
                menuObjectPreviewTexture.name="ShaderStripMod Menu Object "+entry.Name;
            }
            catch(Exception ex){Log("Menu object preview falls back to atlas: "+ex.Message);}
            return menuObjectPreviewTexture;
        }
    }
}