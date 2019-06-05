using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Plugins.Yaml.Psi.UnityAsset;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PsiComponent]
    public class UnityPropertyValueCache : SimpleICache<OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>>
    {
        private OneToSetMap<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation> myLocalCache =
            new OneToSetMap<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation>();

        public class MonoBehaviourPropertyValueWithLocation
        {
            public readonly IPsiSourceFile File;
            public readonly MonoBehaviourPropertyValue Value;

            public MonoBehaviourPropertyValueWithLocation(IPsiSourceFile file, MonoBehaviourPropertyValue value)
            {
                File = file;
                Value = value;
            }

            protected bool Equals(MonoBehaviourPropertyValueWithLocation other)
            {
                return Equals(File, other.File) && Value.Equals(other.Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((MonoBehaviourPropertyValueWithLocation) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (File.GetHashCode() * 397) ^ Value.GetHashCode();
                }
            }

            public string GetSimplePresentation(ISolution solution)
            {
                return Value.GetSimplePresentation(solution, File);
            }
        }

        private static readonly HashSet<string> myIgnoredMonoBehaviourEntries = new HashSet<string>()
        {
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset",
            "m_PrefabAsset",
            "m_Enabled",
            "m_EditorHideFlags",
            "m_Script",
            "m_Name",
            "m_EditorClassIdentifier"
        };

        public UnityPropertyValueCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, CreateMarshaller())
        {
        }


        private static IUnsafeMarshaller<OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>>
            CreateMarshaller()
        {
            return UnsafeMarshallers
                .GetOneToManyMapMarshaller<MonoBehaviourProperty,
                    MonoBehaviourPropertyValue,
                    IList<MonoBehaviourPropertyValue>,
                    OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>>
                (
                    new UniversalMarshaller<MonoBehaviourProperty>(MonoBehaviourProperty.ReadFrom,
                        MonoBehaviourProperty.WriteTo),
                    new UniversalMarshaller<MonoBehaviourPropertyValue>(
                        MonoBehaviourPropertyValueMarshaller.Read,
                        MonoBehaviourPropertyValueMarshaller.Write),
                    n => new OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>(n));
        }

        protected override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return base.IsApplicable(sourceFile) &&
                   sourceFile.LanguageType.Is<UAProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   sourceFile.IsAsset();
        }


        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<UALanguage>() as IYamlFile;
            if (file == null)
                return null;


            var unityPropertyValueCacheItem =
                new OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>();
            foreach (var document in file.DocumentsEnumerable)
            {
                
                if (MonoScriptReferenceFactory.CanContainReference(document))
                {
                    var monoBehaviourId = document.GetAnchor();
                    if (monoBehaviourId == null)
                        continue;

                    var list = GetPropertiesWithValues(document, monoBehaviourId, out var guid);
                    
                    if (guid == null || list == null || list.Count == 0)
                        continue;

                    foreach (var (propertyName, value) in list)
                    {
                        unityPropertyValueCacheItem.Add(new MonoBehaviourProperty(guid, propertyName), value);
                    }
                }
            }

            if (unityPropertyValueCacheItem.Count == 0)
                return null;

            return unityPropertyValueCacheItem;
        }


        private List<(string, MonoBehaviourPropertyValue)> GetPropertiesWithValues(IYamlDocument document, string mbId, out string scriptGuid)
        {
            scriptGuid = "";//document.GetUnityObjectPropertyValue("m_Script").AsFileID()?.guid;
            var gameObjectId = "";//document.GetUnityObjectPropertyValue("m_GameObject").AsFileID()?.guid;
            var entries = document.FindRootBlockMapEntries();
            if (entries == null)
                return null;

            var list = new List<(string, MonoBehaviourPropertyValue)>();
            foreach (var entry in entries.EntriesEnumerable)
            {
                var key = entry.Key.GetPlainScalarText();

                if (!myIgnoredMonoBehaviourEntries.Contains(key))
                {
                    var entryValue = entry.Value;
                    if (entry.Value.GetTextAsBuffer().Length > 50)
                        continue;
                    
                    var fileId = entry.Value.AsFileID();

                    if (fileId == null)
                    {
                        var primitiveValue = entryValue.GetPlainScalarText();
                        var value = new MonoBehaviourPrimitiveValue(primitiveValue, mbId, gameObjectId);
                        list.Add((key, value));
                    }
                    else
                    {
                        var value = new MonoBehaviourReferenceValue(fileId, mbId, gameObjectId);
                        list.Add((key, value));
                    }
                }
            }

            return list;
        }
        
        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            base.Merge(sourceFile, builtPart);
            if (builtPart is OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue> cache)
            {
                AddToLocalCache(sourceFile, cache);
            }
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);
            PopulateLocalCache();
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void PopulateLocalCache()
        {
            foreach (var (file, cacheItems) in Map)
                AddToLocalCache(file, cacheItems);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile,
            OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue> cacheItems)
        {
            foreach (var (property, values) in cacheItems)
            {
                foreach (var value in values)
                {
                    myLocalCache.Add(property, new MonoBehaviourPropertyValueWithLocation(sourceFile, value));
                }
            }
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var cacheItems))
            {
                foreach (var (property, values) in cacheItems)
                {
                    foreach (var value in values)
                    {
                        myLocalCache.Remove(property, new MonoBehaviourPropertyValueWithLocation(sourceFile, value));
                    }
                }
            }
        }

        public List<MonoBehaviourPropertyValueWithLocation> GetUnityPropertyValues(string guid, string propertyName)
        {
            return myLocalCache[new MonoBehaviourProperty(guid, propertyName)].ToList();
        }
    }
}