#nullable enable
using System.Collections.Generic;
using JetBrains.Collections;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Common.ProjectModel
{
    /// <summary>Builder for properties of <typeparamref name="TSubject"/> with unique constant keys.</summary>
    public class ReadonlyUserDataPerSubjectBuilder<TSubject>
    {
        private readonly Dictionary<TSubject, ReadonlyUserData.Builder> myBuilders = new();

        public void Add<T>(TSubject subject, Key<T> key, T value) where T : notnull => myBuilders.GetOrCreateValue(subject, () => new ReadonlyUserData.Builder()).Add(key, value);

        public Dictionary<TSubject, ReadonlyUserData> Build()
        {
            var result = new Dictionary<TSubject, ReadonlyUserData>(myBuilders.Count);
            foreach (var (subject, mapping) in myBuilders)
            {
                result[subject] = mapping.Build();
            }
            myBuilders.Clear();
            return result;
        }
    }
}
