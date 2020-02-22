using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Deserializers;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues
{
    [SolutionComponent]
    public class AssetInspectorValuesContainer : IUnityAssetDataElementContainer
    {
        private readonly DeferredCachesLocks myDeferredCachesLocks;
        private readonly ILogger myLogger;
        private readonly List<IAssetInspectorValueDeserializer> myDeserializers;
        
        private readonly OneToCompactCountingSet<MonoBehaviourField, IAssetValue> myUniqueValuesCount = new OneToCompactCountingSet<MonoBehaviourField, IAssetValue>();
        private readonly OneToCompactCountingSet<MonoBehaviourField, IPsiSourceFile> myChangesInFiles = new OneToCompactCountingSet<MonoBehaviourField, IPsiSourceFile>();
        private readonly Dictionary<MonoBehaviourField, OneToCompactCountingSet<IAssetValue, InspectorVariableUsage>> myUniqueValues = 
            new Dictionary<MonoBehaviourField, OneToCompactCountingSet<IAssetValue, InspectorVariableUsage>>();
        
        private readonly OneToCompactCountingSet<MonoBehaviourField, IAssetValue> myValueCountPerPropertyAndFile = 
            new OneToCompactCountingSet<MonoBehaviourField, IAssetValue>();
        
        private readonly CountingSet<MonoBehaviourFieldWithValue> myValuesWhichAreUniqueInWholeFile = new CountingSet<MonoBehaviourFieldWithValue>();
        private readonly Dictionary<IPsiSourceFile, OneToListMap<string, InspectorVariableUsage>> myPsiSourceFileToInspectorValues = new Dictionary<IPsiSourceFile, OneToListMap<string, InspectorVariableUsage>>();

        
        public AssetInspectorValuesContainer(DeferredCachesLocks deferredCachesLocks, IEnumerable<IAssetInspectorValueDeserializer> assetInspectorValueDeserializer, ILogger logger)
        {
            myDeferredCachesLocks = deferredCachesLocks;
            myLogger = logger;
            myDeserializers = assetInspectorValueDeserializer.OrderByDescending(t => t.Order).ToList();
        }
        
        public IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            if (AssetUtils.IsMonoBehaviourDocument(assetDocument.Buffer))
            {
                var anchor = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
                if (!anchor.HasValue)
                    return null;
                
                var dictionary = new Dictionary<string, IAssetValue>();
                var entries = assetDocument.Document.FindRootBlockMapEntries()?.Entries;
                if (entries == null)
                    return null;
                
                foreach (var entry in entries)
                {
                    var key = entry.Key.GetPlainScalarText();
                    if (key == null || ourIgnoredMonoBehaviourEntries.Contains(key))
                        continue;

                    foreach (var deserializer in myDeserializers)
                    {
                        try
                        {
                            if (deserializer.TryGetInspectorValue(currentSourceFile, entry.Content, out var result))
                            {
                                dictionary[key] = result;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            myLogger.Error(e, "An error occurred while deserializing value {0}", deserializer.GetType().Name);
                        }
                    }
                }

                if (dictionary.TryGetValue(UnityYamlConstants.ScriptProperty, out var scriptValue) && scriptValue is AssetReferenceValue referenceValue)
                {
                    var location = new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor.Value);
                    var script = referenceValue.Reference;
                    var list = new List<InspectorVariableUsage>();
                    foreach (var (key, value) in dictionary)
                    {
                        list.Add(new InspectorVariableUsage(location, script, key, value));
                    }

                    var result = new AssetInspectorValuesDataElement(list);
                    return result;

                }
            }

            return null;
        }

        public void Drop(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetInspectorValuesDataElement;
            foreach (var variableUsage in element.VariableUsages)
            {
                var guid = (variableUsage.ScriptReference as ExternalReference)?.ExternalAssetGuid;
                if (guid == null)
                    continue;

                var mbField = new MonoBehaviourField(guid, variableUsage.Name);
                myUniqueValuesCount.Remove(mbField, variableUsage.Value);
                RemoveUniqueValue(mbField, variableUsage);
                myChangesInFiles.Remove(mbField, sourceFile);
                RemoveChangesPerFile(new MonoBehaviourField(guid, variableUsage.Name, sourceFile), variableUsage);
            }

            myPsiSourceFileToInspectorValues.Remove(sourceFile);
        }

        private void RemoveChangesPerFile(MonoBehaviourField monoBehaviourField, InspectorVariableUsage variableUsage)
        {
                var beforeRemoveDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).Count;
                myValueCountPerPropertyAndFile.Remove(monoBehaviourField, variableUsage.Value);
                var afterRemoveDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).Count;

                if (beforeRemoveDifferentValuesCount == 2 && afterRemoveDifferentValuesCount == 1)
                {
                    var uniqueValue = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).First().Key;
                    var fieldWithValue = new MonoBehaviourFieldWithValue(new MonoBehaviourField(monoBehaviourField.ScriptGuid, monoBehaviourField.Name), uniqueValue);
                    myValuesWhichAreUniqueInWholeFile.Add(fieldWithValue);
                } else if (beforeRemoveDifferentValuesCount == 1 && afterRemoveDifferentValuesCount == 0)
                {
                    var fieldWithValue = new MonoBehaviourFieldWithValue(new MonoBehaviourField(monoBehaviourField.ScriptGuid, monoBehaviourField.Name), variableUsage.Value);
                    myValuesWhichAreUniqueInWholeFile.Remove(fieldWithValue);
                }
        }

        private void RemoveUniqueValue(MonoBehaviourField mbField, InspectorVariableUsage variableUsage)
        {
            if (!myUniqueValues.TryGetValue(mbField, out var oneToCompactCountingSet))
            {
                Assertion.Fail("mbField is not presented");
            }
            else
            {
                oneToCompactCountingSet.Remove(variableUsage.Value, variableUsage);
                if (oneToCompactCountingSet.Count == 0)
                    myUniqueValues.Remove(mbField);
            }
        }

        public void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetInspectorValuesDataElement;
            var inspectorUsages = new OneToListMap<string, InspectorVariableUsage>();

            foreach (var variableUsage in element.VariableUsages)
            {
                var guid = (variableUsage.ScriptReference as ExternalReference)?.ExternalAssetGuid;
                if (guid == null)
                    continue;

                var mbField = new MonoBehaviourField(guid, variableUsage.Name);
                myUniqueValuesCount.Add(mbField ,variableUsage.Value);
                AddUniqueValue(mbField, variableUsage);
                myChangesInFiles.Add(mbField, sourceFile);
                AddChangesPerFile(new MonoBehaviourField(guid, variableUsage.Name, sourceFile), variableUsage);
                
                inspectorUsages.Add(variableUsage.Name, variableUsage);
            }
            
            myPsiSourceFileToInspectorValues.Add(sourceFile, inspectorUsages);
        }

        private void AddChangesPerFile(MonoBehaviourField monoBehaviourField, InspectorVariableUsage variableUsage)
        {
            var beforeAddDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).Count;
            if (beforeAddDifferentValuesCount == 0)
            {
                myValueCountPerPropertyAndFile.Add(monoBehaviourField, variableUsage.Value);
                
                var fieldWithValue = new MonoBehaviourFieldWithValue(new MonoBehaviourField(monoBehaviourField.ScriptGuid, monoBehaviourField.Name), variableUsage.Value);
                myValuesWhichAreUniqueInWholeFile.Add(fieldWithValue);
            } else if (beforeAddDifferentValuesCount == 1)
            {
                var previousValue = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).First().Key;
                myValueCountPerPropertyAndFile.Add(monoBehaviourField, variableUsage.Value);
                var afterAddDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).Count;

                if (afterAddDifferentValuesCount == 2)
                {
                    var fieldWithValue = new MonoBehaviourFieldWithValue(new MonoBehaviourField(monoBehaviourField.ScriptGuid, monoBehaviourField.Name), previousValue);
                    myValuesWhichAreUniqueInWholeFile.Remove(fieldWithValue);
                }
            }
            else
            {
                myValueCountPerPropertyAndFile.Add(monoBehaviourField, variableUsage.Value);
            }
        }

        private void AddUniqueValue(MonoBehaviourField field, InspectorVariableUsage variableUsage)
        {
            if (!myUniqueValues.TryGetValue(field, out var oneToCompactCountingSet))
            {
                oneToCompactCountingSet = new OneToCompactCountingSet<IAssetValue, InspectorVariableUsage>();
                myUniqueValues[field] = oneToCompactCountingSet;
            }

            oneToCompactCountingSet.Add(variableUsage.Value, variableUsage);
        }

        public int GetValueCount(string guid, IEnumerable<string> possibleNames, IAssetValue assetValue)
        {
            var count = 0;
            foreach (var name in possibleNames)
            {
                var mbField = new MonoBehaviourField(guid, name);
                count += myUniqueValuesCount.GetCount(mbField, assetValue);
            }

            return count;
        }
        
        public int GetUniqueValuesCount(string guid, IEnumerable<string> possibleNames)
        {
            var count = 0;
            foreach (var name in possibleNames)
            {
                var mbField = new MonoBehaviourField(guid, name);
                count += myUniqueValuesCount.GetOrEmpty(mbField).Count;
            }

            return count;
        }

        public IEnumerable<IAssetValue> GetUniqueValues(string guid, IEnumerable<string> possibleNames)
        {
            var result = new List<IAssetValue>();
            foreach (var possibleName in possibleNames)
            {
                var mbField = new MonoBehaviourField(guid, possibleName);
                foreach (var value in myUniqueValuesCount.GetOrEmpty(mbField))
                {
                    result.Add(value.Key);
                }
            }

            return result;
        }

        public int GetAffectedFiles(string guid, IEnumerable<string> possibleNames)
        {
            var result = 0;
            foreach (var possibleName in possibleNames)
            {
                result += myChangesInFiles.GetOrEmpty(new MonoBehaviourField(guid, possibleName)).Count;
            }
            
            return result;
        }
        
        public int GetAffectedFilesWithSpecificValue(string guid, IEnumerable<string> possibleNames, IAssetValue value)
        {
            var result = 0;
            foreach (var possibleName in possibleNames)
            {
                result += myValuesWhichAreUniqueInWholeFile.GetCount(new MonoBehaviourFieldWithValue(new MonoBehaviourField(guid, possibleName), value));
            }
            
            return result;
        }
        
        
        private static readonly HashSet<string> ourIgnoredMonoBehaviourEntries = new HashSet<string>()
        {
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset",
            "m_PrefabAsset",
            "m_Enabled",
            "m_EditorHideFlags",
            "m_EditorClassIdentifier"
        };
        
        public string Id => nameof(AssetInspectorValuesContainer);
        public int Order => 0;
        public void Invalidate()
        {
            myUniqueValues.Clear();
            myUniqueValuesCount.Clear();
            myChangesInFiles.Clear();
            myValuesWhichAreUniqueInWholeFile.Clear();
            myPsiSourceFileToInspectorValues.Clear();
            myValueCountPerPropertyAndFile.Clear();
        }

        private class MonoBehaviourField
        {
            public readonly string ScriptGuid;
            public readonly string Name;
            public readonly IPsiSourceFile SourceFile;

            public MonoBehaviourField(string scriptGuid, string name)
            {
                ScriptGuid = scriptGuid;
                Name = name;
                SourceFile = null;
            }
            
            public MonoBehaviourField(string scriptGuid, string name, IPsiSourceFile sourceFile)
            {
                ScriptGuid = scriptGuid;
                Name = name;
                SourceFile = sourceFile;
            }

            public bool Equals(MonoBehaviourField other)
            {
                return ScriptGuid == other.ScriptGuid && Name == other.Name && Equals(SourceFile, other.SourceFile);
            }

            public override bool Equals(object obj)
            {
                return obj is MonoBehaviourField other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = ScriptGuid.GetHashCode();
                    hashCode = (hashCode * 397) ^ Name.GetHashCode();
                    hashCode = (hashCode * 397) ^ (SourceFile != null ? SourceFile.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        private class MonoBehaviourFieldWithValue
        {
            private readonly MonoBehaviourField myField;
            private readonly IAssetValue myValue;

            public MonoBehaviourFieldWithValue(MonoBehaviourField field, IAssetValue value)
            {
                myField = field;
                myValue = value;
            }

            protected bool Equals(MonoBehaviourFieldWithValue other)
            {
                return Equals(myField, other.myField) && Equals(myValue, other.myValue);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((MonoBehaviourFieldWithValue) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((myField != null ? myField.GetHashCode() : 0) * 397) ^ (myValue != null ? myValue.GetHashCode() : 0);
                }
            }
        }

        public IEnumerable<InspectorVariableUsage> GetAssetUsagesFor(IPsiSourceFile sourceFile, IField element)
        {
            var containingType = element?.GetContainingType();

            if (containingType == null)
                return Enumerable.Empty<InspectorVariableUsage>();
            
            return myDeferredCachesLocks.ExecuteUnderReadLock(lf =>
            {
                var result = new List<InspectorVariableUsage>();
                if (!myPsiSourceFileToInspectorValues.TryGetValue(sourceFile, out var usages))
                    return EmptyList<InspectorVariableUsage>.Enumerable;

                foreach (var name in AssetUtils.GetAllNamesFor(element))
                {
                    var usagesData = usages.GetValuesSafe(name);
                    foreach (var usage in usagesData)
                    {
                        var guid = (usage.ScriptReference as ExternalReference)?.ExternalAssetGuid;
                        if (guid == null)
                            continue;

                        var typeElement = AssetUtils.GetTypeElementFromScriptAssetGuid(element.GetSolution(), guid);
                        if (typeElement == null || !typeElement.IsDescendantOf(containingType))
                            continue;

                        result.Add(usage);
                    }
                }

                return result;
            });
        }
    }
}