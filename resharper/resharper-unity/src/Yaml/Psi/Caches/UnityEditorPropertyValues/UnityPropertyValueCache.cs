using System.Collections.Generic;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public class UnityPropertyValueCache : SimpleICache<Dictionary<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation>>
    {
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
        
        
        private static IUnsafeMarshaller<Dictionary<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation>> CreateMarshaller()
        {
            return UnsafeMarshallers.GetCollectionMarshaller( Read, Write,
                n => new Dictionary<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation>(n));
        }

        private static KeyValuePair<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation> Read(UnsafeReader reader)
        {
            return new KeyValuePair<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation>(
                MonoBehaviourProperty.ReadFrom(reader), MonoBehaviourPropertyValueWithLocation.ReadFrom(reader));
        }

        private static void Write(UnsafeWriter writer,
            KeyValuePair<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation> kvp)
        {
            kvp.Key.WriteTo(writer);
            kvp.Value.WriteTo(writer);
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
            FileID gameObject = null;
            var list = new List<(string, MonoBehaviourPropertyValueWithLocation)>();
            foreach (var document in file.DocumentsEnumerable)
            {
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
                        } else if (key.Equals("m_GameObject"))
                        {
                            // TODO: prefab modification 
                            gameObject = entry.Value.AsFileID();
                        }else if (!myIgnoredMonoBehaviourEntries.Contains(key))
                        {
                            var value = entry.Value;
                            var fileId = value.AsFileID();
                            if (fileId == null)
                            {
                                list.Add((key, new MonoBehaviourPropertyValueWithLocation(value.GetPlainScalarText(), gameObject)));
                            }
                            else
                            {
                                list.Add((key, new MonoBehaviourPropertyValueWithLocation(fileId, gameObject)));
                            }
                        }
                    }
                }
            }

            if (guid == null || list.Count == null)
                return null;

            var unityPropertyValueCacheItem = new Dictionary<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation>();
            foreach (var (propertyName, value) in list)
            {
                unityPropertyValueCacheItem[new MonoBehaviourProperty(guid, propertyName)] = value;
            }

            return unityPropertyValueCacheItem;
        }
        
    }
}