using System;
using System.Collections.Generic;

namespace ShaderStripMod.Runtime
{
    internal sealed class ReferenceCountedResourceCache<TKey, TValue> where TValue : class
    {
        internal sealed class Entry
        {
            internal TKey Key;
            internal TValue Value;
            internal int ReferenceCount;
            internal bool Retired;
        }

        internal sealed class Lease : IDisposable
        {
            private ReferenceCountedResourceCache<TKey, TValue> owner;
            private Entry entry;

            internal Lease(ReferenceCountedResourceCache<TKey, TValue> owner, Entry entry)
            {
                this.owner = owner;
                this.entry = entry;
            }

            internal TValue Value => entry == null ? null : entry.Value;

            public void Dispose()
            {
                ReferenceCountedResourceCache<TKey, TValue> currentOwner = owner;
                Entry currentEntry = entry;
                owner = null;
                entry = null;
                if (currentOwner != null && currentEntry != null) currentOwner.Release(currentEntry);
            }
        }

        private readonly Dictionary<TKey, Entry> active;
        private readonly HashSet<Entry> live = new HashSet<Entry>();
        private readonly Action<TValue> destroy;

        internal ReferenceCountedResourceCache(Action<TValue> destroy, IEqualityComparer<TKey> comparer = null)
        {
            this.destroy = destroy ?? throw new ArgumentNullException(nameof(destroy));
            active = new Dictionary<TKey, Entry>(comparer ?? EqualityComparer<TKey>.Default);
        }

        internal int ActiveKeyCount => active.Count;
        internal int LiveResourceCount => live.Count;

        internal Lease Acquire(TKey key, Func<TKey, TValue> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            Entry entry;
            if (!active.TryGetValue(key, out entry))
            {
                TValue value = factory(key);
                if (value == null) throw new InvalidOperationException("Resource factory returned null.");
                entry = new Entry { Key = key, Value = value };
                active.Add(key, entry);
                live.Add(entry);
            }

            checked { entry.ReferenceCount++; }
            return new Lease(this, entry);
        }

        internal bool Invalidate(TKey key)
        {
            Entry entry;
            if (!active.TryGetValue(key, out entry)) return false;
            active.Remove(key);
            Retire(entry);
            return true;
        }

        internal void Clear()
        {
            Entry[] entries = new Entry[active.Count];
            active.Values.CopyTo(entries, 0);
            active.Clear();
            for (int i = 0; i < entries.Length; i++) Retire(entries[i]);
        }

        private void Retire(Entry entry)
        {
            entry.Retired = true;
            DestroyIfUnused(entry);
        }

        private void Release(Entry entry)
        {
            if (entry.ReferenceCount <= 0) throw new InvalidOperationException("Resource lease released more than once.");
            entry.ReferenceCount--;
            DestroyIfUnused(entry);
        }

        private void DestroyIfUnused(Entry entry)
        {
            if (!entry.Retired || entry.ReferenceCount != 0) return;
            if (!live.Remove(entry)) return;
            TValue value = entry.Value;
            entry.Value = null;
            destroy(value);
        }
    }
}
