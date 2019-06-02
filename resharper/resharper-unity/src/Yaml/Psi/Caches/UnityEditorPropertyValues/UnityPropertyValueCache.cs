using System.Collections.Generic;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PsiComponent]
    public class UnityPropertyValueCache : SimpleICache<
        OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>>
    {
        private OneToSetMap<MonoBehaviourProperty, MonoBehaviourPropertyWithLocation> myLocalCache =
            new OneToSetMap<MonoBehaviourProperty, MonoBehaviourPropertyWithLocation>();

        public class MonoBehaviourPropertyWithLocation
        {
            public readonly IPsiSourceFile File;
            public readonly MonoBehaviourPropertyValue Value;

            public MonoBehaviourPropertyWithLocation(IPsiSourceFile file, MonoBehaviourPropertyValue value)
            {
                File = file;
                Value = value;
            }

            protected bool Equals(MonoBehaviourPropertyWithLocation other)
            {
                return string.Equals(File, other.File) && Value.Equals(other.Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((MonoBehaviourPropertyWithLocation) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (File.GetHashCode() * 397) ^ Value.GetHashCode();
                }
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
                   sourceFile.LanguageType.Is<YamlProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   sourceFile.IsAsset();
        }


        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<YamlLanguage>() as IYamlFile;
            if (file == null)
                return null;


            string guid = null;
            var list = new List<(string, MonoBehaviourPropertyValue)>();
            foreach (var document in file.DocumentsEnumerable)
            {
                var properties = UnityYamlPsiExtensions.GetDocumentBlockNodeProperties(document.Body.BlockNode);
                var monoBehaviourId = properties?.AnchorProperty.Text.GetText();
                if (monoBehaviourId == null)
                    continue;
                
                if (MonoScriptReferenceFactory.CanContainReference(document))
                {
                    var entries = document.FindRootBlockMapEntries();
                    if (entries == null)
                        return null;

                    foreach (var entry in entries.EntriesEnumerable)
                    {
                        var key = entry.Key.GetPlainScalarText();
                        if (key.Equals("m_Script"))
                        {
                            guid = entry.Value.AsFileID()?.guid;
                        }
                        else if (!myIgnoredMonoBehaviourEntries.Contains(key))
                        {
                            var entryValue = entry.Value;
                            var fileId = entry.Value.AsFileID();

                            if (fileId == null)
                            {
                                var primitiveValue = entryValue.GetPlainScalarText().NotNull("primitiveValue != null");
                                var value = new MonoBehaviourPrimitiveValue(primitiveValue, monoBehaviourId);
                                list.Add((key, value));
                            }
                            else
                            {
                                var value = new MonoBehaviourReferenceValue(fileId, monoBehaviourId);
                                list.Add((key, value));
                            }
                        }
                    }
                }
            }

            if (guid == null || list.Count == 0)
                return null;

            var unityPropertyValueCacheItem =
                new OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>();
            foreach (var (propertyName, value) in list)
            {
                unityPropertyValueCacheItem.Add(new MonoBehaviourProperty(guid, propertyName), value);
            }

            return unityPropertyValueCacheItem;
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
                    myLocalCache.Add(property, new MonoBehaviourPropertyWithLocation(sourceFile, value));
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
                        myLocalCache.Remove(property, new MonoBehaviourPropertyWithLocation(sourceFile, value));
                    }
                }
            }
        }
    }
}