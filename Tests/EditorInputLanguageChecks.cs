using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public static class Input
    {
        public static bool anyKey;
        public static readonly bool[] Mouse=new bool[3];
        public static bool GetMouseButton(int button) { return Mouse[button]; }
    }
}
public class InputHandler { public bool acceptingInput; }
public class HeroController { }
namespace On
{
    public static class InputHandler { public delegate void orig_Update(global::InputHandler self); }
    public static class HeroController
    {
        public delegate void orig_LookForInput(global::HeroController self);
        public delegate void orig_LookForQueueInput(global::HeroController self);
        public delegate void orig_DoAttack(global::HeroController self);
    }
}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private bool browserWaitingForInputRelease;
        private int browserInputReleaseFrame;
        private static void RunEditorInputLanguageChecks()
        {
            Check(UiLocalization.NormalizeLanguage(null)=="English","Missing language defaults to English");
            Check(UiLocalization.NormalizeLanguage("junk")=="English","Invalid language falls back to English");
            foreach(string value in new[]{"Russian","RUSSIAN","ru","Русский"}) Check(UiLocalization.NormalizeLanguage(value)=="Russian","Russian aliases");
            foreach(string value in new[]{"English","ENGLISH","en"}) Check(UiLocalization.NormalizeLanguage(value)=="English","English aliases");
            Check(UiLocalization.Text("Mask","English")=="Mask","English tab caption");
            Check(UiLocalization.Text("Mask","Russian")=="Область","Russian tab caption");
            Check(UiLocalization.Text("Область","English")=="Mask","Legacy Russian caption returns English");
            Check(UiLocalization.Text("Apply and close","Russian")=="Применить и закрыть","Localize mask action");
            Check(UiLocalization.Text("Enabled: Town / RestBench","Russian")=="Включено: Town / RestBench","Keep object identity untranslated");
            Check(UiLocalization.Text("Не удалось применить: В выделенной области нет изображения объекта.","English")=="Cannot apply: The selection contains no image pixels.","Localize legacy errors");
            Check(UiLocalization.Text("sactx-0-2048x4096-DXT5-Core","Russian")=="sactx-0-2048x4096-DXT5-Core","Keep atlas identifiers");
            Check(UiLocalization.Text("C:\\assets\\bench.png","Russian")=="C:\\assets\\bench.png","Keep file paths");
            Check(!BrowserInputPolicy.ShouldClose(false,true),"Esc stays with game when F9 closed");
            Check(!BrowserInputPolicy.ShouldClose(true,false),"Unrelated key does not close F9");
            Check(BrowserInputPolicy.ShouldClose(true,true),"Esc closes F9");
            var mod=new ShaderStripMod();var handler=new InputHandler { acceptingInput=true };
            bool accepted=false;int updates=0;
            On.InputHandler.orig_Update update=self=>{updates++;accepted=self.acceptingInput;};
            UnityEngine.Time.frameCount=20;mod.textureBrowserVisible=true;
            mod.OnInputHandlerUpdate(update,handler);
            Check(updates==1&&!accepted&&handler.acceptingInput,"Preserve controller Update while blocking game's pause branch");
            bool threw=false;try {mod.OnInputHandlerUpdate(self=>{throw new Exception();},handler);} catch(Exception){threw=true;}
            Check(threw&&handler.acceptingInput,"Restore input field even if original update throws");
            handler.acceptingInput=false;mod.OnInputHandlerUpdate(update,handler);
            Check(!handler.acceptingInput,"Preserve external input lock");handler.acceptingInput=true;
            mod.textureBrowserVisible=false;mod.browserWaitingForInputRelease=true;mod.browserInputReleaseFrame=21;
            UnityEngine.Input.anyKey=true;UnityEngine.Time.frameCount=22;
            mod.OnInputHandlerUpdate(update,handler);Check(!accepted,"Esc held after F9 closes stays blocked");
            UnityEngine.Input.anyKey=false;UnityEngine.Input.Mouse[0]=true;
            mod.OnInputHandlerUpdate(update,handler);Check(!accepted,"Held close click stays blocked in main menu without HeroController");
            UnityEngine.Input.Mouse[0]=false;mod.OnInputHandlerUpdate(update,handler);
            Check(accepted&&!mod.browserWaitingForInputRelease,"Release restores game input without a hero");
            mod.browserInputReleaseFrame=25;UnityEngine.Time.frameCount=25;
            mod.OnInputHandlerUpdate(update,handler);Check(!accepted,"Close frame grace blocks game's pause branch");
            UnityEngine.Time.frameCount=26;mod.OnInputHandlerUpdate(update,handler);Check(accepted,"Game's next Esc allowed after grace");
            var hero=new HeroController();int actions=0;
            mod.textureBrowserVisible=true;mod.OnHeroLookForInput(self=>actions++,hero);mod.OnHeroLookForQueueInput(self=>actions++,hero);mod.OnHeroDoAttack(self=>actions++,hero);
            Check(actions==0,"No attack or queue input while F9 open");
            mod.textureBrowserVisible=false;mod.OnHeroLookForInput(self=>actions++,hero);mod.OnHeroLookForQueueInput(self=>actions++,hero);mod.OnHeroDoAttack(self=>actions++,hero);
            Check(actions==3,"Restore hero input when closed");
            foreach(double x1 in new[]{0.0,.2,1.0}) foreach(double y1 in new[]{0.0,.3,1.0})
            foreach(double x2 in new[]{0.0,.8,1.0}) foreach(double y2 in new[]{0.0,.7,1.0})
            {
                var points=SpriteMaskRectangle.FromCorners(x1,y1,x2,y2);var b=SpriteMaskRectangle.GetBounds(points);
                Check(points.Length==4&&b.X==Math.Min(x1,x2)&&b.Y==Math.Min(y1,y2),"Normalize reverse rectangle drag");
                Check(SpriteMaskRectangle.HasArea(points)==(x1!=x2&&y1!=y2),"Reject empty/line selection");
                if(SpriteMaskRectangle.HasArea(points))
                {
                    var clipped=SpriteMaskGeometry.Clip(new[]{new SpriteMaskGeometry.Vertex(0,0,0,0,0),new SpriteMaskGeometry.Vertex(1,0,0,1,0),new SpriteMaskGeometry.Vertex(1,1,0,1,1),new SpriteMaskGeometry.Vertex(0,1,0,0,1)},new[]{0,1,2,0,2,3},points);
                    Check(clipped.Count>0,"Rectangle clips source geometry in every drag direction");
                }
            }
            var clamped=SpriteMaskRectangle.GetBounds(SpriteMaskRectangle.FromCorners(-3,-1,4,7));Check(clamped.Width==1&&clamped.Height==1,"Clamp dragged bounds at image edges");
            bool invalid=false;try {SpriteMaskRectangle.FromCorners(double.NaN,0,1,1);} catch(ArgumentException){invalid=true;}Check(invalid,"Reject nonfinite selection");
            foreach(double width in new[]{1.0,3.0,50.0,320.0}) foreach(double height in new[]{1.0,3.0,40.0,200.0})
            foreach(double thickness in new[]{2.0,4.0})
            {
                var edges=SpriteMaskRectangle.Frame(7,11,width,height,thickness);Check(edges.Length==4,"Four sides for all selection sizes");
                foreach(var edge in edges) Check(edge.Width>0&&edge.Height>0&&edge.X>=7&&edge.Y>=11&&edge.X+edge.Width<=7+width&&edge.Y+edge.Height<=11+height,"Every edge stays inside selection to avoid GUI clipping");
                Check(edges[0].Y==11&&edges[1].Y+edges[1].Height==11+height&&edges[2].X==7&&edges[3].X+edges[3].Width==7+width,"Visible top bottom left right geometry");
            }
            var polygon=new List<SpriteMaskGeometry.Point>{new SpriteMaskGeometry.Point(.1,.2),new SpriteMaskGeometry.Point(.8,.4),new SpriteMaskGeometry.Point(.4,.9)};
            var rectangle=SpriteMaskRectangle.FromBounds(polygon);
            Check(rectangle.Length==4&&polygon.Count==3&&polygon[1].Y==.4,"Opening legacy polygon does not mutate its persisted shape");
        }
    }
}
