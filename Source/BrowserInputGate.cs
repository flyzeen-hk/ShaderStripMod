using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using UnityEngine.EventSystems;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private Hook browserMenuInputHook;
        private bool browserMenuInputHookFailed;
        private void UpdateTextureBrowserMenuInputBlocker()
        {
            if(!ShouldBlockBrowserGameplayInput())
            {
                RestoreTextureBrowserMenuInputBlocker();return;
            }
            if(browserMenuInputHook!=null||browserMenuInputHookFailed)return;
            try
            {
                var update=typeof(EventSystem).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
                if(update==null)throw new MissingMethodException(typeof(EventSystem).FullName,"Update");
                browserMenuInputHook=new Hook(update,new Action<Action<EventSystem>,EventSystem>(OnBrowserMenuEventSystemUpdate));
            }
            catch(Exception ex)
            {
                browserMenuInputHookFailed=true;
                Log("Menu input gate could not be installed: "+ex.Message);
            }
        }
        private void OnBrowserMenuEventSystemUpdate(Action<EventSystem> orig,EventSystem self)
        {
            // The game's ValidateDeselect coroutine dereferences EventSystem.current.
            // Keep the EventSystem enabled and its selected object alive while suppressing dispatch.
            if(!ShouldBlockBrowserGameplayInput())orig(self);
        }
        private void RestoreTextureBrowserMenuInputBlocker()
        {
            if(browserMenuInputHook!=null)
            {
                browserMenuInputHook.Dispose();browserMenuInputHook=null;
            }
            browserMenuInputHookFailed=false;
        }
    }
}