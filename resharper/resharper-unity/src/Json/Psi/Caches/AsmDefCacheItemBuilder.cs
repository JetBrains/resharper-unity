using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.Caches
{
    public class AsmDefCacheItemBuilder
    {
        private string myName;
        private int myOffset;
        private readonly List<string> myReferences = new List<string>();

        public void SetNameDefinition(string name, int offset)
        {
            myName = name;
            myOffset = offset;
        }

        public void AddReference(string reference)
        {
            myReferences.Add(reference);
        }

        public bool HasNameDefinition => !string.IsNullOrEmpty(myName);

        public AsmDefCacheItem Build()
        {
            return HasNameDefinition ? new AsmDefCacheItem(myName, myOffset, myReferences.ToArray()) : null;
        }
    }
}