using System;
using System.Collections.Generic;
using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private void CollectTk2dMenuResources(HashSet<int> seen)
        {
            foreach(var text in Resources.FindObjectsOfTypeAll<tk2dTextMesh>())
            {
                if(text==null||!IsMenuResource(text))continue;
                try
                {
                    var renderer=text.GetComponent<Renderer>();
                    var material=renderer!=null ? renderer.sharedMaterial : (text.font!=null ? text.font.material : null);
                    AddMenuResourceEntry(text,"Menu tk2d Text",GetMaterialMainTexture(material),null,material,renderer,text.text,GetObjectName(text.font));
                    if(renderer!=null)seen.Add(renderer.GetInstanceID());
                }catch(Exception ex){Log("Menu tk2d text skipped: "+ex.Message);}
            }
            foreach(var sprite in Resources.FindObjectsOfTypeAll<tk2dBaseSprite>())
            {
                if(sprite==null||!IsMenuResource(sprite))continue;
                try
                {
                    var renderer=sprite.GetComponent<Renderer>();
                    if(renderer!=null&&seen.Contains(renderer.GetInstanceID()))continue;
                    var definition=sprite.GetCurrentSpriteDef();
                    var material=definition!=null ? (definition.materialInst!=null ? definition.materialInst : definition.material) : (renderer!=null ? renderer.sharedMaterial : null);
                    AddMenuResourceEntry(sprite,"Menu tk2d Sprite",GetMaterialMainTexture(material),null,material,renderer,definition!=null ? definition.name : "","");
                    if(renderer!=null)seen.Add(renderer.GetInstanceID());
                }catch(Exception ex){Log("Menu tk2d sprite skipped: "+ex.Message);}
            }
        }
    }
}