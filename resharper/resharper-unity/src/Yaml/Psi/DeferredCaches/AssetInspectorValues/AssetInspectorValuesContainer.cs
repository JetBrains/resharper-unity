using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Deserializers;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
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
        private readonly AssetDocumentHierarchyElementContainer myHierarchyElementContainer;
        private readonly ILogger myLogger;
        private readonly List<IAssetInspectorValueDeserializer> myDeserializers;


        private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers = new Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer>();

        private readonly OneToCompactCountingSet<MonoBehaviourField, int> myUniqueValuesCount = new OneToCompactCountingSet<MonoBehaviourField, int>();
        private readonly OneToCompactCountingSet<MonoBehaviourField, IPsiSourceFile> myChangesInFiles = new OneToCompactCountingSet<MonoBehaviourField, IPsiSourceFile>();

        private readonly OneToCompactCountingSet<MonoBehaviourField, int> myValueCountPerPropertyAndFile =
            new OneToCompactCountingSet<MonoBehaviourField, int>();

        private readonly CountingSet<MonoBehaviourFieldWithValue> myValuesWhichAreUniqueInWholeFile = new CountingSet<MonoBehaviourFieldWithValue>();


        // cached value for fast presentation
        private readonly OneToSetMap<MonoBehaviourField, IAssetValue> myUniqueValuesInstances = new OneToSetMap<MonoBehaviourField, IAssetValue>();

        // For estimated counter (it's hard to track real counter due to chain resolve, prefab modifications points to another file, another file could point to another file and so on)
        // Also, it is possible that field name are used in script components with different guids. If there is only `1 guid, we could resolve type element and check that field  belongs to that type element.
        // but if there are a lot of guids, we could not resolve each type element due to performance reason, several guids could be accepted, because field could belong to abstract class
        private readonly OneToCompactCountingSet<int, Guid> myNameHashToGuids = new OneToCompactCountingSet<int, Guid>();
        private readonly CountingSet<string> myNamesInPrefabModifications = new CountingSet<string>();

        // find usage scope
        private readonly OneToCompactCountingSet<string, IPsiSourceFile> myNameToSourceFile = new OneToCompactCountingSet<string, IPsiSourceFile>();

        public AssetInspectorValuesContainer(IShellLocks shellLocks, AssetDocumentHierarchyElementContainer hierarchyElementContainer, IEnumerable<IAssetInspectorValueDeserializer> assetInspectorValueDeserializer, ILogger logger)
        {
            myShellLocks = shellLocks;
            myHierarchyElementContainer = hierarchyElementContainer;
            myLogger = logger;
            myDeserializers = assetInspectorValueDeserializer.OrderByDescending(t => t.Order).ToList();
        }

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AssetInspectorValuesDataElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return true;
        }

        public object Build(SeldomInterruptChecker seldomInterruptChecker, IPsiSourceFile currentAssetSourceFile, AssetDocument assetDocument)
        {
            var modifications = ProcessPrefabModifications(currentAssetSourceFile, assetDocument);
            if (AssetUtils.IsMonoBehaviourDocument(assetDocument.Buffer))
            {

                var anchor = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
                if (!anchor.HasValue)
                    return new InspectorValuesBuildResult(new LocalList<InspectorVariableUsage>(), modifications);

                var dictionary = new Dictionary<string, IAssetValue>();
                var entries = assetDocument.Document.GetUnityObjectProperties()?.Entries;
                if (entries == null)
                    return new InspectorValuesBuildResult(new LocalList<InspectorVariableUsage>(), modifications);

                foreach (var entry in entries)
                {
                    var key = entry.Key.GetPlainScalarText();
                    if (key == null || ourIgnoredMonoBehaviourEntries.Contains(key))
                        continue;

                    foreach (var deserializer in myDeserializers)
                    {
                        try
                        {
                            if (deserializer.TryGetInspectorValue(currentAssetSourceFile, entry.Content, out var resultValue))
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
                    var location = new LocalReference(currentAssetSourceFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), anchor.Value);
                    var result = new LocalList<InspectorVariableUsage>();

                    foreach (var (key, value) in dictionary)
                    {
                        if  (key.Equals(UnityYamlConstants.ScriptProperty) || key.Equals(UnityYamlConstants.GameObjectProperty))
                            continue;

                        result.Add(new InspectorVariableUsage(location, script, key, value));
                    }

                    return new InspectorValuesBuildResult(result, modifications);
                }
            }

            return new InspectorValuesBuildResult(new LocalList<InspectorVariableUsage>(), modifications);
        }

        private ImportedInspectorValues ProcessPrefabModifications(IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            var result = new ImportedInspectorValues();

            if (assetDocument.HierarchyElement is IPrefabInstanceHierarchy prefabInstanceHierarchy)
            {
                foreach (var modification in prefabInstanceHierarchy.PrefabModifications)
                {
                    if (!(modification.Target is ExternalReference externalReference))
                        continue;

                    if (modification.PropertyPath.Contains("."))
                        continue;

                    var location = new LocalReference(currentSourceFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), PrefabsUtil.GetImportedDocumentAnchor(prefabInstanceHierarchy.Location.LocalDocumentAnchor, externalReference.LocalDocumentAnchor));
                    result.Modifications[new ImportedValueReference(location, modification.PropertyPath)] = (modification.Value, new AssetReferenceValue(modification.ObjectReference));
                }
            }

            return result;
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var element = unityAssetDataElement as AssetInspectorValuesDataElement;
            var usages = element.VariableUsages;

            // inverted order is matter for Remove/AddUniqueValue
            for (int i = usages.Count - 1; i >= 0; i--)
            {
                var variableUsage = usages[i];
                var scriptReference = variableUsage.ScriptReference;
                var guid = scriptReference.ExternalAssetGuid;

                myNameToSourceFile.Remove(variableUsage.Name, currentAssetSourceFile);
                var mbField = new MonoBehaviourField(guid, variableUsage.Name.GetPlatformIndependentHashCode());

                RemoveUniqueValue(mbField, variableUsage);
                myChangesInFiles.Remove(mbField, currentAssetSourceFile);
                RemoveChangesPerFile(new MonoBehaviourField(guid, variableUsage.Name.GetPlatformIndependentHashCode(), currentAssetSourceFile), variableUsage);

                myNameHashToGuids.Remove(variableUsage.Name.GetPlatformIndependentHashCode(), scriptReference.ExternalAssetGuid);
            }

            foreach (var (reference, _) in element.ImportedInspectorValues.Modifications)
            {
                myNamesInPrefabModifications.Remove(reference.Name);
            }

            myPointers.Remove(currentAssetSourceFile);
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

        private void RemoveUniqueValue(MonoBehaviourField mbField, InspectorVariableUsage variableUsage)
        {
            var valueHash = variableUsage.Value.GetHashCode();

            var previousCount = myUniqueValuesCount.GetOrEmpty(mbField).Count;
            myUniqueValuesCount.Remove(mbField, valueHash);
            var newCount = myUniqueValuesCount.GetOrEmpty(mbField).Count;
            if (newCount < 2 && newCount != previousCount)
            {
                var isRemoved = myUniqueValuesInstances.Remove(mbField, variableUsage.Value);
                Assertion.Assert(isRemoved, "value should be presented");
            }
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            myPointers[currentAssetSourceFile] = unityAssetDataElementPointer;

            var element = unityAssetDataElement as AssetInspectorValuesDataElement;


            foreach (var variableUsage in element.VariableUsages)
            {
                var scriptReference = variableUsage.ScriptReference;
                var guid = scriptReference.ExternalAssetGuid;

                myNameToSourceFile.Add(variableUsage.Name, currentAssetSourceFile);

                var mbField = new MonoBehaviourField(guid, variableUsage.Name.GetPlatformIndependentHashCode());
                AddUniqueValue(mbField, variableUsage);
                myChangesInFiles.Add(mbField, currentAssetSourceFile);
                AddChangesPerFile(new MonoBehaviourField(guid, variableUsage.Name.GetPlatformIndependentHashCode(), currentAssetSourceFile), variableUsage);

                myNameHashToGuids.Add(variableUsage.Name.GetPlatformIndependentHashCode(), scriptReference.ExternalAssetGuid);
            }

            foreach (var (reference, _) in element.ImportedInspectorValues.Modifications)
            {
                myNamesInPrefabModifications.Add(reference.Name);
                myNameToSourceFile.Add(reference.Name, currentAssetSourceFile);
            }
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

        private void AddUniqueValue(MonoBehaviourField field, InspectorVariableUsage variableUsage)
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
            var names = possibleNames.ToArray();
            foreach (var name in names)
            {
                if (myNamesInPrefabModifications.GetCount(name) > 0)
                    return true;
            }

            return AssetUtils.HasPossibleDerivedTypesWithMember(ownerGuid, containingType, names, myNameHashToGuids);
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
            myUniqueValuesCount.Clear();
            myChangesInFiles.Clear();
            myValuesWhichAreUniqueInWholeFile.Clear();
            myValueCountPerPropertyAndFile.Clear();
            myPointers.Clear();
        }

        private class MonoBehaviourField
        {
            public readonly Guid ScriptGuid;
            public readonly int NameHash;
            public readonly IPsiSourceFile AssetSourceFile;

            public MonoBehaviourField(Guid scriptGuid, int nameHash, IPsiSourceFile assetSourceFile = null)
            {
                ScriptGuid = scriptGuid;
                NameHash = nameHash;
                AssetSourceFile = assetSourceFile;
            }

            public bool Equals(MonoBehaviourField other)
            {
                return ScriptGuid == other.ScriptGuid && NameHash == other.NameHash && Equals(AssetSourceFile, other.AssetSourceFile);
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
                    hashCode = (hashCode * 397) ^ (AssetSourceFile != null ? AssetSourceFile.GetHashCode() : 0);
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

        public IEnumerable<UnityInspectorFindResult> GetAssetUsagesFor(IPsiSourceFile sourceFile, IField element)
        {
            myShellLocks.AssertReadAccessAllowed();


            var containingType = element?.GetContainingType();

            if (containingType == null)
                yield break;

            var names = AssetUtils.GetAllNamesFor(element).ToJetHashSet();
            foreach (var (usage, isPrefabModification) in GetUsages(sourceFile))
            {
                if (!names.Contains(usage.Name))
                    continue;
                var scriptReference = usage.ScriptReference;
                var guid = scriptReference.ExternalAssetGuid;

                var typeElement = AssetUtils.GetTypeElementFromScriptAssetGuid(element.GetSolution(), guid);
                if (typeElement == null || !typeElement.IsDescendantOf(containingType))
                    continue;

                yield return new UnityInspectorFindResult(sourceFile, element, usage, usage.Location, isPrefabModification);
            }
        }

        private IEnumerable<(InspectorVariableUsage usage, bool isPrefabModification)> GetUsages(IPsiSourceFile sourceFile)
        {
            var dataElement = myPointers.GetValueSafe(sourceFile)?.GetElement(sourceFile, Id) as AssetInspectorValuesDataElement;
            if (dataElement == null)
                yield break;

            foreach (var usage in dataElement.VariableUsages)
                yield return (usage, false);

            foreach (var (reference, modification) in dataElement.ImportedInspectorValues.Modifications)
            {
                var hierearchyElement = myHierarchyElementContainer.GetHierarchyElement(reference.LocalReference, true);
                Assertion.Assert(hierearchyElement != null, "hierearchyElement != null");
                if (!(hierearchyElement is IScriptComponentHierarchy scriptElement))
                    continue;
                yield return (new InspectorVariableUsage(reference.LocalReference, scriptElement.ScriptReference, reference.Name, modification.value ?? modification.objectReference), true);
            }
        }


        public LocalList<IPsiSourceFile> GetPossibleFilesWithUsage(IField element)
        {
            var result = new LocalList<IPsiSourceFile>();
            foreach (var name in AssetUtils.GetAllNamesFor(element))
            {
                foreach (var sourceFile in myNameToSourceFile.GetValues(name))
                {
                    result.Add(sourceFile);
                }
            }

            return result;
        }
    }
}