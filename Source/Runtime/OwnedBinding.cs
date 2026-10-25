using System;

namespace ShaderStripMod.Runtime
{
    internal sealed class OwnedBinding<T> where T : class
    {
        internal readonly T Original;
        internal readonly T Owned;

        internal OwnedBinding(T original, T owned)
        {
            if (owned == null) throw new ArgumentNullException("owned");
            Original = original;
            Owned = owned;
        }

        internal bool IsStillOwned(T current)
        {
            return ReferenceEquals(current, Owned);
        }

        internal bool TryGetRestoreValue(T current, out T restoreValue)
        {
            if (!IsStillOwned(current))
            {
                restoreValue = null;
                return false;
            }

            restoreValue = Original;
            return true;
        }
    }
}
