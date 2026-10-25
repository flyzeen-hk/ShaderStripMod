#if RUNTIME_AUDIT
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        // Compiled only with RuntimeAudit=true. Title-menu integration checks; no save is loaded.
        private sealed class StableRuntimeAudit : MonoBehaviour
        {
            ShaderStripMod mod;
            string directory;
            readonly List<string> results=new List<string>();
            bool started,aborted,finished,snapshotTaken,previousBackground,backgroundSnapshotTaken,restored;
            bool enabledBefore,pausedBefore,browserBefore,pauseBefore,audioBefore,pauseActiveBefore,pauseAudioBefore;
            string fileBefore,logoBefore,layoutBefore,colorBefore,mediaDirectoryBefore,languageBefore;
            string abortReason="";
            float scaleBefore,timeBefore,pauseScaleBefore;
            int tabBefore;
            SpriteRenderer dummy;
            Texture2D dummyTexture;
            Sprite dummySprite;
            Material dummyMaterial;
            int widthBefore,heightBefore;bool fullscreenBefore;
            string cameraMessageBefore;
            void Update()
            {
                if(finished)return;
                if(Input.anyKeyDown||Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)||Input.GetMouseButtonDown(2)){aborted=true;abortReason="human input detected";}
                if(started&&(mod==null||!MenuMediaPolicy.IsTitleScene(mod.GetActiveSceneName())||HeroController.SilentInstance!=null)){aborted=true;abortReason="scene changed or hero exists";}
            }
            void Start()
            {
                mod=ShaderStripMod.Instance;
                try
                {
                    if(mod==null){Destroy(this);return;}
                    string marker=Path.Combine(Path.GetDirectoryName(mod.settingsFilePath),"ShaderStripMod.runtime-audit.txt");
                    if(!File.Exists(marker)){Destroy(this);return;}
                    directory=File.ReadAllText(marker,Encoding.UTF8).Trim();
                    if(!Directory.Exists(directory)){Destroy(this);return;}
                    previousBackground=Application.runInBackground;backgroundSnapshotTaken=true;
                    Application.runInBackground=true;
                    StartCoroutine(Run());
                }
                catch(Exception ex){Check(false,"audit startup: "+ex);Finish();Destroy(this);}
            }
            void Check(bool value,string label)
            {
                results.Add((value?"PASS ":"FAIL ")+label);
                try{mod?.Log("RuntimeAudit "+results[results.Count-1]);}catch{}
            }
            void Cleanup(Action action,string label)
            {try{action();}catch(Exception ex){Check(false,"cleanup "+label+": "+ex);}}
            void Finish()
            {
                if(finished)return;finished=true;
                Restore();
                if(!string.IsNullOrEmpty(directory)&&Directory.Exists(directory))
                    Cleanup(()=>File.WriteAllText(Path.Combine(directory,"RuntimeAudit.report.txt"),string.Join("\n",results)+"\nABORTED="+aborted+"\nABORT_REASON="+abortReason,Encoding.UTF8),"report");
                try{mod?.Log("RuntimeAudit finished.");}catch{}
            }
            void OnDestroy()
            {if(!finished&&(snapshotTaken||backgroundSnapshotTaken)){aborted=true;Finish();}}
            bool Safe()
            {return !aborted&&mod!=null&&MenuMediaPolicy.IsTitleScene(mod.GetActiveSceneName())&&HeroController.SilentInstance==null;}
            void Snapshot()
            {
                snapshotTaken=true;enabledBefore=mod.customMenuEnabled;pausedBefore=mod.customMenuPaused;
                fileBefore=mod.customMenuFile;logoBefore=mod.customMenuLogoFile;layoutBefore=mod.customMenuLayout;
                colorBefore=mod.customMenuTextColor;scaleBefore=mod.customMenuTextScale;mediaDirectoryBefore=mod.menuMediaDirectory;
                browserBefore=mod.textureBrowserVisible;pauseBefore=mod.pauseGameWhileBrowserOpen;languageBefore=mod.uiLanguage;tabBefore=mod.textureBrowserDetailTab;
                timeBefore=Time.timeScale;audioBefore=AudioListener.pause;
                widthBefore=Screen.width;heightBefore=Screen.height;fullscreenBefore=Screen.fullScreen;cameraMessageBefore=mod.cameraPinMessage;
                pauseActiveBefore=mod.textureBrowserPauseActive;pauseScaleBefore=mod.textureBrowserPreviousTimeScale;pauseAudioBefore=mod.browserPreviousAudioPause;
            }
            void Restore()
            {
                if(restored)return;restored=true;
                if(snapshotTaken&&mod!=null)
                {
                    Cleanup(()=>mod.ReleaseCustomMenu(),"custom menu");
                    Cleanup(()=>mod.DisposeCameraPin(),"camera pin");mod.cameraPinMessage=cameraMessageBefore;
                    Cleanup(()=>Screen.SetResolution(widthBefore,heightBefore,fullscreenBefore),"screen size");
                    Cleanup(()=>mod.SetTextureBrowserVisible(false),"close F9");
                    mod.customMenuEnabled=enabledBefore;mod.customMenuPaused=pausedBefore;
                    mod.customMenuFile=fileBefore;mod.customMenuLogoFile=logoBefore;mod.customMenuLayout=layoutBefore;mod.customMenuTextColor=colorBefore;
                    mod.customMenuTextScale=scaleBefore;mod.menuMediaDirectory=mediaDirectoryBefore;mod.menuMediaFilesDirty=true;
                    mod.pauseGameWhileBrowserOpen=pauseBefore;mod.uiLanguage=languageBefore;mod.textureBrowserDetailTab=tabBefore;
                    if(browserBefore)Cleanup(()=>mod.SetTextureBrowserVisible(true),"restore F9");
                    mod.textureBrowserPauseActive=pauseActiveBefore;mod.textureBrowserPreviousTimeScale=pauseScaleBefore;mod.browserPreviousAudioPause=pauseAudioBefore;
                    Cleanup(()=>{Time.timeScale=timeBefore;AudioListener.pause=audioBefore;},"time and audio");
                    Cleanup(()=>mod.UpdateImageBackground(),"restore background");
                }
                Cleanup(()=>{if(dummy!=null)Destroy(dummy.gameObject);},"dummy renderer");
                Cleanup(()=>{if(dummySprite!=null)Destroy(dummySprite);},"dummy sprite");
                Cleanup(()=>{if(dummyTexture!=null)Destroy(dummyTexture);},"dummy texture");
                Cleanup(()=>{if(dummyMaterial!=null)Destroy(dummyMaterial);},"dummy material");
                if(backgroundSnapshotTaken)Cleanup(()=>Application.runInBackground=previousBackground,"runInBackground");
            }
            IEnumerator Wait(float seconds)
            {float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end&&Safe())yield return null;}
            IEnumerator Capture(string name)
            {
                yield return new WaitForEndOfFrame();if(!Safe())yield break;
                Texture2D texture=null;
                try
                {
                    texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false);
                    texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();
                    File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());
                    Check(true,"framebuffer "+name+" "+Screen.width+"x"+Screen.height);
                }
                catch(Exception ex){Check(false,"framebuffer "+name+": "+ex);}
                finally{if(texture!=null)Destroy(texture);}
            }
            Color Sample(Texture texture)
            {
                Texture2D copy=null;
                try{string note;copy=mod.CopyTextureToTexture2D(texture,out note);return copy.GetPixel(Mathf.Min(copy.width-1,50),Mathf.Min(copy.height-1,50));}
                finally{if(copy!=null)Destroy(copy);}
            }
            string AuditObject(UnityEngine.Object value)
            {return value==null ? "null" : value.name+"#"+value.GetInstanceID();}
            string AuditMaterial(Material material)
            {
                if(material==null)return "null";
                string color=material.HasProperty("_Color") ? " color="+material.GetColor("_Color") : "";
                return AuditObject(material)+" shader="+AuditObject(material.shader)+" queue="+material.renderQueue+" texture="+AuditObject(mod.GetMaterialMainTexture(material))+color;
            }
            string AuditLayerMask(int mask)
            {
                var names=new List<string>();
                for(int i=0;i<32;i++)if((mask&(1<<i))!=0)names.Add(i+":"+LayerMask.LayerToName(i));
                return "0x"+mask.ToString("X8")+" ["+string.Join(",",names)+"]";
            }
            void DumpRendering(string stage)
            {
                try
                {
                    var text=new StringBuilder();var cameras=Resources.FindObjectsOfTypeAll<Camera>();
                    text.AppendLine("===== "+stage+" scene="+mod.GetActiveSceneName()+" screen="+Screen.width+"x"+Screen.height+" custom="+mod.customMenuEnabled+" media="+mod.customMenuFile+" =====");
                    foreach(var camera in cameras)
                    {
                        if(camera==null||camera.gameObject==null||!camera.gameObject.scene.IsValid())continue;
                        try
                        {
                            text.AppendLine("CAMERA "+AuditObject(camera)+" path="+mod.GetObjectPath(camera.gameObject)+" scene="+camera.gameObject.scene.name+" enabled="+camera.enabled+" active="+camera.gameObject.activeInHierarchy);
                            text.AppendLine("  depth="+camera.depth+" rect="+camera.rect+" pixelRect="+camera.pixelRect+" display="+camera.targetDisplay+" target="+AuditObject(camera.targetTexture)+" clear="+camera.clearFlags+" color="+camera.backgroundColor);
                            text.AppendLine("  mask="+AuditLayerMask(camera.cullingMask)+" position="+camera.transform.position+" ortho="+camera.orthographic+" orthoSize="+camera.orthographicSize+" near="+camera.nearClipPlane+" far="+camera.farClipPlane);
                            foreach(var component in camera.GetComponents<Behaviour>())if(component!=null)text.AppendLine("  CAMERA COMPONENT "+component.GetType().FullName+" enabled="+component.enabled);
                            foreach(UnityEngine.Rendering.CameraEvent evt in Enum.GetValues(typeof(UnityEngine.Rendering.CameraEvent)))
                                foreach(var buffer in camera.GetCommandBuffers(evt))text.AppendLine("  BUFFER "+evt+" name="+buffer.name+" bytes="+buffer.sizeInBytes);
                        }
                        catch(Exception ex){text.AppendLine("  CAMERA ERROR "+ex.Message);}
                    }
                    foreach(var renderer in Resources.FindObjectsOfTypeAll<Renderer>())
                    {
                        if(renderer==null||renderer.gameObject==null||!renderer.gameObject.scene.IsValid()||!renderer.gameObject.activeInHierarchy||!renderer.enabled)continue;
                        try
                        {
                            var obj=renderer.gameObject;var owners=new List<string>();
                            foreach(var camera in cameras)if(camera!=null&&camera.enabled&&camera.gameObject.activeInHierarchy&&(camera.cullingMask&(1<<obj.layer))!=0)owners.Add(AuditObject(camera));
                            text.AppendLine("RENDERER "+renderer.GetType().Name+" "+AuditObject(renderer)+" path="+mod.GetObjectPath(obj)+" scene="+obj.scene.name+" layer="+obj.layer+":"+LayerMask.LayerToName(obj.layer));
                            text.AppendLine("  sorting="+SortingLayer.IDToName(renderer.sortingLayerID)+":"+renderer.sortingOrder+" bounds="+renderer.bounds+" hidden="+renderer.forceRenderingOff+" visible="+renderer.isVisible+" cameras=["+string.Join(",",owners)+"]");
                            foreach(var material in renderer.sharedMaterials)text.AppendLine("  MATERIAL "+AuditMaterial(material));
                            var sprite=renderer as SpriteRenderer;
                            if(sprite!=null)text.AppendLine("  SPRITE "+AuditObject(sprite.sprite)+" texture="+AuditObject(sprite.sprite!=null?sprite.sprite.texture:null)+" color="+sprite.color+" drawMode="+sprite.drawMode+" flip="+sprite.flipX+","+sprite.flipY);
                        }
                        catch(Exception ex){text.AppendLine("  RENDERER ERROR "+ex.Message);}
                    }
                    // uGUI artwork uses CanvasRenderer, which is not a Renderer. Include it so
                    // menu border images/gradients can be distinguished from world sprites.
                    foreach(var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
                    {
                        if(canvas==null||!canvas.gameObject.scene.IsValid()||!canvas.isActiveAndEnabled)continue;
                        text.AppendLine("CANVAS "+AuditObject(canvas)+" path="+mod.GetObjectPath(canvas.gameObject)+" mode="+canvas.renderMode+" camera="+AuditObject(canvas.worldCamera)+" sorting="+canvas.sortingLayerName+":"+canvas.sortingOrder+" override="+canvas.overrideSorting);
                    }
                    foreach(var graphic in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Graphic>())
                    {
                        if(graphic==null||!graphic.gameObject.scene.IsValid()||!graphic.isActiveAndEnabled)continue;
                        try
                        {
                            var obj=graphic.gameObject;
                            text.AppendLine("GRAPHIC "+graphic.GetType().Name+" "+AuditObject(graphic)+" path="+mod.GetObjectPath(obj)+" scene="+obj.scene.name+" layer="+obj.layer+":"+LayerMask.LayerToName(obj.layer)+" rect="+graphic.rectTransform.rect+" color="+graphic.color+" texture="+AuditObject(graphic.mainTexture)+" canvas="+AuditObject(graphic.canvas));
                            text.AppendLine("  MATERIAL "+AuditMaterial(graphic.material));
                            var image=graphic as UnityEngine.UI.Image;
                            if(image!=null)text.AppendLine("  UI SPRITE "+AuditObject(image.overrideSprite!=null?image.overrideSprite:image.sprite));
                        }
                        catch(Exception ex){text.AppendLine("  GRAPHIC ERROR "+ex.Message);}
                    }
                    File.AppendAllText(Path.Combine(directory,"MenuRendering.txt"),text.ToString()+"\n",Encoding.UTF8);
                    Check(true,"rendering diagnostic "+stage);
                }
                catch(Exception ex){Check(false,"rendering diagnostic "+stage+": "+ex);}
            }
            bool MediaReady(){return mod.menuPlayback!=null&&mod.menuPlayback.Texture!=null&&!mod.menuPlayback.Failed;}
            IEnumerator Ready(float timeout)
            {float end=Time.realtimeSinceStartup+timeout;while(!MediaReady()&&Time.realtimeSinceStartup<end&&Safe()){mod.UpdateImageBackground();yield return null;}}
            IEnumerator Body()
            {
                float end=Time.realtimeSinceStartup+60;bool titleReady=false;
                while(Time.realtimeSinceStartup<end)
                {
                    if(aborted)yield break;
                    if(mod!=null&&MenuMediaPolicy.IsTitleScene(mod.GetActiveSceneName())&&UIManager.instance!=null&&
                       UIManager.instance.mainMenuScreen!=null&&UIManager.instance.mainMenuScreen.alpha>.99f&&UIManager.instance.gameTitle!=null&&HeroController.SilentInstance==null){titleReady=true;break;}
                    yield return null;
                }
                if(!titleReady||!Safe()){Check(false,"title menu safety/warmup");yield break;}
                started=true;Snapshot();mod.pauseGameWhileBrowserOpen=true;
                var callbackField=typeof(Camera).GetField("onPreCull",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
                var callbacks=callbackField!=null ? callbackField.GetValue(null) as Delegate : null;
                int pinIndex=-1,backgroundIndex=-1,index=0;
                if(callbacks!=null)foreach(var callback in callbacks.GetInvocationList())
                {if(callback.Target==mod&&callback.Method.Name=="OnPinnedCameraPreCull")pinIndex=index;if(callback.Target==mod&&callback.Method.Name=="OnImageBackgroundCameraPreCull")backgroundIndex=index;index++;}
                Check(pinIndex>=0&&backgroundIndex>pinIndex,"native pinned-camera callback precedes background view capture");
                mod.customMenuEnabled=false;mod.ReleaseCustomMenu();mod.UpdateImageBackground();
                yield return Wait(2);
                var originalLogo=UIManager.instance.gameTitle.sprite;
                Check(true,"title "+mod.GetActiveSceneName()+" logo="+(UIManager.instance.gameTitle!=null));
                DumpRendering("before-custom-png");
                yield return Capture("00-original");
                mod.textureBrowserSearch="RestBench";mod.RefreshMenuTextureBrowserEntries();
                var sb=new StringBuilder();int logos=0,texts=0;
                foreach(var entry in mod.textureBrowserEntries)
                {
                    if(entry.Name.Contains("LogoTitle"))logos++;
                    if(entry.Kind.IndexOf("Text",StringComparison.OrdinalIgnoreCase)>=0)texts++;
                    sb.AppendLine(entry.Kind+" | "+entry.Name+"\n"+entry.Details+"\n");
                }
                File.WriteAllText(Path.Combine(directory,"MenuResources.txt"),sb.ToString(),Encoding.UTF8);
                Check(logos>0&&texts>0&&mod.textureBrowserSearch=="","real logo/text discovery: logos="+logos+" texts="+texts);
                mod.menuMediaDirectory=directory;mod.menuMediaFilesDirty=true;mod.customMenuEnabled=true;mod.customMenuPaused=false;mod.customMenuLayout="Cover";
                mod.customMenuFile="qa-menu-background.png";mod.customMenuLogoFile="qa-menu-logo.png";
                mod.customMenuTextColor="Cyan";mod.customMenuTextScale=1.1f;mod.UpdateImageBackground();yield return Wait(.5f);
                Check(MediaReady(),"native PNG decode");
                Check(UIManager.instance.gameTitle.sprite!=originalLogo,"native custom logo binding");
                Check(Vector3.Distance(UIManager.instance.gameTitle.sprite.bounds.center,originalLogo.bounds.center)<.001f,"native custom logo center preserved");
                var decorationSnapshot=new Dictionary<Renderer,bool>(mod.menuDecorationOriginalEnabled);
                bool decorationsHidden=decorationSnapshot.Count>0;
                foreach(var pair in decorationSnapshot)if(pair.Key!=null&&pair.Key.enabled)decorationsHidden=false;
                Check(decorationsHidden&&UIManager.instance.gameTitle.enabled,"native original menu decorations suppressed; logo retained");
                DumpRendering("after-custom-png");
                yield return Capture("01-png-logo");
                mod.uiLanguage="Russian";mod.SetTextureBrowserVisible(true);mod.SelectTextureBrowserDetailTab(TextureBrowserMenuDesignTab);
                yield return Wait(.3f);Check(Time.timeScale==0&&AudioListener.pause,"real F9 pause time+audio");yield return Capture("02-menu-design-ru");
                Check(UnityEngine.EventSystems.EventSystem.current!=null&&UnityEngine.EventSystems.EventSystem.current.enabled&&mod.browserMenuInputHook!=null,"native F9 input hook retains EventSystem.current");
                mod.SelectTextureBrowserDetailTab(TextureBrowserPreviewTab);
                for(int i=0;i<mod.textureBrowserEntries.Count;i++)if(mod.textureBrowserEntries[i].Name.Contains("LogoTitle")){mod.selectedTextureBrowserIndex=i;break;}
                mod.ResetTextureBrowserPreviewView();mod.textureBrowserPreviewZoom=8;mod.textureBrowserPreviewPan=new Vector2(300,-200);
                yield return Wait(.2f);yield return Capture("03-preview-clipping");
                mod.SetTextureBrowserVisible(false);mod.ReleaseMenuPlayback();mod.customMenuFile="qa-menu-animation.gif";mod.UpdateImageBackground();
                yield return Ready(10);Check(MediaReady(),"native GIF upload");
                if(MediaReady())
                {
                    var first=Sample(mod.menuPlayback.Texture);yield return Wait(.8f);var second=Sample(mod.menuPlayback.Texture);
                    Check(Vector4.Distance(first,second)>.05f,"GIF actually advances");
                    mod.customMenuPaused=true;mod.UpdateImageBackground();var paused=Sample(mod.menuPlayback.Texture);yield return Wait(.9f);
                    Check(Vector4.Distance(paused,Sample(mod.menuPlayback.Texture))<.01f,"native GIF pause");
                    yield return Capture("04-gif");mod.customMenuPaused=false;
                }
                mod.ReleaseMenuPlayback();mod.customMenuFile="qa-menu-video.mp4";mod.customMenuPaused=true;mod.UpdateImageBackground();
                yield return Ready(30);Check(MediaReady(),"native MP4 H.264 prepare");
                if(MediaReady())
                {
                    mod.customMenuPaused=false;mod.UpdateImageBackground();yield return Wait(.8f);var first=Sample(mod.menuPlayback.Texture);
                    yield return Wait(.8f);var second=Sample(mod.menuPlayback.Texture);
                    Check(Vector4.Distance(first,second)>.05f,"native MP4 playback advances");
                    mod.SetTextureBrowserVisible(true);mod.UpdateImageBackground();yield return Wait(.2f);var paused=Sample(mod.menuPlayback.Texture);yield return Wait(.8f);
                    Check(Vector4.Distance(paused,Sample(mod.menuPlayback.Texture))<.01f,"native MP4 F9 pause");
                    mod.SetTextureBrowserVisible(false);yield return Capture("05-video");
                }
                mod.customMenuEnabled=false;mod.ReleaseCustomMenu();Check(UIManager.instance.gameTitle.sprite==originalLogo,"native logo restoration");
                bool decorationsRestored=true;foreach(var pair in decorationSnapshot)if(pair.Key!=null&&pair.Key.enabled!=pair.Value)decorationsRestored=false;
                Check(decorationsRestored&&mod.menuDecorationOriginalEnabled.Count==0,"native original menu decorations restored");
                dummyTexture=new Texture2D(160,80,TextureFormat.RGBA32,false);var pixels=new Color[160*80];
                for(int y=0;y<80;y++)for(int x=0;x<160;x++)pixels[y*160+x]=x>8&&x<151&&y>8&&y<71?new Color(.1f,.7f,1):Color.clear;
                dummyTexture.SetPixels(pixels);dummyTexture.Apply();dummyTexture.name="RuntimeAuditDecoration";
                dummySprite=Sprite.Create(dummyTexture,new Rect(0,0,160,80),new Vector2(.5f,.5f),100);
                dummyMaterial=new Material(Shader.Find("Sprites/Default"));dummyMaterial.mainTexture=dummyTexture;
                var obj=new GameObject("RuntimeAuditDecoration");dummy=obj.AddComponent<SpriteRenderer>();dummy.sprite=dummySprite;dummy.sharedMaterial=dummyMaterial;dummy.forceRenderingOff=true;
                mod.textureBrowserEntries.Clear();mod.textureBrowserMenuMode=false;
                mod.textureBrowserEntries.Add(new TextureBrowserEntry {Kind="Nearby Sprite",Name="RuntimeAuditDecoration",Renderer=dummy,Texture=dummyTexture,Sprite=dummySprite});
                mod.selectedTextureBrowserIndex=0;mod.textureBrowserNeedsRefresh=false;mod.SetTextureBrowserVisible(true);mod.SelectTextureBrowserDetailTab(TextureBrowserMaskTab);mod.BeginMaskEditor(dummy);
                yield return Wait(.3f);yield return Capture("06-mask-workspace");
                mod.maskEditorPoints.Clear();mod.maskEditorPoints.AddRange(SpriteMaskRectangle.FromCorners(.1,.1,.8,.9));mod.maskEditorResultDirty=true;
                yield return Capture("07-mask-frame");
                mod.maskEditorOptions=true;mod.maskLibraryMode=true;yield return Capture("08-mask-library");
                mod.SelectTextureBrowserDetailTab(TextureBrowserRulesTab);mod.textureBrowserDetailScroll=Vector2.zero;
                yield return Wait(.2f);yield return Capture("09-rules-wide");
                mod.SelectTextureBrowserDetailTab(TextureBrowserSettingsTab);mod.textureBrowserDetailScroll=Vector2.zero;
                yield return Wait(.2f);yield return Capture("10-settings-camera-ru");
                Check(mod.GetPinnableCamera()==null,"camera pin disabled in title menu");mod.ToggleCameraPin();
                Check(!mod.cameraPin.Active,"title-menu pin attempt leaves camera unchanged");
                var method=typeof(CameraController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                mod.cameraPinUpdateHook=new MonoMod.RuntimeDetour.Hook(method,new Action<Action<CameraController>,CameraController>(mod.OnPinnedCameraLateUpdate));
                yield return Wait(.2f);Check(mod.cameraPinUpdateHook!=null,"native camera LateUpdate detour installs; unpinned title update runs");
                mod.DisposeCameraPin();Check(mod.cameraPinUpdateHook==null,"native camera detour disposes");
                var parent=new GameObject("RuntimeAuditPinParent");var target=new GameObject("RuntimeAuditPinTarget");
                try
                {
                    target.transform.parent=parent.transform;target.transform.position=new Vector3(10,20,-38);
                    var anchor=new CameraPinAnchor();anchor.Capture(target.transform,mod.GetActiveSceneName());
                    parent.transform.position=new Vector3(5,15,3);anchor.Enforce();
                    Check(Vector3.Distance(target.transform.position,new Vector3(10,20,-38))<.001f,"native Unity camera anchor cancels parent movement in world coordinates");
                    anchor.Release();target.transform.position=Vector3.zero;anchor.Enforce();
                    Check(target.transform.position==Vector3.zero,"native Unity anchor release permits movement");
                }
                finally{Destroy(target);Destroy(parent);}
                Screen.SetResolution(800,600,false);yield return Wait(.8f);
                Check(Screen.width==800&&Screen.height==600,"native small-window resolution");
                mod.SelectTextureBrowserDetailTab(TextureBrowserSettingsTab);mod.textureBrowserDetailScroll=Vector2.zero;
                yield return Wait(.2f);yield return Capture("11-settings-small-ru");
                mod.SelectTextureBrowserDetailTab(TextureBrowserRulesTab);mod.textureBrowserDetailScroll=Vector2.zero;
                yield return Wait(.2f);yield return Capture("12-rules-small-top");
                mod.textureBrowserDetailScroll=new Vector2(0,10000);yield return Wait(.2f);yield return Capture("13-rules-small-bottom");
                Screen.SetResolution(widthBefore,heightBefore,fullscreenBefore);yield return Wait(.6f);
                Check(Screen.width==widthBefore&&Screen.height==heightBefore,"native resolution restored");
                mod.SetTextureBrowserVisible(false);Check(true,"completed integration sequence");
            }
            IEnumerator Run()
            {
                // Unity executes yielded IEnumerator objects outside a parent's MoveNext catch.
                // Flatten nested routines so every audit action shares this catch/finally boundary.
                var stack=new Stack<IEnumerator>();stack.Push(Body());
                try
                {
                    while(stack.Count>0)
                    {
                        if(aborted||(started&&!Safe())){aborted=true;break;}
                        var current=stack.Peek();bool next;object yielded=null;
                        try{next=current.MoveNext();if(next)yielded=current.Current;}
                        catch(Exception ex){Check(false,"integration exception: "+ex);break;}
                        if(!next)
                        {
                            stack.Pop();Cleanup(()=>{(current as IDisposable)?.Dispose();},"completed coroutine");continue;
                        }
                        var nested=yielded as IEnumerator;
                        if(nested!=null)stack.Push(nested);else yield return yielded;
                    }
                }
                finally
                {
                    while(stack.Count>0)
                    {
                        var current=stack.Pop();Cleanup(()=>{(current as IDisposable)?.Dispose();},"pending coroutine");
                    }
                    Finish();
                }
                // Quit only this opt-in title-menu test process, with no hero/save and no human input.
                if(!aborted&&mod!=null&&MenuMediaPolicy.IsTitleScene(mod.GetActiveSceneName())&&HeroController.SilentInstance==null)Application.Quit();
                Destroy(this);
            }
        }
    }
}
#endif
