#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Common.ProjectModel
{
    public readonly struct ReadonlyUserData
    {
        private readonly Dictionary<object, object> myState;

        private ReadonlyUserData(Dictionary<object, object> state)
        {
            myState = state;
        }
        
        public T GetValue<T>(Key<T> key) => (T)myState[key];

        public bool TryGetValue<T>(Key<T> key, [MaybeNullWhen(false)] out T value)
        {
            if (!myState.TryGetValue(key, out var v))
            {
                value = default;
                return false;
            }

            value = (T)v;
            return true;
        }
        
        public class Builder
        {
            private Dictionary<object, object> myData = new();

            public void Add<T>(Key<T> key, T value) where T : notnull => myData[key] = value;

            public ReadonlyUserData Build()
            {
                var lookup = new ReadonlyUserData(myData);
                myData = null!;
                return lookup;
            }
        }
    }
}