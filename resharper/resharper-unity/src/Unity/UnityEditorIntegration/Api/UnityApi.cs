#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Caches;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    public enum SerializedFieldStatus
    {
        Unknown = 0,
        NonSerializedField = 1,
        SerializedField = 2
    }

    [SolutionComponent]
    public class UnityApi
    {
        // https://docs.unity3d.com/Documentation/Manual/script-Serialization.html
        private static readonly JetHashSet<IClrTypeName> ourUnityBuiltinSerializedFieldTypes = new JetHashSet<IClrTypeName>
        {
            KnownTypes.Vector2, KnownTypes.Vector3, KnownTypes.Vector4,
            KnownTypes.Vector2Int, KnownTypes.Vector3Int,
            KnownTypes.Rect, KnownTypes.RectInt, KnownTypes.RectOffset,
            KnownTypes.Quaternion,
            KnownTypes.Matrix4x4,
            KnownTypes.Color, KnownTypes.Color32,
            KnownTypes.LayerMask,
            KnownTypes.Bounds, KnownTypes.BoundsInt,
            KnownTypes.AnimationCurve,
            KnownTypes.Gradient,
            KnownTypes.GUIStyle,
            KnownTypes.SphericalHarmonicsL2,
            KnownTypes.LazyLoadReference
        };

        private readonly UnityVersion myUnityVersion;
        private readonly UnityTypeCache myUnityTypeCache;
        private readonly UnityTypesProvider myUnityTypesProvider;
        private readonly KnownTypesCache myKnownTypesCache;
        private readonly IUnitySerializedReferenceProvider mySerializedReferenceProvider;

        public UnityApi(UnityVersion unityVersion, UnityTypeCache unityTypeCache, UnityTypesProvider unityTypesProvider,
            KnownTypesCache knownTypesCache, IUnitySerializedReferenceProvider serializedReferenceProvider)
        {
            myUnityVersion = unityVersion;
            myUnityTypeCache = unityTypeCache;
            myUnityTypesProvider = unityTypesProvider;
            myKnownTypesCache = knownTypesCache;
            mySerializedReferenceProvider = serializedReferenceProvider;
        }

        public bool IsUnityType([NotNullWhen(true)] ITypeElement? type) =>
            type != null && myUnityTypeCache.IsUnityType(type);

        public static bool IsDotsImplicitlyUsedType([NotNullWhen(true)] ITypeElement? typeElement) =>
            IsDerivesFromSystemBase(typeElement)
            || IsDerivesFromISystem(typeElement)
            || IsDerivesFromIAspect(typeElement)
            || IsDerivesFromIComponentData(typeElement)
            || IsDerivesFromIJobEntity(typeElement)
            || typeElement.DerivesFrom(KnownTypes.IBaker);

        public static bool IsDerivesFromIJobEntity(ITypeElement? typeElement)
        {
            return typeElement.DerivesFrom(KnownTypes.IJobEntity);
        }

        public static bool IsDerivesFromIAspect(ITypeElement? typeElement)
        {
            return typeElement.DerivesFrom(KnownTypes.IAspect);
        }

        public static bool IsDerivesFromSystemBase(ITypeElement? typeElement)
        {
            return typeElement.DerivesFrom(KnownTypes.ComponentSystemBase);
        }

        public static bool IsDerivesFromISystem(ITypeElement? typeElement)
        {
            return typeElement.DerivesFrom(KnownTypes.ISystem);
        }

        public static bool IsDerivesFromIComponentData(ITypeElement? typeElement)
        {
            return typeElement.DerivesFrom(KnownTypes.IComponentData);
        }

        public static bool IsComponentLookup(ITypeElement? typeElement)
        {
            return typeElement?.GetClrName().Equals(KnownTypes.ComponentLookup) ?? false;
        }
        public static bool IsBaker(ITypeElement? typeElement)
        {
            return typeElement?.GetClrName().Equals(KnownTypes.Baker) ?? false;
        }
        public static bool IsSystemStateType(ITypeElement? typeElement)
        {
            return typeElement?.GetClrName().Equals(KnownTypes.SystemState) ?? false;
        }
        public static bool IsSystemAPI(ITypeElement? typeElement)
        {
            return typeElement?.GetClrName().Equals(KnownTypes.SystemAPI) ?? false;
        }

        public static bool IsRefRO(ITypeElement? typeElement)
        {
            return typeElement?.GetClrName().Equals(KnownTypes.RefRO) ?? false;
        }

        public static bool IsRefRW(ITypeElement? typeElement)
        {
            return typeElement?.GetClrName().Equals(KnownTypes.RefRW) ?? false;
        }

        // A serialised field cannot be abstract or generic, but a type declaration that will be serialised can be. This
        // method differentiates between a type declaration and a type usage. Consider renaming if we ever need to
        // expose stricter checking publicly
        public SerializedFieldStatus IsSerializableTypeDeclaration([NotNullWhen(true)] ITypeElement? type, bool useSwea = true) //TODO - use serializedRefProvider
        {
            // We only support type declarations in a project. We shouldn't get any other type
            if (type?.Module is IProjectPsiModule projectPsiModule)
            {
                var project = projectPsiModule.Project;
                return IsSerializableType(type, project, false, useSwea);
            }

            return SerializedFieldStatus.NonSerializedField;
        }

        private SerializedFieldStatus IsSerializableType([NotNullWhen(true)] ITypeElement? type, IProject project, bool isTypeUsage,
            bool useSwea = true,
            bool hasSerializeReference = false)
        {
            if (IsSerializableTypeSimpleCheck(type, project, isTypeUsage, hasSerializeReference))
                return SerializedFieldStatus.SerializedField;

            return mySerializedReferenceProvider.GetSerializableStatus(type, useSwea);
        }

        // NOTE: This method assumes that the type is not a descendant of UnityEngine.Object!
        private bool IsSerializableTypeSimpleCheck([NotNullWhen(true)] ITypeElement? type, IProject project, bool isTypeUsage,
            bool hasSerializeReference = false)
        {
            if (type is not (IStruct or IClass))
                return false;

            if (isTypeUsage)
            {
                // Type usage (e.g. field declaration) is stricter. Means it must be a concrete type with no type
                // parameters, unless the type usage is for [SerializeReference], which allows abstract types
                if (type is IModifiersOwner { IsAbstract: true } && !hasSerializeReference)
                    return false;

                // Unity 2020.1 allows fields to have generic types. It's currently undocumented, but there are no
                // limitations on the number of type parameters, or even nested type parameters. The base type needs to
                // be serializable, but type parameters don't (if a non-serializable type parameter is used as a field,
                // it just isn't serialised).
                // https://blogs.unity3d.com/2020/03/17/unity-2020-1-beta-is-now-available-for-feedback/
                var unityVersion = myUnityVersion.GetActualVersion(project);
                if (unityVersion < new Version(2020, 1) && type is ITypeParametersOwner typeParametersOwner &&
                    typeParametersOwner.TypeParameters.Count > 0)
                {
                    return false;
                }
            }

            if (type is IClass @class && @class.IsStaticClass())
                return false;

            // System.Dictionary is special cased and excluded. We can see this in UnitySerializationLogic.cs in the
            // reference source repo. It also excludes anything with a full name beginning "System.", which includes
            // "System.Version" (which is marked [Serializable]). However, it doesn't exclude string, int, etc.
            // TODO: Rewrite this whole section to properly mimic UnitySerializationLogic.cs
            var name = type.GetClrName();

            if (Equals(name, KnownTypes.SystemVersion) || Equals(name, PredefinedType.GENERIC_DICTIONARY_FQN))
                return false;

            if (name.FullName.StartsWith("System."))
                return false;

            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                var hasAttributeInstance = type.HasAttributeInstance(PredefinedType.SERIALIZABLE_ATTRIBUTE_CLASS, true);
                return hasAttributeInstance;
            }
        }

        public bool IsEventFunction([NotNullWhen(true)] IMethod? method) => method != null && GetUnityEventFunction(method) != null;

        public SerializedFieldStatus IsSerialisedField(IField? field, bool useSwea = true)
        {
            if (field == null || field.IsStatic || !field.IsField || field.IsReadonly)
                return SerializedFieldStatus.NonSerializedField;

            // [NonSerialized] trumps everything, even if there's a [SerializeField] as well
            if (field.HasAttributeInstance(PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS, false))
                return SerializedFieldStatus.NonSerializedField;

            var hasSerializeField = field.HasAttributeInstance(KnownTypes.SerializeField, false);
            var hasSerializeReference = field.HasAttributeInstance(KnownTypes.SerializeReference, false);

            // TODO - could be private (at least in Unity2019.4 up to 2021)
            if (field.GetAccessRights() != AccessRights.PUBLIC && !hasSerializeField && !hasSerializeReference)
                return SerializedFieldStatus.NonSerializedField;

            // Field is now either public or has [SerializeField] or [SerializeReference], so is likely to be serialised

            var containingType = field.ContainingType;
            if (!IsUnityType(containingType))
            {
                var isSerializableTypeDeclaration = IsSerializableTypeDeclaration(containingType, useSwea);
                if (isSerializableTypeDeclaration != SerializedFieldStatus.SerializedField)
                    return isSerializableTypeDeclaration;
            }

            return IsFieldTypeSerializable(field, hasSerializeReference, useSwea);
        }

        private SerializedFieldStatus IsFieldTypeSerializable(IProperty property, bool hasSerializeReference, bool useSwea)
        {
            // We need the project to get the current Unity version. this is only called for type usage (e.g. field
            // type), so it's safe to assume that the field is in a source file belonging to a project
            var project = (property.Module as IProjectPsiModule)?.Project;
            return project == null
                ? SerializedFieldStatus.NonSerializedField
                : IsFieldTypeSerializable(property.Type, project, hasSerializeReference, useSwea);
        }

        public SerializedFieldStatus IsFieldTypeSerializable(IField field, bool hasSerializeReference, bool useSwea)
        {
            // Rules for what field types can be serialised.
            // See https://docs.unity3d.com/ScriptReference/SerializeField.html

            // example: [SerializeField] public unsafe fixed byte MyByteBuff[3];
            if (field.IsFixedSizeBufferField()
                && field.Type is IPointerType pointerType
                && IsUnitySimplePredefined(pointerType.ElementType))
            {
                return SerializedFieldStatus.SerializedField;
            }

            // We need the project to get the current Unity version. this is only called for type usage (e.g. field
            // type), so it's safe to assume that the field is in a source file belonging to a project
            var project = (field.Module as IProjectPsiModule)?.Project;
            return project == null
                ? SerializedFieldStatus.NonSerializedField
                : IsFieldTypeSerializable(field.Type, project, hasSerializeReference, useSwea);
        }

        private SerializedFieldStatus IsFieldTypeSerializable([NotNullWhen(true)] IType? type, IProject project,
            bool hasSerializeReference, bool useSwea)
        {
            if (type is IArrayType { Rank: 1 } arrayType)
            {
                return IsSimpleFieldTypeSerializable(arrayType.ElementType, project, hasSerializeReference, useSwea);
            }

            if (type is IDeclaredType declaredType &&
                Equals(declaredType.GetClrName(), PredefinedType.GENERIC_LIST_FQN))
            {
                var substitution = declaredType.GetSubstitution();
                var typeParameter = declaredType.GetTypeElement()?.TypeParameters[0];
                if (typeParameter != null)
                {
                    var substitutedType = substitution.Apply(typeParameter);
                    if (substitutedType.IsTypeParameterType())
                        return SerializedFieldStatus.SerializedField;
                    return IsSimpleFieldTypeSerializable(substitutedType, project, hasSerializeReference, useSwea);
                }
            }

            return IsSimpleFieldTypeSerializable(type, project, hasSerializeReference, useSwea);
        }

        private SerializedFieldStatus IsSimpleFieldTypeSerializable(IType? type, IProject project,
            bool hasSerializeReference, bool useSwea)
        {
            // We include type parameter types (T) in this test, which Unity obviously won't. We treat them as
            // serialised fields rather than show false positive redundant attribute warnings, etc. Adding the test
            // here allows us to support T[] and List<T>

            if (type == null)
                return SerializedFieldStatus.NonSerializedField;

            if (IsUnitySimplePredefined(type))
                return SerializedFieldStatus.SerializedField;

            if (type.IsEnumType())
                return SerializedFieldStatus.SerializedField;

            if (IsUnityBuiltinType(type))
                return SerializedFieldStatus.SerializedField;

            if (type.GetTypeElement().DerivesFrom(KnownTypes.Object))
                return SerializedFieldStatus.SerializedField;

            if (type.IsTypeParameterType())
                return SerializedFieldStatus.SerializedField;

            return IsSerializableType(type.GetTypeElement(), project, true, useSwea, hasSerializeReference);
        }

        private static bool IsUnitySimplePredefined(IType type)
        {
            return type.IsSimplePredefined() && !Equals(((IDeclaredType)type).GetClrName(), PredefinedType.DECIMAL_FQN);
        }

        // An auto property can have [field: SerializeField] which makes the backing field a seralised field, albeit
        // with a weird name. The auto property must be writable, or the backing field is generated as readonly, which
        // isn't serialisable (so not true for getter only or init setter only properties)
        public SerializedFieldStatus IsSerialisedAutoProperty(IProperty? property, bool useSwea) //TODO - probably update it as well
        {
            if (property is not { IsAuto: true, IsWritable: true, IsStatic: false })
                return SerializedFieldStatus.NonSerializedField;

            var hasSerializeField = property.HasFieldAttribute(KnownTypes.SerializeField);
            var hasSerializeReference = property.HasFieldAttribute(KnownTypes.SerializeReference);

            if (!hasSerializeField && !hasSerializeReference)
                return SerializedFieldStatus.NonSerializedField;

            var containingType = property.ContainingType;
            if (!IsUnityType(containingType))
            {
                // if (IsSerializableTypeDeclaration(containingType, useSwea) != SerializedFieldStatus.SerializedField)//TODO != SerializedField, maybe is not the best solution
                //     return SerializedFieldStatus.NonSerializedField;
                var isSerializableTypeDeclaration = IsSerializableTypeDeclaration(containingType, useSwea);
                if (isSerializableTypeDeclaration != SerializedFieldStatus.SerializedField)
                    return isSerializableTypeDeclaration;
            }

            return IsFieldTypeSerializable(property, hasSerializeReference, useSwea);
        }

        // Best effort attempt at preventing false positives for type members that are actually being used inside a
        // scene. We don't have enough information to do this by name, so we'll mark all potential event handlers as
        // implicitly used by Unity
        // See https://github.com/Unity-Technologies/UnityCsReference/blob/02f8e8ca594f156dd6b2088ad89451143ca1b87e/Editor/Mono/Inspector/UnityEventDrawer.cs#L397
        //
        // Unity Editor will only list public methods, but will invoke any method, even if it's private.
        public bool IsPotentialEventHandler([NotNullWhen(true)] IMethod? method, bool isFindUsages = true)
        {
            if (method == null || !method.ReturnType.IsVoid())
                return false;

            // Type.GetMethods() returns public instance methods only
            if (method.GetAccessRights() != AccessRights.PUBLIC && !isFindUsages|| method.IsStatic)
                return false;

            return IsUnityType(method.ContainingType) &&
                   !method.HasAttributeInstance(PredefinedType.OBSOLETE_ATTRIBUTE_CLASS, true);
        }

        public bool IsPotentialEventHandler([NotNullWhen(true)] IProperty? property, bool isFindUsages = true) =>
            IsPotentialEventHandler(property?.Setter, isFindUsages);

        public IEnumerable<UnityEventFunction> GetEventFunctions(ITypeElement type, Version unityVersion)
        {
            var types = myUnityTypesProvider.Types;
            unityVersion = types.NormaliseSupportedVersion(unityVersion);
            foreach (var unityType in UnityTypeUtils.GetBaseUnityTypes(myUnityTypesProvider, type, unityVersion, myKnownTypesCache))
            {
                foreach (var function in unityType.GetEventFunctions(unityVersion))
                    yield return function;
            }
        }

        public UnityEventFunction? GetUnityEventFunction(IMethod method) => GetUnityEventFunction(method, out _);

        public UnityEventFunction? GetUnityEventFunction(IMethod method, out MethodSignatureMatch match)
        {
            Assertion.Assert(method.IsValid(), "DeclaredElement is not valid");
            match = MethodSignatureMatch.NoMatch;

            if (method.Module is not IProjectPsiModule projectPsiModule)
                return null;

            var unityVersion = GetNormalisedActualVersion(projectPsiModule.Project);
            return GetUnityEventFunction(method, unityVersion, out match);
        }

        public UnityEventFunction? GetUnityEventFunction(IMethod method, Version unityVersion,
            out MethodSignatureMatch match)
        {
            match = MethodSignatureMatch.NoMatch;

            var containingType = method.ContainingType;
            if (containingType == null) return null;

            foreach (var type in UnityTypeUtils.GetBaseUnityTypes(containingType, unityVersion, myUnityTypesProvider, myKnownTypesCache))
            {
                foreach (var function in type.GetEventFunctions(unityVersion))
                {
                    match = function.Match(method);
                    if (function.Match(method) != MethodSignatureMatch.NoMatch)
                        return function;
                }
            }

            return null;
        }

        public Version GetNormalisedActualVersion(IProject project) =>
            myUnityTypesProvider.Types.NormaliseSupportedVersion(myUnityVersion.GetActualVersion(project));

        private static bool IsUnityBuiltinType(IType type)
        {
            return type is IDeclaredType declaredType &&
                   ourUnityBuiltinSerializedFieldTypes.Contains(declaredType.GetClrName());
        }
    }
}
