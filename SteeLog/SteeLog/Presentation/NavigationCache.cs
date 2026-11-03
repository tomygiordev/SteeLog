using System;
using System.Collections.Generic;

namespace SteeLog.Presentation
{
    public static class NavigationCache
    {
        public static T GetOrCreate<TBase, T>(IDictionary<Type, TBase> cache, Func<T> factory, Func<TBase, bool> isDisposed)
            where TBase : class where T : class, TBase
        {
            if (cache.TryGetValue(typeof(T), out var existing) && !isDisposed(existing)) return (T)existing;
            return factory();
        }
    }
}
