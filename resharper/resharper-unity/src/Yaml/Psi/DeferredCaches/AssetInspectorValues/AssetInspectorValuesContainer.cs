using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
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
        private readonly IShellLocks myShellLocks;
        private readonly ILogger myLogger;
        private readonly List<IAssetInspectorValueDeserializer> myDeserializers;
        
        
        private readonly OneToSetMap<MonoBehaviourField, IAssetValue> myUniqueValuesInstances = new OneToSetMap<MonoBehaviourField, IAssetValue>();
        private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers = new Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer>();
        
        private readonly OneToCompactCountingSet<MonoBehaviourField, int> myUniqueValuesCount = new OneToCompactCountingSet<MonoBehaviourField, int>();
        private readonly OneToCompactCountingSet<MonoBehaviourField, IPsiSourceFile> myChangesInFiles = new OneToCompactCountingSet<MonoBehaviourField, IPsiSourceFile>();
        private readonly Dictionary<MonoBehaviourField, OneToCompactCountingSet<int, InspectorVariableUsagePointer>> myUniqueValues = 
            new Dictionary<MonoBehaviourField, OneToCompactCountingSet<int, InspectorVariableUsagePointer>>();
        
        private readonly OneToCompactCountingSet<MonoBehaviourField, int> myValueCountPerPropertyAndFile = 
            new OneToCompactCountingSet<MonoBehaviourField, int>();
        
        private readonly CountingSet<MonoBehaviourFieldWithValue> myValuesWhichAreUniqueInWholeFile = new CountingSet<MonoBehaviourFieldWithValue>();
        private readonly Dictionary<IPsiSourceFile, OneToListMap<int, InspectorVariableUsagePointer>> myPsiSourceFileToInspectorValues = new Dictionary<IPsiSourceFile, OneToListMap<int, InspectorVariableUsagePointer>>();

        private readonly OneToCompactCountingSet<int, Guid> myNameToGuids = new OneToCompactCountingSet<int, Guid>();
        
        private readonly OneToCompactCountingSet<int, IPsiSourceFile> myNameToSourceFile = new OneToCompactCountingSet<int, IPsiSourceFile>();
        
        public AssetInspectorValuesContainer(IShellLocks shellLocks, IEnumerable<IAssetInspectorValueDeserializer> assetInspectorValueDeserializer, ILogger logger)
        {
            myShellLocks = shellLocks;
            myLogger = logger;
            myDeserializers = assetInspectorValueDeserializer.OrderByDescending(t => t.Order).ToList();
        }

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AssetInspectorValuesDataElement(sourceFile);
        }

        public object Build(SeldomInterruptChecker seldomInterruptChecker, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
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
                            if (deserializer.TryGetInspectorValue(currentSourceFile, entry.Content, out var resultValue))
                            {
                                dictionary[key] = resultValue;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            myLogger.Error(e, "An error occurred while deserializing value {0}", deserializer.GetType().Name);
                        }
                    }
                }

                if (dictionary.TryGetValue(UnityYamlConstants.ScriptProperty, out var scriptValue) && scriptValue is AssetReferenceValue referenceValue
                                                                                                   && referenceValue.Reference is ExternalReference script)
                {
                    var location = new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor.Value);
                    var result = new LocalList<InspectorVariableUsage>();

                    foreach (var (key, value) in dictionary)
                    {
                        if  (key.Equals("m_Script") || key.Equals("m_GameObject"))
                            continue;

                        result.Add(new InspectorVariableUsage(location, script, key, value));
                    }

                    return result;
                }
            }

            return null;
        }

        public void Drop(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetInspectorValuesDataElement;
            foreach (var variableUsagePointer in element.EnumeratePointers())
            {
                var variableUsage = element.GetVariableUsage(variableUsagePointer);

                var scriptReference = variableUsage.ScriptReference;
                var guid = scriptReference.ExternalAssetGuid;
                
                myNameToSourceFile.Remove(variableUsage.NameHash, sourceFile);
                var mbField = new MonoBehaviourField(guid, variableUsage.NameHash);
                
                RemoveUniqueValue(mbField, variableUsagePointer, variableUsage);
                myChangesInFiles.Remove(mbField, sourceFile);
                RemoveChangesPerFile(new MonoBehaviourField(guid, variableUsage.NameHash, sourceFile), variableUsage);
                
                if (scriptReference is ExternalReference externalReference)
                    myNameToGuids.Remove(variableUsage.NameHash, externalReference.ExternalAssetGuid);
            }

            myPsiSourceFileToInspectorValues.Remove(sourceFile);

            myPointers.Remove(sourceFile);
        }

        private void RemoveChangesPerFile(MonoBehaviourField monoBehaviourField, InspectorVariableUsage variableUsage)
        {
                var beforeRemoveDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).Count;
                myValueCountPerPropertyAndFile.Remove(monoBehaviourField, variableUsage.Value.GetHashCode());
                var afterRemoveDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).Count;

                if (beforeRemoveDifferentValuesCount == 2 && afterRemoveDifferentValuesCount == 1)
                {
                    var uniqueValue = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).First().Key;
                    var fieldWithValue = new MonoBehaviourFieldWithValue(new MonoBehaviourField(monoBehaviourField.ScriptGuid, monoBehaviourField.NameHash), uniqueValue);
                    myValuesWhichAreUniqueInWholeFile.Add(fieldWithValue);
                } else if (beforeRemoveDifferentValuesCount == 1 && afterRemoveDifferentValuesCount == 0)
                {
                    var fieldWithValue = new MonoBehaviourFieldWithValue(new MonoBehaviourField(monoBehaviourField.ScriptGuid, monoBehaviourField.NameHash), variableUsage.Value.GetHashCode());
                    myValuesWhichAreUniqueInWholeFile.Remove(fieldWithValue);
                }
        }

        private void RemoveUniqueValue(MonoBehaviourField mbField, InspectorVariableUsagePointer pointer, InspectorVariableUsage variableUsage)
        {
            var valueHash = variableUsage.Value.GetHashCode();

            var previousCount = myUniqueValuesCount.GetOrEmpty(mbField).Count;
            myUniqueValuesCount.Add(mbField, valueHash);
            var newCount = myUniqueValuesCount.GetOrEmpty(mbField).Count;
            if (newCount < 2 && newCount != previousCount)
            {
                var isRemoved = myUniqueValuesInstances.Remove(mbField, variableUsage.Value);
                Assertion.Assert(isRemoved, "value should be presented");
            }

            
            
            if (!myUniqueValues.TryGetValue(mbField, out var oneToCompactCountingSet))
            {
                Assertion.Fail("mbField is not presented");
            }
            else
            {
                oneToCompactCountingSet.Remove(variableUsage.Value.GetHashCode(), pointer);
                if (oneToCompactCountingSet.Count == 0)
                    myUniqueValues.Remove(mbField);
            }
        }

        public void Merge(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            myPointers[sourceFile] = unityAssetDataElementPointer;
            
            var element = unityAssetDataElement as AssetInspectorValuesDataElement;
            var inspectorUsages = new OneToListMap<int, InspectorVariableUsagePointer>();

            foreach (var variableUsagePointer in element.EnumeratePointers())
            {
                var variableUsage = element.GetVariableUsage(variableUsagePointer);

                var scriptReference = variableUsage.ScriptReference;
                var guid = scriptReference.ExternalAssetGuid;
                
                myNameToSourceFile.Add(variableUsage.NameHash, sourceFile);

                var mbField = new MonoBehaviourField(guid, variableUsage.NameHash);
                AddUniqueValue(mbField, variableUsagePointer, variableUsage);
                myChangesInFiles.Add(mbField, sourceFile);
                AddChangesPerFile(new MonoBehaviourField(guid, variableUsage.NameHash, sourceFile), variableUsage);
                
                inspectorUsages.Add(variableUsage.NameHash, variableUsagePointer);

                if (scriptReference is ExternalReference externalReference)
                    myNameToGuids.Add(variableUsage.NameHash, externalReference.ExternalAssetGuid);
            }
            
            myPsiSourceFileToInspectorValues.Add(sourceFile, inspectorUsages);
        }

        private void AddChangesPerFile(MonoBehaviourField monoBehaviourField, InspectorVariableUsage variableUsage)
        {
            var beforeAddDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).Count;
            if (beforeAddDifferentValuesCount == 0)
            {
                myValueCountPerPropertyAndFile.Add(monoBehaviourField, variableUsage.Value.GetHashCode());
                
                var fieldWithValue = new MonoBehaviourFieldWithValue(new MonoBehaviourField(monoBehaviourField.ScriptGuid, monoBehaviourField.NameHash), variableUsage.Value.GetHashCode());
                myValuesWhichAreUniqueInWholeFile.Add(fieldWithValue);
            } else if (beforeAddDifferentValuesCount == 1)
            {
                var previousValue = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).First().Key;
                myValueCountPerPropertyAndFile.Add(monoBehaviourField, variableUsage.Value.GetHashCode());
                var afterAddDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(monoBehaviourField).Count;

                if (afterAddDifferentValuesCount == 2)
                {
                    var fieldWithValue = new MonoBehaviourFieldWithValue(new MonoBehaviourField(monoBehaviourField.ScriptGuid, monoBehaviourField.NameHash), previousValue);
                    myValuesWhichAreUniqueInWholeFile.Remove(fieldWithValue);
                }
            }
            else
            {
                myValueCountPerPropertyAndFile.Add(monoBehaviourField, variableUsage.Value.GetHashCode());
            }
        }

        private void AddUniqueValue(MonoBehaviourField field, InspectorVariableUsagePointer pointer, InspectorVariableUsage variableUsage)
        {
            var uniqueValuePtr = variableUsage.Value.GetHashCode();

            var previousCount = myUniqueValuesCount.GetOrEmpty(field).Count;
            myUniqueValuesCount.Add(field, uniqueValuePtr);
            var newCount = myUniqueValuesCount.GetOrEmpty(field).Count;
            if (previousCount < 2 && newCount != previousCount)
            {
                var isAdded =  myUniqueValuesInstances.Add(field, variableUsage.Value);
                Assertion.Assert(isAdded, "value should not be presented");
            }

            
            if (!myUniqueValues.TryGetValue(field, out var oneToCompactCountingSet))
            {
                oneToCompactCountingSet = new OneToCompactCountingSet<int, InspectorVariableUsagePointer>();
                myUniqueValues[field] = oneToCompactCountingSet;
            }

            oneToCompactCountingSet.Add(variableUsage.Value.GetHashCode(), pointer);
        }

        public int GetValueCount(Guid guid, IEnumerable<string> possibleNames, IAssetValue assetValue)
        {
            myShellLocks.AssertReadAccessAllowed();

            var count = 0;
            foreach (var name in possibleNames)
            {
                var mbField = new MonoBehaviourField(guid, name.GetPlatformIndependentHashCode());
                count += myUniqueValuesCount.GetCount(mbField, assetValue.GetHashCode());
            }

            return count;
        }
        
        public int GetUniqueValuesCount(Guid guid, IEnumerable<string> possibleNames)
        {
            myShellLocks.AssertReadAccessAllowed();

            var count = 0;
            foreach (var name in possibleNames)
            {
                var mbField = new MonoBehaviourField(guid, name.GetPlatformIndependentHashCode());
                count += myUniqueValuesCount.GetOrEmpty(mbField).Count;
            }

            return count;
        }

        public bool IsIndexResultEstimated(Guid ownerGuid, ITypeElement containingType, IEnumerable<string> possibleNames)
        {
            myShellLocks.AssertReadAccessAllowed();

            // TODO: prefab modifications
            // TODO: drop daemon dependency and inject compoentns in consructor
            var configuration = containingType.GetSolution().GetComponent<SolutionAnalysisConfiguration>();
            if (configuration.Enabled.Value && configuration.CompletedOnceAfterStart.Value && configuration.Loaded.Value)
            {
                var service = containingType.GetSolution().GetComponent<SolutionAnalysisService>();
                var id = service.GetElementId(containingType);
                if (id.HasValue && service.UsageChecker is IGlobalUsageChecker checker)
                {
                    // no inheritors
                    if (checker.GetDerivedTypeElementsCount(id.Value) == 0)
                        return false;
                }
            }
            
            var count = 0;
            foreach (var possibleName in possibleNames)
            {
                var values = myNameToGuids.GetValues(possibleName.GetPlatformIndependentHashCode());
                count += values.Length;
                if (values.Length == 1 && !values[0].Equals(ownerGuid))
                    count++;
            }
            
            return count > 1;
        }
        
        
        public IAssetValue GetUniqueValueDifferTo(Guid guid, IEnumerable<string> possibleNames, IAssetValue assetValue)
        {
            myShellLocks.AssertReadAccessAllowed();

            var result = new List<IAssetValue>();
            foreach (var possibleName in possibleNames)
            {
                var mbField = new MonoBehaviourField(guid, possibleName.GetPlatformIndependentHashCode());
                foreach (var value in myUniqueValuesInstances.GetValuesSafe(mbField))
                {
                    result.Add(value);
                }
            }

            Assertion.Assert(result.Count <= 2, "result.Count <= 2");
            if (assetValue == null)
                return result.First();
            return result.First(t => !t.Equals(assetValue));
        }

        public int GetAffectedFiles(Guid guid, IEnumerable<string> possibleNames)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var result = 0;
            foreach (var possibleName in possibleNames)
            {
                result += myChangesInFiles.GetOrEmpty(new MonoBehaviourField(guid, possibleName.GetPlatformIndependentHashCode())).Count;
            }
            
            return result;
        }
        
        public int GetAffectedFilesWithSpecificValue(Guid guid, IEnumerable<string> possibleNames, IAssetValue value)
        {
            myShellLocks.AssertReadAccessAllowed();  
            
            var result = 0;
            foreach (var possibleName in possibleNames)
            {
                result += myValuesWhichAreUniqueInWholeFile.GetCount(new MonoBehaviourFieldWithValue(new MonoBehaviourField(guid, possibleName.GetPlatformIndependentHashCode()), value.GetHashCode()));
            }
            
            return result;
        }
        
        
        private static readonly HashSet<string> ourIgnoredMonoBehaviourEntries = new HashSet<string>()
        {
            "serializedVersion",
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
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
            myPointers.Clear();
        }

        private class MonoBehaviourField
        {
            public readonly Guid ScriptGuid;
            public readonly int NameHash;
            public readonly IPsiSourceFile SourceFile;

            public MonoBehaviourField(Guid scriptGuid, int nameHash, IPsiSourceFile sourceFile = null)
            {
                ScriptGuid = scriptGuid;
                NameHash = nameHash;
                SourceFile = sourceFile;
            }
            
            public bool Equals(MonoBehaviourField other)
            {
                return ScriptGuid == other.ScriptGuid && NameHash == other.NameHash && Equals(SourceFile, other.SourceFile);
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
                    hashCode = (hashCode * 397) ^ NameHash.GetHashCode();
                    hashCode = (hashCode * 397) ^ (SourceFile != null ? SourceFile.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        private class MonoBehaviourFieldWithValue
        {
            private readonly MonoBehaviourField myField;
            private readonly int myValueHash;

            public MonoBehaviourFieldWithValue(MonoBehaviourField field, int valueHash)
            {
                myField = field;
                myValueHash = valueHash;
            }

            protected bool Equals(MonoBehaviourFieldWithValue other)
            {
                return Equals(myField, other.myField) && Equals(myValueHash, other.myValueHash);
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
                    return ((myField != null ? myField.GetHashCode() : 0) * 397) ^ (myValueHash.GetHashCode());
                }
            }
        }

        public IEnumerable<InspectorVariableUsage> GetAssetUsagesFor(IPsiSourceFile sourceFile, IField element)
        {
            myShellLocks.AssertReadAccessAllowed();

            var dataElement = myPointers.GetValueSafe(sourceFile)?.Element as AssetInspectorValuesDataElement;
            if (dataElement == null)
                return Enumerable.Empty<InspectorVariableUsage>();
            var containingType = element?.GetContainingType();

            if (containingType == null)
                return Enumerable.Empty<InspectorVariableUsage>();
            
            var result = new List<InspectorVariableUsage>();
            if (!myPsiSourceFileToInspectorValues.TryGetValue(sourceFile, out var usages))
                return EmptyList<InspectorVariableUsage>.Enumerable;

            foreach (var name in AssetUtils.GetAllNamesFor(element))
            {
                var usagesData = usages.GetValuesSafe(name.GetPlatformIndependentHashCode());
                foreach (var usagePointer in usagesData)
                {
                    var usage = dataElement.GetVariableUsage(usagePointer);
                    var scriptReference = usage.ScriptReference;
                    var guid = scriptReference.ExternalAssetGuid;

                    var typeElement = AssetUtils.GetTypeElementFromScriptAssetGuid(element.GetSolution(), guid);
                    if (typeElement == null || !typeElement.IsDescendantOf(containingType))
                        continue;

                    result.Add(usage);
                }
            }

            return result;
        }

        public LocalList<IPsiSourceFile> GetPossibleFilesWithUsage(IField element)
        {
            var result = new LocalList<IPsiSourceFile>();
            foreach (var name in AssetUtils.GetAllNamesFor(element))
            {
                foreach (var sourceFile in myNameToSourceFile.GetValues(name.GetPlatformIndependentHashCode()))
                {
                    result.Add(sourceFile);
                }
            }

            return result;
        }
    }
}