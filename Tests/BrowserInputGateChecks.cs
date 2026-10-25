using System;
using System.Reflection;
namespace UnityEngine.EventSystems
{
    public class EventSystem
    {
        public static EventSystem current;public readonly object Selected=new object();public int Updates;
        public bool enabled=true;
        protected virtual void Update(){Updates++;}
        public EventSystem(){current=this;}
    }
}
namespace MonoMod.RuntimeDetour
{
    public sealed class Hook:IDisposable
    {
        public static Hook Current;public static int Constructed;public static bool Fail;
        public readonly Action<Action<UnityEngine.EventSystems.EventSystem>,UnityEngine.EventSystems.EventSystem> Detour;
        public bool Disposed;
        public Hook(MethodBase source,Delegate detour)
        {
            if(Fail)throw new InvalidOperationException("Cannot hook for test");
            if(source==null||source.Name!="Update")throw new Exception("Unexpected target method");
            Detour=(Action<Action<UnityEngine.EventSystems.EventSystem>,UnityEngine.EventSystems.EventSystem>)detour;
            Current=this;Constructed++;
        }
        public void Dispose(){if(Disposed)throw new Exception("Disposed twice");Disposed=true;}
    }
}
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        bool Block;string Message;bool ShouldBlockBrowserGameplayInput(){return Block;}void Log(string value){Message=value;}
        static int checks;static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
        public static void Main()
        {
            var mod=new ShaderStripMod();var system=new UnityEngine.EventSystems.EventSystem();var selected=system.Selected;
            mod.UpdateTextureBrowserMenuInputBlocker();Check(mod.browserMenuInputHook==null,"Closed browser has no dispatch detour");
            mod.Block=true;mod.UpdateTextureBrowserMenuInputBlocker();var hook=MonoMod.RuntimeDetour.Hook.Current;
            Check(hook!=null&&!hook.Disposed&&system.enabled,"Opening F9 installs gate and leaves EventSystem enabled");
            int created=MonoMod.RuntimeDetour.Hook.Constructed;mod.UpdateTextureBrowserMenuInputBlocker();
            Check(created==MonoMod.RuntimeDetour.Hook.Constructed,"Repeated updates reuse one owned hook");
            hook.Detour(self=>self.Updates++,system);
            Check(system.Updates==0&&UnityEngine.EventSystems.EventSystem.current==system&&system.Selected==selected,"F9 blocks dispatch while native ValidateDeselect still sees current and selected object");
            // Close-release grace is represented by the same production gameplay-input policy.
            mod.UpdateTextureBrowserMenuInputBlocker();hook.Detour(self=>self.Updates++,system);Check(system.Updates==0,"Held close input stays blocked without deselecting native menu");
            mod.Block=false;hook.Detour(self=>self.Updates++,system);Check(system.Updates==1,"Released input reaches native dispatch immediately");
            mod.UpdateTextureBrowserMenuInputBlocker();Check(hook.Disposed&&mod.browserMenuInputHook==null,"Release restores dispatch by disposing owned hook");
            mod.RestoreTextureBrowserMenuInputBlocker();Check(hook.Disposed&&system.enabled,"Repeated release is harmless and never changes enabled state");
            mod.Block=true;mod.UpdateTextureBrowserMenuInputBlocker();hook=MonoMod.RuntimeDetour.Hook.Current;mod.RestoreTextureBrowserMenuInputBlocker();
            Check(hook.Disposed&&mod.browserMenuInputHook==null,"Unload/scene cleanup disposes gate even when browser was open");
            MonoMod.RuntimeDetour.Hook.Fail=true;mod.UpdateTextureBrowserMenuInputBlocker();
            Check(mod.browserMenuInputHookFailed&&mod.Message!=null&&system.enabled&&UnityEngine.EventSystems.EventSystem.current==system,"Hook failure is reported without breaking native EventSystem");
            created=MonoMod.RuntimeDetour.Hook.Constructed;mod.UpdateTextureBrowserMenuInputBlocker();Check(created==MonoMod.RuntimeDetour.Hook.Constructed,"Hook failure is not retried every frame");
            mod.Block=false;mod.UpdateTextureBrowserMenuInputBlocker();MonoMod.RuntimeDetour.Hook.Fail=false;mod.Block=true;mod.UpdateTextureBrowserMenuInputBlocker();
            Check(mod.browserMenuInputHook!=null,"Next open can retry after a previously failed hook");mod.RestoreTextureBrowserMenuInputBlocker();
            foreach(float width in new[]{160f,220,320})foreach(float height in new[]{160f,360,512})
            {
                var scroll=BrowserListLayout.Viewport(12,92,width,height);var footer=BrowserListLayout.Footer(12,92,width,height);
                Check(scroll.Y+scroll.Height<=footer.Y,"Rows cannot overlap footer at any normal browser size");
                Check(footer.Y+footer.Height<=92+height&&footer.X+footer.Width<=12+width,"Counter fits inside browser list frame");
                Check(scroll.Width>0&&scroll.Height>0&&footer.Width>0,"List retains usable scroll area at smaller dimensions");
            }
            Console.WriteLine("PASS: "+checks+" actual production menu input gate and list footer assertions.");
        }
    }
}