using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private const int TextureBrowserMaskTab = 6;
        private readonly RendererVisibilityOverride<Renderer> spriteMaskVisibility = new RendererVisibilityOverride<Renderer>();
        private readonly Dictionary<string,SpriteMaskRecord> spriteMasks = new Dictionary<string,SpriteMaskRecord>(StringComparer.Ordinal);
        private readonly Dictionary<Renderer,MaskDrawCache> spriteMaskDrawCache = new Dictionary<Renderer,MaskDrawCache>();
        private string spriteMaskMessage = "Select a static decoration in Nearby, then open Mask.";
        private sealed class MaskSource
        {
            internal SpriteMaskGeometry.Vertex[] Vertices;
            internal int[] Triangles;
            internal Texture Texture, AlphaTexture;
            internal Rect Bounds;
            internal string Fingerprint, ColorFingerprint;
        }
        private sealed class MaskDrawCache
        {
            internal Mesh Mesh;
            internal string Fingerprint, ColorFingerprint;
            internal SpriteMaskRecord Rule;
        }
        private string SpriteMaskDirectory { get { return Path.Combine(Path.GetDirectoryName(preserveFilePath),"sprite-masks"); } }
        private string HashMaskBytes(byte[] bytes)
        {
            using (var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
        }
        private string MaskIdentity(Renderer renderer)
        {
            if (renderer==null || renderer.gameObject==null) return "";
            var components=renderer.gameObject.GetComponents<Renderer>(); int componentIndex=Array.IndexOf(components,renderer);
            // Include sibling indices to distinguish identically named decorations in a room.
            var parts=new List<string>();
            for (Transform t=renderer.transform;t!=null;t=t.parent) parts.Add(t.name+"["+t.GetSiblingIndex()+"]");
            parts.Reverse();
            return GetRendererSceneName(renderer)+"\n"+string.Join("/",parts.ToArray())+"\n"+renderer.GetType().Name+"#"+componentIndex;
        }
        private string MaskFilePath(SpriteMaskRecord rule)
        { return Path.Combine(SpriteMaskDirectory,HashMaskBytes(Encoding.UTF8.GetBytes(rule.Identity))+".mask"); }
        private SpriteMaskRecord GetSpriteMask(Renderer renderer)
        {
            if (spriteMasks.Count==0 || renderer==null || IsProxyRenderer(renderer)) return null;
            SpriteMaskRecord rule; return spriteMasks.TryGetValue(MaskIdentity(renderer),out rule) ? rule : null;
        }
        private bool HasActiveSpriteMask(Renderer renderer)
        { var rule=GetSpriteMask(renderer); return rule!=null && rule.Enabled; }
        private bool HasActiveSpriteMasks()
        { foreach (var rule in spriteMasks.Values) if (rule.Enabled) return true; return false; }
        private void LoadSpriteMasks()
        {
            try
            {
                var loaded=new Dictionary<string,SpriteMaskRecord>(StringComparer.Ordinal);
                if (Directory.Exists(SpriteMaskDirectory))
                {
                    string[] paths=Directory.GetFiles(SpriteMaskDirectory,"*.mask");
                    if (paths.Length>512) throw new IOException("Too many mask files (limit: 512).");
                    foreach (string path in paths)
                    {
                        try
                        {
                            if (new FileInfo(path).Length>32768) throw new IOException("File exceeds 32 KiB.");
                            var rule=SpriteMaskRecord.Parse(File.ReadAllText(path));
                            if (!string.Equals(Path.GetFullPath(path),Path.GetFullPath(MaskFilePath(rule)),StringComparison.OrdinalIgnoreCase))
                                throw new IOException("Mask filename does not match its identity.");
                            loaded.Add(rule.Identity,rule);
                        }
                        catch (Exception ex) { Log("Sprite mask skipped: "+Path.GetFileName(path)+": "+ex.Message); }
                    }
                }
                RestorePreservedRendererDraws(); ReleaseSpriteMaskMeshes();
                spriteMasks.Clear(); foreach (var pair in loaded) spriteMasks.Add(pair.Key,pair.Value);
                spriteMaskMessage="Loaded masks: "+spriteMasks.Count+".";
            }
            catch (Exception ex) { spriteMaskMessage="Mask reload failed: "+ex.Message; Log(spriteMaskMessage); }
        }
        private MaskSource ReadMaskSource(Renderer renderer)
        {
            if (renderer==null || renderer.gameObject==null) throw new ArgumentException("Select an object in Nearby.");
            if (IsProxyRenderer(renderer) || IsProjectileCandidateRenderer(renderer) || IsHeroEntityPath(GetObjectPath(renderer.gameObject))) throw new ArgumentException("Masks are for decorations. The hero, projectiles and proxy objects cannot be edited.");
            var materials=renderer.sharedMaterials;
            if (materials==null || materials.Length!=1 || materials[0]==null) throw new ArgumentException("The object has multiple materials. Select a separate sprite.");
            Vector3[] positions; Vector2[] uv; int[] triangles; Color[] colors=null;
            Texture texture=GetMaterialMainTexture(materials[0]);
            var spriteRenderer=renderer as SpriteRenderer;
            if (spriteRenderer!=null)
            {
                if (spriteRenderer.sprite==null || spriteRenderer.drawMode!=SpriteDrawMode.Simple)
                    throw new ArgumentException("Select a regular static sprite.");
                var sprite=spriteRenderer.sprite; var vertices=sprite.vertices; positions=new Vector3[vertices.Length];
                for (int i=0;i<vertices.Length;i++) positions[i]=new Vector3(vertices[i].x,vertices[i].y,0);
                uv=sprite.uv; var indices=sprite.triangles; triangles=new int[indices.Length];
                for (int i=0;i<indices.Length;i++) triangles[i]=indices[i];
                texture=sprite.texture;
            }
            else
            {
                var filter=renderer.GetComponent<MeshFilter>();
                if (!(renderer is MeshRenderer) || filter==null || filter.sharedMesh==null)
                    throw new ArgumentException("This object type does not support masks.");
                var mesh=filter.sharedMesh;
                if (!mesh.isReadable || mesh.subMeshCount!=1 || mesh.vertexCount>4096 || mesh.GetTopology(0)!=MeshTopology.Triangles)
                    throw new ArgumentException("This object's geometry cannot be safely clipped. Select a separate sprite.");
                positions=mesh.vertices; uv=mesh.uv; triangles=mesh.triangles; colors=mesh.colors;
            }
            if (texture==null || positions.Length<3 || uv.Length!=positions.Length || triangles.Length==0 || triangles.Length>24576)
                throw new ArgumentException("This object has no accessible image.");
            float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
            foreach (var p in positions)
            { minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z); }
            if (maxX-minX<0.00001f || maxY-minY<0.00001f || maxZ-minZ>0.001f)
                throw new ArgumentException("Only flat sprites and decorations are supported.");
            var result=new MaskSource { Texture=texture,AlphaTexture=spriteRenderer!=null ? spriteRenderer.sprite.associatedAlphaSplitTexture : null,Triangles=triangles,Bounds=Rect.MinMaxRect(minX,minY,maxX,maxY),Vertices=new SpriteMaskGeometry.Vertex[positions.Length] };
            using (var stream=new MemoryStream())
            using (var writer=new BinaryWriter(stream))
            {
                writer.Write(texture.name ?? ""); writer.Write(texture.width); writer.Write(texture.height);
                writer.Write(positions.Length); writer.Write(triangles.Length);
                for (int i=0;i<positions.Length;i++)
                {
                    var p=positions[i]; var c=colors!=null && colors.Length==positions.Length ? colors[i] : Color.white;
                    result.Vertices[i]=new SpriteMaskGeometry.Vertex(p.x,p.y,p.z,uv[i].x,uv[i].y,c.r,c.g,c.b,c.a);
                    writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);writer.Write(uv[i].x);writer.Write(uv[i].y);
                }
                foreach (int i in triangles) writer.Write(i);
                writer.Flush(); result.Fingerprint=HashMaskBytes(stream.ToArray());
                stream.SetLength(0);stream.Position=0;
                foreach (var v in result.Vertices) { writer.Write(v.R);writer.Write(v.G);writer.Write(v.B);writer.Write(v.A); }
                writer.Flush();result.ColorFingerprint=HashMaskBytes(stream.ToArray());
            }
            return result;
        }
        private Mesh BuildMaskMesh(MaskSource source, IList<SpriteMaskGeometry.Point> normalized)
        {
            var points=new List<SpriteMaskGeometry.Point>();
            foreach (var p in normalized) points.Add(new SpriteMaskGeometry.Point(source.Bounds.x+p.X*source.Bounds.width,source.Bounds.y+p.Y*source.Bounds.height));
            var clipped=SpriteMaskGeometry.Clip(source.Vertices,source.Triangles,points);
            if (clipped.Count==0) throw new ArgumentException("The selection contains no image pixels.");
            return CreateMaskUnityMesh(clipped);
        }
        private Mesh CreateMaskUnityMesh(IList<SpriteMaskGeometry.Vertex> vertices)
        {
            var positions=new Vector3[vertices.Count]; var uv=new Vector2[vertices.Count]; var colors=new Color[vertices.Count]; var indices=new int[vertices.Count];
            for (int i=0;i<vertices.Count;i++)
            { var v=vertices[i];positions[i]=new Vector3((float)v.X,(float)v.Y,(float)v.Z);uv[i]=new Vector2((float)v.U,(float)v.V);colors[i]=new Color((float)v.R,(float)v.G,(float)v.B,(float)v.A);indices[i]=i; }
            var mesh=new Mesh { name="ShaderStripMod.SpriteMask",hideFlags=HideFlags.HideAndDontSave };
            try { mesh.vertices=positions;mesh.uv=uv;mesh.colors=colors;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh; }
            catch { Object.Destroy(mesh); throw; }
        }
        private bool TryDrawSpriteMask(CommandBuffer buffer, Renderer renderer, out int drawn)
        {
            drawn=0; var rule=GetSpriteMask(renderer);
            if (rule==null || !rule.Enabled) return false;
            try
            {
                if (!spriteMaskVisibility.Hold(renderer,r => r.forceRenderingOff,(r,value) => r.forceRenderingOff=value)) return true;
                var source=ReadMaskSource(renderer);
                if (source.Fingerprint!=rule.Fingerprint) throw new ArgumentException("The source image changed. Open Mask and save the selection again.");
                MaskDrawCache cache;
                if (!spriteMaskDrawCache.TryGetValue(renderer,out cache) || cache.Rule!=rule || cache.Mesh==null || cache.Fingerprint!=source.Fingerprint || cache.ColorFingerprint!=source.ColorFingerprint)
                {
                    var mesh=BuildMaskMesh(source,rule.Points);
                    if (cache!=null && cache.Mesh!=null) Object.Destroy(cache.Mesh);
                    cache=new MaskDrawCache { Mesh=mesh,Rule=rule,Fingerprint=source.Fingerprint,ColorFingerprint=source.ColorFingerprint }; spriteMaskDrawCache[renderer]=cache;
                }
                var block=new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                Matrix4x4 matrix=renderer.localToWorldMatrix;
                var spriteRenderer=renderer as SpriteRenderer;
                if (spriteRenderer!=null)
                {
                    matrix=matrix*Matrix4x4.Scale(new Vector3(spriteRenderer.flipX ? -1:1,spriteRenderer.flipY ? -1:1,1));
                    block.SetTexture("_MainTex",source.Texture);block.SetColor("_RendererColor",spriteRenderer.color);block.SetVector("_Flip",new Vector4(1,1,1,1));
                    var alpha=spriteRenderer.sprite.associatedAlphaSplitTexture;
                    block.SetFloat("_EnableExternalAlpha",alpha!=null ? 1:0);
                    if (alpha!=null) block.SetTexture("_AlphaTex",alpha);
                }
                buffer.DrawMesh(cache.Mesh,matrix,renderer.sharedMaterials[0],0,-1,block);drawn=1;
            }
            catch (Exception ex) { spriteMaskMessage="Mask hidden: "+ex.Message; }
            // A stale/invalid mask must never fall back to drawing the whole sprite.
            return true;
        }
        private void ReleaseSpriteMaskMeshes()
        {
            foreach (var cache in spriteMaskDrawCache.Values) if (cache.Mesh!=null) Object.Destroy(cache.Mesh);
            spriteMaskDrawCache.Clear();
            spriteMaskVisibility.ReleaseAll(r => r!=null,(r,value) => r.forceRenderingOff=value);
        }
        private void SaveSpriteMask(Renderer renderer, MaskSource source, IList<SpriteMaskGeometry.Point> points)
        {
            var rule=new SpriteMaskRecord { Identity=MaskIdentity(renderer),Label=GetRendererSceneName(renderer)+" / "+GetObjectPath(renderer.gameObject),Fingerprint=source.Fingerprint,Enabled=true };
            rule.Points.AddRange(points);rule.Validate();
            using (var validation=new MaskMeshLease(BuildMaskMesh(source,points))) { }
            if (!spriteMasks.ContainsKey(rule.Identity) && spriteMasks.Count>=512) throw new ArgumentException("Saved mask limit reached: 512.");
            SpriteMaskRecord.WriteAtomic(MaskFilePath(rule),rule.Serialize());
            spriteMasks[rule.Identity]=rule;
            RefreshSpriteMaskRendering();spriteMaskMessage="Mask saved: only the selected part is visible.";
        }
        private sealed class MaskMeshLease : IDisposable
        {
            readonly Mesh mesh; internal MaskMeshLease(Mesh value) { mesh=value; }
            public void Dispose() { if (mesh!=null) Object.Destroy(mesh); }
        }
        private void SetSpriteMaskEnabled(SpriteMaskRecord rule, bool enabled)
        {
            bool old=rule.Enabled; rule.Enabled=enabled;
            try { SpriteMaskRecord.WriteAtomic(MaskFilePath(rule),rule.Serialize()); }
            catch { rule.Enabled=old;throw; }
            RefreshSpriteMaskRendering();spriteMaskMessage=enabled ? "Mask enabled." : "Mask disabled. Other visibility rules still apply.";
        }
        private void RemoveSpriteMask(SpriteMaskRecord rule)
        {
            string path=MaskFilePath(rule);
            if (File.Exists(path))
            {
                string removed=Path.Combine(SpriteMaskDirectory,"removed"); Directory.CreateDirectory(removed);
                File.Move(path,Path.Combine(removed,Path.GetFileName(path)+"."+Guid.NewGuid().ToString("N")));
            }
            spriteMasks.Remove(rule.Identity);RefreshSpriteMaskRendering();spriteMaskMessage="Mask removed. A backup is in sprite-masks/removed.";
        }
        private void RefreshSpriteMaskRendering()
        {
            RestorePreservedRendererDraws();RestorePreservedRendererProxies();ReleaseSpriteMaskMeshes();RestoreShaders();
            MarkPreservedRendererCacheDirty();if (visualsActive) ApplyStrip();
        }
    }
}



