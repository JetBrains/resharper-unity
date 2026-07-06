#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    public static class SerializeReferenceProviderDiffUtils
    {
        //Merges one file's/assembly's type-to-interface contribution into the global index (the exact
        //seam UnitySerializedReferenceInfoIndex.Merge uses). Public so the incremental add/remove
        //(partial-class) lifecycle can be unit-tested without a full solution.
        public static void MergeTypeToInterfaces(IndexClassInfoDictionary classInfo,
            ClassMetaInfoDictionary? oldData, ClassMetaInfoDictionary? newData)
        {
            var diff = CalculateDiff(oldData, newData);
            ApplyDiff(classInfo, diff);
        }

        internal static List<TDIff> CalculateDiff<TDIff, TSetElement>(CountingSet<TSetElement>? oldSet,
            CountingSet<TSetElement>? newSet, Func<TSetElement, DiffType, int, TDIff> createDiff)
        {
            if (oldSet == null && newSet == null)
                return new List<TDIff>(0);

            if (oldSet == null && newSet != null) //all data was added
                return newSet.Select(pair => createDiff(pair.Key, DiffType.Added, pair.Value)).ToList();

            if (oldSet != null && newSet == null) //all data was removed
                return oldSet.Select(pair => createDiff(pair.Key, DiffType.Removed, pair.Value)).ToList();


            List<TDIff> result = new(Math.Max(oldSet!.Count, newSet!.Count));

            //check if new elements were added or amount of existed changed
            foreach (var (newElementId, newCount) in newSet)
            {
                //newCount couldn't be <= 0 - in this case it wouldn't exists
                var oldCount = oldSet.GetCount(newElementId);

                if (oldCount == newCount) //nothing changed
                    continue;

                //amount of elements changed, could be a new element
                var countDiff = newCount - oldCount;

                result.Add(createDiff(
                    newElementId,
                    countDiff > 0 ? DiffType.Added : DiffType.Removed,
                    Math.Abs(countDiff)
                ));
            }


            //check if oldElements were removed
            foreach (var (oldElementId, oldCount) in oldSet)
            {
                var newCount = newSet.GetCount(oldElementId);
                if (newCount == 0) //check only removed elements
                {
                    result.Add(createDiff(
                        oldElementId,
                        DiffType.Removed,
                        oldCount
                    ));
                }
            }


            return result;
        }

        private static void ApplyDiff(this CountingSet<ElementId> set, List<CountingSetDiff> diff)
        {
            foreach (var chunk in diff)
            {
                switch (chunk.DiffType)
                {
                    case DiffType.Added:
                        set.Add(chunk.Id, chunk.Count);
                        break;
                    case DiffType.Removed:
                        set.Remove(chunk.Id, chunk.Count);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static ClassMetaInfoDiff CalculateDiff(ClassMetaInfo? oldInfo, ClassMetaInfo? newInfo)
        {
            if (oldInfo == null && newInfo == null)
                return ClassMetaInfoDiff.EmptyDiff;

            //empty name means unresolved; empty <-> real is a name resolution, not a different class
            if (oldInfo != null && newInfo != null
                && !string.IsNullOrEmpty(oldInfo.ClassName)
                && !string.IsNullOrEmpty(newInfo.ClassName)
                && oldInfo.ClassName != newInfo.ClassName)
            {
                throw new ArgumentException(
                    $"Building diff for different classes old:'{oldInfo.ClassName}', new:'{newInfo.ClassName}'");
            }


            var superClassesDiff = CalculateDiff(oldInfo?.SuperClasses, newInfo?.SuperClasses, CreateDiff);
            var serializeReferenceHoldersDiff =
                CalculateDiff(oldInfo?.SerializeReferenceHolders, newInfo?.SerializeReferenceHolders, CreateDiff);
            var typeParametersDiff = CalculateDiff(oldInfo?.TypeParameters, newInfo?.TypeParameters);

            var className = !string.IsNullOrEmpty(oldInfo?.ClassName) ? oldInfo!.ClassName
                : !string.IsNullOrEmpty(newInfo?.ClassName) ? newInfo!.ClassName
                : string.Empty;

            return new ClassMetaInfoDiff(className, superClassesDiff,
                serializeReferenceHoldersDiff, typeParametersDiff);
        }

        private static CountingSetDiff CreateDiff(ElementId id, DiffType diffType, int count)
        {
            return new(id, diffType, count);
        }

        private static List<TypeParametersSetDiff> CalculateDiff(Dictionary<ElementId, TypeParameter>? oldDict,
            Dictionary<ElementId, TypeParameter>? newDict)
        {
            if (oldDict == null && newDict == null)
                return new List<TypeParametersSetDiff>();

            if (oldDict == null && newDict != null) //all data was added
                return newDict.Select(pair => new TypeParametersSetDiff(pair.Value.ElementId, DiffType.Added,
                        pair.Value.Index, pair.Value.Name,
                        CalculateDiff(null, pair.Value.SerializeReferenceHolders, CreateDiff),
                        isNewDeclaration: true))
                    .ToList();

            if (oldDict != null && newDict == null) //all data was removed
                return oldDict.Select(pair => new TypeParametersSetDiff(pair.Value.ElementId, DiffType.Removed,
                        pair.Value.Index, pair.Value.Name
                        , new List<CountingSetDiff>()))
                    .ToList();


            List<TypeParametersSetDiff> result = new(Math.Max(oldDict!.Count, newDict!.Count));

            //check if new elements were added or amount of existed changed
            foreach (var (newId, newParameter) in newDict)
            {
                var contains = oldDict.TryGetValue(newId, out var oldParameter);

                var serializeReferenceHoldersDiff = CalculateDiff(oldParameter?.SerializeReferenceHolders,
                    newParameter.SerializeReferenceHolders,
                    CreateDiff);

                if (!contains || serializeReferenceHoldersDiff.Count > 0) //nothing changed
                    result.Add(new TypeParametersSetDiff(
                        newParameter.ElementId,
                        DiffType.Added,
                        newParameter.Index,
                        newParameter.Name,
                        serializeReferenceHoldersDiff,
                        isNewDeclaration: !contains));
            }


            //check if oldElements were removed
            foreach (var (oldId, oldParameter) in oldDict)
            {
                var contains = newDict.TryGetValue(oldId, out _);
                if (!contains) //check only removed elements
                    result.Add(new TypeParametersSetDiff(
                        oldParameter.ElementId,
                        DiffType.Removed,
                        oldParameter.Index,
                        oldParameter.Name,
                        new List<CountingSetDiff>()));
            }

            return result;
        }

        private static void ApplyDiff(this IndexClassInfo data,
            ClassMetaInfoDiff diff,
            IndexClassInfoDictionary classInfo,
            ElementId diffElementId)
        {
            var dataNameIsEmpty = string.IsNullOrEmpty(data.ClassName);
            if (!dataNameIsEmpty && diff.ClassName != data.ClassName)
                throw new ArgumentException(
                    $"Applying diff to wrong class diff.ClassName:'{diff.ClassName}', metaInfo:'{data.ClassName}'");

            if (dataNameIsEmpty && !string.IsNullOrEmpty(diff.ClassName))
                data.ReplaceEmptyName(diff.ClassName);

            if (diff.IsEmpty())
                return;

            data.SuperClasses.ApplyDiff(diff.SuperClassesDiff);
            data.SerializeReferenceHolders.ApplyDiff(diff.SerializeReferenceHoldersDiff);

            foreach (var superClassesDiff in diff.SuperClassesDiff)
            {
                var superClassId = superClassesDiff.Id;
                if (classInfo.TryGetValue(superClassId, out var superClassInfo))
                {
                    switch (superClassesDiff.DiffType)
                    {
                        case DiffType.Added:
                            superClassInfo.Inheritors.Add(diffElementId);
                            break;
                        case DiffType.Removed:
                            superClassInfo.Inheritors.Remove(diffElementId);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    switch (superClassesDiff.DiffType)
                    {
                        case DiffType.Added:
                            var indexClassInfo =
                                new IndexClassInfo(string.Empty); //TODO - maybe names are useless - just for debug
                            indexClassInfo.Inheritors.Add(diffElementId);
                            classInfo.Add(superClassId, indexClassInfo);
                            break;
                        case DiffType.Removed: //Super class already removed
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        internal static List<TypeToInterfaceDiff> CalculateDiff(ClassMetaInfoDictionary? oldData,
            ClassMetaInfoDictionary? newData)
        {
            if (oldData == null && newData == null)
                return new List<TypeToInterfaceDiff>(0);

            if (oldData == null && newData != null) //all data was added
                return newData.Select(pair =>
                    new TypeToInterfaceDiff(pair.Key, CalculateDiff(null, pair.Value), DiffType.Added,
                        isNewDeclaration: true)).ToList();

            if (oldData != null && newData == null) //all data was removed
                return oldData.Select(pair =>
                    new TypeToInterfaceDiff(pair.Key, CalculateDiff(pair.Value, null), DiffType.Removed)).ToList();


            List<TypeToInterfaceDiff> result = new(Math.Max(oldData!.Count, newData!.Count));

            //check if new elements were added or changed
            foreach (var (newElementId, newMetaInfo) in newData)
            {
                oldData.TryGetValue(newElementId, out var oldMetaInfo);
                var typeToInterfaceDiffs = CalculateDiff(oldMetaInfo, newMetaInfo);
                result.Add(new TypeToInterfaceDiff(newElementId, typeToInterfaceDiffs, DiffType.Added,
                    isNewDeclaration: oldMetaInfo == null));
            }


            //check if oldElements were removed
            foreach (var (oldElementId, oldInfo) in oldData)
            {
                if (!newData.ContainsKey(oldElementId)) //check only removed elements
                {
                    result.Add(new TypeToInterfaceDiff(oldElementId, CalculateDiff(oldInfo, null), DiffType.Removed));
                }
            }


            return result;
        }


        internal static void ApplyDiff(IndexClassInfoDictionary classInfo, List<TypeToInterfaceDiff> diffs)
        {
            foreach (var diff in diffs)
            {
                var classMetaInfoDiff = diff.MetaInfoDiff;
                if (diff.DiffType == DiffType.None && classMetaInfoDiff.IsEmpty())
                    continue;

                var diffElementId = diff.ElementId;

                if (classInfo.TryGetValue(diffElementId, out var metaInfo))
                {
                    metaInfo.ApplyDiff(classMetaInfoDiff, classInfo, diffElementId);
                    if (metaInfo.ApplyDeclarationDelta(diff.DiffType, diff.IsNewDeclaration))
                        classInfo.Remove(diffElementId);
                }
                else
                {
                    //Assertion.Require(!string.IsNullOrEmpty(classMetaInfoDiff.ClassName));
                    Assertion.Require(!classMetaInfoDiff.IsEmpty() || diff.DiffType == DiffType.Added, $"!classMetaInfoDiff.IsEmpty() || diff.DiffType == DiffType.Added [isEmpty:{classMetaInfoDiff.IsEmpty()}, diff.Type:{diff.DiffType}]");

                    metaInfo = new IndexClassInfo(classMetaInfoDiff.ClassName);
                    metaInfo.ApplyDiff(classMetaInfoDiff, classInfo, diffElementId);
                    metaInfo.ApplyDeclarationDelta(diff.DiffType, diff.IsNewDeclaration);
                    classInfo.Add(diffElementId, metaInfo);
                }

                ProcessTypeParametersDiff(classInfo, classMetaInfoDiff);
            }
        }

        private static void ProcessTypeParametersDiff(IndexClassInfoDictionary classInfo,
            ClassMetaInfoDiff classMetaInfoDiff)
        {
            foreach (var diff in classMetaInfoDiff.TypeParametersSetDiffs)
            {
                var diffElementId = diff.Id;

                if (classInfo.TryGetValue(diffElementId, out var metaInfo))
                {
                    //type parameter - removed or updated; drop only when the last declaring file removes it
                    if (metaInfo.ApplyDeclarationDelta(diff.DiffType, diff.IsNewDeclaration))
                        classInfo.Remove(diffElementId);
                    else if (diff.DiffType != DiffType.Removed)
                        metaInfo.SerializeReferenceHolders.ApplyDiff(diff.SerializeReferenceHoldersDiff);
                }
                else
                {
                    // Assertion.Require(!string.IsNullOrEmpty(diff.ClassName));
                    Assertion.Require(diff.DiffType == DiffType.Added, $"diff.DiffType == DiffType.Added [diffType:{diff.DiffType}]");

                    metaInfo = new IndexClassInfo(diff.ClassName, true);
                    metaInfo.ApplyDeclarationDelta(diff.DiffType, diff.IsNewDeclaration);
                    metaInfo.SerializeReferenceHolders.ApplyDiff(diff.SerializeReferenceHoldersDiff);
                    classInfo.Add(diffElementId, metaInfo);
                }
            }
        }

        private static void UnionWith(this TypeParameter typeParameter, TypeParameter other)
        {
            Assertion.Require(typeParameter.Index == other.Index, "typeParameter.Index == other.Index");
            Assertion.Require(typeParameter.ElementId == other.ElementId, "typeParameter.ElementId == other.ElementId");
            Assertion.Require(typeParameter.Name == other.Name, "typeParameter.Name == other.Name");
            typeParameter.SerializeReferenceHolders.UnionWith(other.SerializeReferenceHolders);
        }

        public static void UnionWith(this Dictionary<ElementId, TypeParameter> dict,
            Dictionary<ElementId, TypeParameter> other)
        {
            foreach (var (key, value) in other)
            {
                if (dict.TryGetValue(key, out var existedValue))
                    existedValue.UnionWith(value);
                else
                    dict.Add(key, value);
            }
        }

        public static void UnionWith<T>(this CountingSet<T> set, CountingSet<T> other)
        {
            foreach (var (key, value) in other)
            {
                set.Add(key, value);
            }
        }

        internal static void ApplyDiff(IndexClassInfoDictionary classInfo, List<TypeParameterResolvesDiff> resolvesDiff)
        {
            foreach (var diff in resolvesDiff)
            {
                var resolution = diff.TypeParameterResolve;
                
                Assertion.Require(resolution != null, "resolution != null");

                var openTypeExists = classInfo.TryGetValue(resolution.OpenTypeId, out var openTypeInfo);
                var resolvedTypeExists = classInfo.TryGetValue(resolution.ResolvedTypeId, out var resolvedInfo);

                switch (diff.DiffType)
                {
                    case DiffType.Removed:
                        if (openTypeExists)
                        {
                            Assertion.Require(openTypeInfo != null, $"openTypeInfo != null | {diff.DiffType}");
                            openTypeInfo.Inheritors.Remove(resolution.ResolvedTypeId);
                        }

                        if (resolvedTypeExists)
                        {
                            Assertion.Require(resolvedInfo != null, $"resolvedInfo != null | {diff.DiffType}");
                            resolvedInfo.SuperClasses.Remove(resolution.OpenTypeId);
                        }
                        break;
                    case DiffType.Added:
                        if (!openTypeExists)
                        {
                            openTypeInfo = new IndexClassInfo(resolution.ResolutionString, true);
                            classInfo.Add(resolution.OpenTypeId, openTypeInfo);
                        }
                        
                        Assertion.Require(openTypeInfo != null, $"openTypeInfo != null | {diff.DiffType}");
                        openTypeInfo.Inheritors.Add(resolution.ResolvedTypeId);


                        if (!resolvedTypeExists)
                        {
                            resolvedInfo = new IndexClassInfo(string.Empty, true);
                            
                            //TODO: remove this extra validations after fix
                            var resolutionResolvedTypeId = resolution.ResolvedTypeId;
                            var alreadyHasSameKey =
                                classInfo.TryGetValue(resolutionResolvedTypeId, out var doubleCheckVal);
                            Assertion.Require(!alreadyHasSameKey, $"Dictionary {nameof(classInfo)} already has this key:{resolutionResolvedTypeId}, value:{doubleCheckVal}, attemption to add {resolvedInfo}");

                            if(!alreadyHasSameKey)
                                classInfo.Add(resolutionResolvedTypeId, resolvedInfo);
                        }

                        Assertion.Require(resolvedInfo != null, $"resolvedInfo != null | {diff.DiffType}");
                        resolvedInfo.SuperClasses.Add(resolution.OpenTypeId);
                        
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}