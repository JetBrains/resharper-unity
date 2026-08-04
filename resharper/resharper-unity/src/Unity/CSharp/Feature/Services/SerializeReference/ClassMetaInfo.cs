#nullable enable
using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    internal class ClassMetaInfoDictionary : Dictionary<ElementId, ClassMetaInfo>
    {
        public ClassMetaInfoDictionary()
        {
        }

        public ClassMetaInfoDictionary(ClassMetaInfoDictionary typeToInterfaces)
            : base(typeToInterfaces)
        {
        }
    }

    internal class IndexClassInfoDictionary : Dictionary<ElementId, IndexClassInfo>
    {
        public IndexClassInfoDictionary()
        {
        }

        public IndexClassInfoDictionary(IndexClassInfoDictionary dictionary)
            : base(dictionary)
        {
        }
    }

    internal class IndexClassInfo
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<IndexClassInfo>();

        public IndexClassInfo(string className, bool isTypeParameter = false)
            : this(className, isTypeParameter, new CountingSet<ElementId>(), new CountingSet<ElementId>(),
                new CountingSet<ElementId>())
        {
        }

        private IndexClassInfo(string className, bool isTypeParameter, CountingSet<ElementId> superClasses,
            CountingSet<ElementId> inheritors, CountingSet<ElementId> serializeReferenceHolders)
        {
            ClassName = className;
            SuperClasses = superClasses;
            Inheritors = inheritors;
            SerializeReferenceHolders = serializeReferenceHolders;
            IsTypeParameter = isTypeParameter;
        }

        internal void ReplaceEmptyName(string newName)
        {
            Assertion.Require(string.IsNullOrEmpty(ClassName));
            ClassName = newName;
        }

        public string ClassName { get; private set; }

        public bool IsTypeParameter { get; }
        public CountingSet<ElementId> SuperClasses { get; } // or classes which resolves into this type

        public CountingSet<ElementId> Inheritors { get; } //or resolves

        public CountingSet<ElementId> SerializeReferenceHolders { get; }

        //partial classes: several files can declare the same type. Count declaring files so the entry
        //survives until the last one is removed. Placeholders (referenced-only super classes / type
        //parameters) stay at 0 and live while their relationship sets are non-empty.
        private int myDeclarationCount;

        //applies a declaration add/remove; returns true when the entry is no longer referenced and can be dropped
        internal bool ApplyDeclarationDelta(DiffType diffType, bool isNewDeclaration)
        {
            if (diffType == DiffType.Removed)
            {
                //clamp at 0: an unbalanced removal must not under-count a later legitimate declaration
                if (myDeclarationCount > 0)
                    myDeclarationCount--;
                else
                    ourLogger.Warn($"Unbalanced declaration removal for '{ClassName}', count is already 0");
                return CanBeDropped();
            }

            if (isNewDeclaration)
                myDeclarationCount++;
            return false;
        }

        internal bool CanBeDropped()
        {
            return myDeclarationCount <= 0 && IsEmpty();
        }

        public bool IsEmpty()
        {
            return SuperClasses.Count == 0 && SerializeReferenceHolders.Count == 0 && Inheritors.Count == 0;
        }

        public override string ToString()
        {
            return $"{nameof(ClassName)}: {ClassName}, {nameof(IsTypeParameter)}: {IsTypeParameter}, {nameof(SuperClasses)}: {SuperClasses.Count}, {nameof(Inheritors)}: {Inheritors.Count}, {nameof(SerializeReferenceHolders)}: {SerializeReferenceHolders.Count}";
        }
    }

    public class TypeParameter
    {
        public TypeParameter(ElementId elementId, string name, int index,
            CountingSet<ElementId> serializeReferenceHolders)
        {
            ElementId = elementId;
            Name = name;
            Index = index;
            SerializeReferenceHolders = serializeReferenceHolders;
        }

        public ElementId ElementId { get; }
        public string Name { get; }
        public int Index { get; }
        public CountingSet<ElementId> SerializeReferenceHolders { get; }

        public bool Equals(TypeParameter other)
        {
            return ElementId.Equals(other.ElementId) && Name == other.Name && Index == other.Index &&
                   SerializeReferenceHolders.Equals(other.SerializeReferenceHolders);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TypeParameter)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ElementId.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Index;
                hashCode = (hashCode * 397) ^ SerializeReferenceHolders.GetHashCode();
                return hashCode;
            }
        }
    }

    public class ClassMetaInfo
    {
        public ClassMetaInfo(string className)
            : this(className
                , new CountingSet<ElementId>()
                , new CountingSet<ElementId>(), new Dictionary<ElementId, TypeParameter>())
        {
        }

        public ClassMetaInfo(string className
            , CountingSet<ElementId> superClasses
            , CountingSet<ElementId> serializeReferenceHolders, Dictionary<ElementId, TypeParameter> typeParameters)
        {
            ClassName = className;
            SuperClasses = superClasses;
            SerializeReferenceHolders = serializeReferenceHolders;
            TypeParameters = typeParameters;
        }

        public string ClassName { get; }

        public CountingSet<ElementId> SuperClasses { get; }
        public CountingSet<ElementId> SerializeReferenceHolders { get; }
        public Dictionary<ElementId, TypeParameter> TypeParameters { get; }

        public void UnionWith(ClassMetaInfo other)
        {
            SuperClasses.UnionWith(other.SuperClasses);
            SerializeReferenceHolders.UnionWith(other.SerializeReferenceHolders);
            TypeParameters.UnionWith(other.TypeParameters);
        }

        public override string ToString()
        {
            return
                $"{nameof(ClassName)}: {ClassName}, {nameof(SuperClasses)}: {SuperClasses.Count}, {nameof(SerializeReferenceHolders)}: {SerializeReferenceHolders.Count}" +
                $", {nameof(TypeParameters)}: {TypeParameters.Count}";
        }
    }

    internal class TypeParameterResolve
    {
        public readonly ElementId OpenTypeId;
        public readonly string ResolutionString;
        public readonly ElementId ResolvedTypeId;

        public TypeParameterResolve(string resolutionString, ElementId openTypeId, ElementId resolvedTypeId)
        {
            ResolutionString = resolutionString;
            OpenTypeId = openTypeId;
            ResolvedTypeId = resolvedTypeId;
        }

        public bool Equals(TypeParameterResolve other)
        {
            return ResolutionString == other.ResolutionString && OpenTypeId.Equals(other.OpenTypeId) &&
                   ResolvedTypeId.Equals(other.ResolvedTypeId);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TypeParameterResolve)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ResolutionString.GetHashCode();
                hashCode = (hashCode * 397) ^ OpenTypeId.GetHashCode();
                hashCode = (hashCode * 397) ^ ResolvedTypeId.GetHashCode();
                return hashCode;
            }
        }
    }
}