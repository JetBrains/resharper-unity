#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Daemon.UsageChecking;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    internal enum DiffType
    {
        None,
        Added,
        Removed
    }

    internal readonly struct TypeParametersSetDiff
    {
        public readonly ElementId Id;
        public readonly DiffType DiffType;
        public readonly int Index;
        public readonly string ClassName;
        public readonly List<CountingSetDiff> SerializeReferenceHoldersDiff;
        //true when this file starts declaring the type parameter (was absent in the old data)
        public readonly bool IsNewDeclaration;

        public TypeParametersSetDiff(ElementId id, DiffType diffType, int index, string className,
            List<CountingSetDiff> serializeReferenceHoldersDiff, bool isNewDeclaration = false)
        {
            Id = id;
            DiffType = diffType;
            Index = index;
            ClassName = className;
            SerializeReferenceHoldersDiff = new List<CountingSetDiff>(serializeReferenceHoldersDiff);
            IsNewDeclaration = isNewDeclaration;
        }
    }

    internal readonly struct TypeParameterResolvesDiff
    {
        public readonly TypeParameterResolve TypeParameterResolve;
        public readonly DiffType DiffType;
        public readonly int Count;

        public TypeParameterResolvesDiff(TypeParameterResolve typeParameterResolve, DiffType diffType, int count)
        {
            TypeParameterResolve = typeParameterResolve;
            DiffType = diffType;
            Count = count;
        }
    }


    internal readonly struct CountingSetDiff
    {
        public readonly ElementId Id;
        public readonly DiffType DiffType;
        public readonly int Count;

        public CountingSetDiff(ElementId id, DiffType diffType, int count)
        {
            Id = id;
            DiffType = diffType;
            Count = count;
        }
    }

    internal readonly struct ClassMetaInfoDiff


    {
        public readonly List<CountingSetDiff> SuperClassesDiff;
        public readonly List<CountingSetDiff> SerializeReferenceHoldersDiff;
        public readonly List<TypeParametersSetDiff> TypeParametersSetDiffs;
        public readonly string ClassName;

        public ClassMetaInfoDiff(string className, List<CountingSetDiff> superClassesDiff,
            List<CountingSetDiff> serializeReferenceHoldersDiff, List<TypeParametersSetDiff> typeParametersSetDiffs)
        {
            SuperClassesDiff = superClassesDiff;
            SerializeReferenceHoldersDiff = serializeReferenceHoldersDiff;
            TypeParametersSetDiffs = typeParametersSetDiffs;
            ClassName = className;
        }

        public bool IsEmpty()
        {
            return SuperClassesDiff.Count == 0
                   && SerializeReferenceHoldersDiff.Count == 0
                   && TypeParametersSetDiffs.Count == 0;
        }


        public static ClassMetaInfoDiff EmptyDiff => new(string.Empty, new List<CountingSetDiff>(0),
            new List<CountingSetDiff>(0), new List<TypeParametersSetDiff>(0));
    }

    internal readonly struct TypeToInterfaceDiff
    {
        public readonly ElementId ElementId;
        public readonly ClassMetaInfoDiff MetaInfoDiff;
        public readonly DiffType DiffType;
        //true when this file starts declaring the type (was absent in the old data)
        public readonly bool IsNewDeclaration;


        public TypeToInterfaceDiff(ElementId elementId, ClassMetaInfoDiff metaInfoDiff, DiffType diffType,
            bool isNewDeclaration = false)
        {
            ElementId = elementId;
            MetaInfoDiff = metaInfoDiff;
            DiffType = diffType;
            IsNewDeclaration = isNewDeclaration;
        }
    }
}