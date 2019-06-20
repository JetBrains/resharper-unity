using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PsiComponent]
    public class UnityPropertyValueCache : SimpleICache<OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>>
    {
        private PropertyValueLocalCache myLocalCache = new PropertyValueLocalCache();

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

        private readonly IContextBoundSettingsStore myContextBoundSettingStore;

        public UnityPropertyValueCache(ISolution solution, Lifetime lifetime, IPersistentIndexManager persistentIndexManager,
            ISettingsStore settings)
            : base(lifetime, persistentIndexManager, CreateMarshaller())
        {
            myContextBoundSettingStore = settings.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
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
            return myContextBoundSettingStore.GetValue((UnitySettings k) => k.EnableInspectorPropertiesEditor) && 
                   base.IsApplicable(sourceFile) &&
                   sourceFile.LanguageType.Is<UnityYamlProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   sourceFile.IsAsset();
        }


        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<UnityYamlLanguage>() as IYamlFile;
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
            scriptGuid = document.GetUnityObjectPropertyValue("m_Script").AsFileID()?.guid;
            var gameObjectId = document.GetUnityObjectPropertyValue("m_GameObject").AsFileID()?.fileID;
            var entries = document.FindRootBlockMapEntries();
            if (entries == null)
                return null;

            var list = new List<(string, MonoBehaviourPropertyValue)>();
            foreach (var entry in entries.EntriesEnumerable)
            {
                var key = entry.Key.GetPlainScalarText();

                if (!myIgnoredMonoBehaviourEntries.Contains(key))
                {
                    // TODO fix parser, entryValue should not be null
                    var entryContent= entry.Content;
                    if (entryContent is IChameleonNode)
                    {
                        list.Add((key, new MonoBehaviourHugeValue(mbId, gameObjectId)));
                        continue;
                    }

                    var entryValue = entryContent?.Value;
                    var fileId = entryValue?.AsFileID();

                    if (fileId == null)
                    {
                        var primitiveValue = entryValue?.GetPlainScalarText() ?? string.Empty;
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

        public IEnumerable<MonoBehaviourPropertyValueWithLocation> GetUnityPropertyValues(string guid, string propertyName)
        {
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myLocalCache.GetValues(query);
        }
        
        public int GetValueCount(string guid, string propertyName, object value)
        {
            return myLocalCache.GetValueCount(new MonoBehaviourProperty(guid, propertyName), value);
        }

        public int GetPropertyValuesCount(string guid, string propertyName)
        {
            return myLocalCache.GetPropertyValuesCount(new MonoBehaviourProperty(guid, propertyName));
        }
        
        public int GetPropertyUniqueValuesCount(string guid, string propertyName)
        {
            return myLocalCache.GetPropertyUniqueValuesCount(new MonoBehaviourProperty(guid, propertyName));
        }
    }
}