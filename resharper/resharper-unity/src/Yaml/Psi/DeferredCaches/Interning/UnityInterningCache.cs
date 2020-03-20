using JetBrains.Application.PersistentMap;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.Serialization;
using JetBrains.Util.Caches;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning
{
    [SolutionInstanceComponent]
    public class UnityInterningCache
    {
        private readonly IOptimizedPersistentSortedMap<ReferenceIndex, IHierarchyReference> myReferencesInterning;
        private readonly IOptimizedPersistentSortedMap<StringIndex, string> myStringsInterning;
        public UnityInterningCache(Lifetime lifetime,  ISolutionCaches solutionCaches)
        {
            var referencesInterningMap = solutionCaches.Db.GetMap(nameof(UnityInterningCache) + "References", new UniversalMarshaller<ReferenceIndex>(ReferenceIndex.Read, ReferenceIndex.Write), 
                new UniversalMarshaller<IHierarchyReference>(ReadReference, WriteReference));
            myReferencesInterning = new OptimizedPersistentSortedMap<ReferenceIndex, IHierarchyReference>(lifetime, referencesInterningMap);
            myReferencesInterning.Cache = new DirectMappedCache<ReferenceIndex, IHierarchyReference>(10_000);            
            
            var stringsMap = solutionCaches.Db.GetMap(nameof(UnityInterningCache) + "Strings", new UniversalMarshaller<StringIndex>(StringIndex.Read, StringIndex.Write), 
                UnsafeMarshallers.UnicodeStringMarshaller);
            myStringsInterning = new OptimizedPersistentSortedMap<StringIndex, string>(lifetime, stringsMap);
            myStringsInterning.Cache = new DirectMappedCache<StringIndex, string>(1_000);

            RegisterKnownStrings();
        }

        private void RegisterKnownStrings()
        {
            myStringsInterning[new StringIndex("MonoBehaviour")] = "MonoBehaviour";
            myStringsInterning[new StringIndex("Transform")] = "Transform";
        }

        private void WriteReference(UnsafeWriter writer, IHierarchyReference value)
        {
            writer.WritePolymorphic(value);
        }

        private IHierarchyReference ReadReference(UnsafeReader reader)
        {
            return reader.ReadPolymorphic<IHierarchyReference>();
        }

        public IHierarchyReference GetReference(ReferenceIndex referenceIndex)
        {
            return myReferencesInterning.GetValueSafe(referenceIndex);
        }
        
        
        public T GetReference<T>(ReferenceIndex referenceIndex) where  T : class, IHierarchyReference
        {
            var result = GetReference(referenceIndex);
            if (result == null)
                return null;
            
            var castedResult = result as T;
            Assertion.Assert(castedResult != null, "result is T");

            return castedResult;
        }
        

        public ReferenceIndex InternReference(IHierarchyReference hierarchyReference)
        {
            if (hierarchyReference == null)
                return new ReferenceIndex(null);
            
            var index = new ReferenceIndex(hierarchyReference);
            var currentValue = myReferencesInterning.GetValueSafe(index);
            if (currentValue != null)
            {
                Assertion.Assert(currentValue.Equals(hierarchyReference), "currentValue.Equals(hierarchyReference)");
                return index;
            }
            
            myReferencesInterning[index] = hierarchyReference;
            return index;
        }
        
        
        public StringIndex InternString(string value)
        {
            if (value == null)
                return new StringIndex(null);
            
            Assertion.Assert(value != null, "value != null");
            var index = new StringIndex(value);
            var currentValue = myStringsInterning.GetValueSafe(index);
            if (currentValue != null)
            {
                Assertion.Assert(currentValue.Equals(value), "currentValue.Equals(value)");
                return index;
            }
            
            myStringsInterning[index] = value;
            return index;
        }
        
        
        public string GetString(StringIndex stringIndex)
        {
            return myStringsInterning.GetValueSafe(stringIndex).NotNull("GetString != null");
        }

        public ReferenceIndex? TryInternReference(IHierarchyReference location)
        {
            var index = new ReferenceIndex(location);
            if (myReferencesInterning.ContainsKey(index))
                return index;
            return null;
        }
    }
}