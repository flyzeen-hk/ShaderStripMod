using System;
using System.Collections.Generic;
namespace ShaderStripMod
{
    internal sealed class MenuValueOverride<TTarget,TValue> where TTarget:class
    {
        private sealed class State { internal TValue Original,Applied; internal Func<TValue,TValue,TValue,TValue> ObserveChange; }
        private readonly Dictionary<TTarget,State> states=new Dictionary<TTarget,State>();
        internal void Apply(TTarget target,Func<TTarget,TValue> read,Action<TTarget,TValue> write,Func<TValue,TValue> transform,Func<TValue,TValue,TValue,TValue> observeChange=null)
        {
            var current=read(target);State state;
            if(!states.TryGetValue(target,out state)){state=new State {Original=current,Applied=current};states.Add(target,state);}
            else if(!EqualityComparer<TValue>.Default.Equals(current,state.Applied))state.Original=observeChange==null ? current : observeChange(state.Original,current,state.Applied);
            state.ObserveChange=observeChange;
            var value=transform(state.Original);
            try {if(!EqualityComparer<TValue>.Default.Equals(current,value))write(target,value);}
            finally
            {
                // A setter may update the property before a dependent operation (for example tk2d.Commit) throws.
                // Remember that update so a later release can still restore the original value.
                try {state.Applied=read(target);} catch(Exception) { }
            }
        }
        internal void Release(Func<TTarget,bool> alive,Func<TTarget,TValue> read,Action<TTarget,TValue> write,Action<Exception> report=null)
        {
            ReleaseWhere(target=>true,alive,read,write,report);
        }
        internal void ReleaseWhere(Func<TTarget,bool> selected,Func<TTarget,bool> alive,Func<TTarget,TValue> read,Action<TTarget,TValue> write,Action<Exception> report=null)
        {
            var released=new List<TTarget>();
            foreach(var pair in states)
            {
                try
                {
                    if(!selected(pair.Key))continue;
                    if(alive(pair.Key))
                    {
                        var current=read(pair.Key);var state=pair.Value;
                        var restore=EqualityComparer<TValue>.Default.Equals(current,state.Applied) ? state.Original : (state.ObserveChange==null ? current : state.ObserveChange(state.Original,current,state.Applied));
                        if(!EqualityComparer<TValue>.Default.Equals(current,restore))write(pair.Key,restore);
                    }
                    released.Add(pair.Key);
                }
                catch(Exception ex) {if(report!=null)report(ex);}
            }
            // Keep failed targets for a later retry, while always releasing the other targets.
            foreach(var target in released)states.Remove(target);
        }
    }
}
