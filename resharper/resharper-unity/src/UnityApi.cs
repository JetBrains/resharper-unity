using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
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
            KnownTypes.GUIStyle
        };

        private readonly UnityVersion myUnityVersion;
        private readonly KnownTypesCache myKnownTypesCache;
        private readonly Lazy<UnityTypes> myTypes;

        public UnityApi(UnityVersion unityVersion, KnownTypesCache knownTypesCache)
        {
            myUnityVersion = unityVersion;
            myKnownTypesCache = knownTypesCache;
            myTypes = Lazy.Of(() =>
            {
                var apiXml = new ApiXml();
                return apiXml.LoadTypes();
            }, true);
        }

        [NotNull]
        private IEnumerable<UnityType> GetBaseUnityTypes([CanBeNull] ITypeElement type)
        {
            if (type?.Module is IProjectPsiModule projectPsiModule)
            {
                var unityVersion = myUnityVersion.GetActualVersion(projectPsiModule.Project);
                return GetBaseUnityTypes(type, unityVersion);
            }
            return EmptyArray<UnityType>.Instance;
        }

        [NotNull]
        private IEnumerable<UnityType> GetBaseUnityTypes([NotNull] ITypeElement type, Version unityVersion)
        {
            var types = myTypes.Value;
            unityVersion = types.NormaliseSupportedVersion(unityVersion);
            return GetBaseUnityTypes(types, type, unityVersion);
        }

        public bool IsUnityType([CanBeNull] ITypeElement type)
        {
            return GetBaseUnityTypes(type).Any();
        }

        public bool IsComponentSystemType([CanBeNull] ITypeElement typeElement)
        {
            // This covers ComponentSystem, JobComponentSystem and SystemBase
            return typeElement.DerivesFrom(KnownTypes.ComponentSystemBase);
        }

        // A serialised field cannot be abstract or generic, but a type declaration that will be serialised can be. This
        // method differentiates between a type declaration and a type usage. Consider renaming if we ever need to
        // expose stricter checking publicly
        public bool IsSerializableTypeDeclaration([CanBeNull] ITypeElement type)
        {
            // We only support type declarations in a project. We shouldn't get any other type
            if (type?.Module is IProjectPsiModule projectPsiModule)
            {
                var project = projectPsiModule.Project;
                return IsSerializableType(type, project, false);
            }

            return false;
        }

        // NOTE: This method assumes that the type is not a descendant of UnityEngine.Object!
        private bool IsSerializableType([CanBeNull] ITypeElement type, [NotNull] IProject project, bool isTypeUsage,
            bool hasSerializeReference = false)
        {
            if (!(type is IStruct || type is IClass))
                return false;

            if (isTypeUsage)
            {
                // Type usage (e.g. field declaration) is stricter. Means it must be a concrete type with no type
                // parameters, unless the type usage is for [SerializeReference], which allows abstract types
                if (type is IModifiersOwner modifiersOwner && modifiersOwner.IsAbstract && !hasSerializeReference)
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

            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                return type.HasAttributeInstance(PredefinedType.SERIALIZABLE_ATTRIBUTE_CLASS, true);
        }

        public bool IsEventFunction([CanBeNull] IMethod method)
        {
            return method != null && GetUnityEventFunction(method) != null;
        }

        public bool IsSerialisedField([CanBeNull] IField field)
        {
            if (field == null || field.IsStatic || field.IsConstant || field.IsReadonly)
                return false;

            var containingType = field.GetContainingType();
            if (!IsUnityType(containingType) && !IsSerializableTypeDeclaration(containingType))
                return false;

            // [NonSerialized] trumps everything, even if there's a [SerializeField] as well
            if (field.HasAttributeInstance(PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS, false))
                return false;

            var hasSerializeReference = field.HasAttributeInstance(KnownTypes.SerializeReference, false);
            if (field.GetAccessRights() != AccessRights.PUBLIC
                && !field.HasAttributeInstance(KnownTypes.SerializeField, false)
                && !hasSerializeReference)
            {
                return false;
            }

            // We need the project to get the current Unity version. this is only called for type usage (e.g. field
            // type), so it's safe to assume that the field is in a source file belonging to a project
            var project = (field.Module as IProjectPsiModule)?.Project;

            // Rules for what field types can be serialised.
            // See https://docs.unity3d.com/ScriptReference/SerializeField.html
            return project != null &&
                   (IsSimpleSerialisedFieldType(field.Type, project, hasSerializeReference) ||
                    IsSerialisedFieldContainerType(field.Type, project, hasSerializeReference));
        }

        private bool IsSimpleSerialisedFieldType([CanBeNull] IType type, [NotNull] IProject project, bool hasSerializeReference)
        {
            // We include type parameter types (T) in this test, which Unity obviously won't. We treat them as
            // serialised fields rather than show false positive redundant attribute warnings, etc. Adding the test
            // here allows us to support T[] and List<T>
            return type != null && (type.IsSimplePredefined()
                                    || type.IsEnumType()
                                    || IsUnityBuiltinType(type as IDeclaredType)
                                    || type.GetTypeElement().DerivesFrom(KnownTypes.Object)
                                    || IsSerializableType(type.GetTypeElement(), project, true, hasSerializeReference)
                                    || type.IsTypeParameterType());
        }

        private bool IsSerialisedFieldContainerType([CanBeNull] IType type, [NotNull] IProject project, bool hasSerializeReference)
        {
            if (type is IArrayType arrayType && arrayType.Rank == 1 &&
                IsSimpleSerialisedFieldType(arrayType.ElementType, project, hasSerializeReference))
            {
                return true;
            }

            if (type is IDeclaredType declaredType &&
                Equals(declaredType.GetClrName(), PredefinedType.GENERIC_LIST_FQN))
            {
                var substitution = declaredType.GetSubstitution();
                var typeParameter = declaredType.GetTypeElement()?.TypeParameters[0];
                if (typeParameter != null)
                {
                    var substitutedType = substitution.Apply(typeParameter);
                    return substitutedType.IsTypeParameterType() ||
                           IsSimpleSerialisedFieldType(substitutedType, project, hasSerializeReference);
                }
            }

            return false;
        }

        // Best effort attempt at preventing false positives for type members that are actually being used inside a
        // scene. We don't have enough information to do this by name, so we'll mark all potential event handlers as
        // implicitly used by Unity
        // See https://github.com/Unity-Technologies/UnityCsReference/blob/02f8e8ca594f156dd6b2088ad89451143ca1b87e/Editor/Mono/Inspector/UnityEventDrawer.cs#L397
        //
        // Unity Editor will only list public methods, but will invoke any method, even if it's private.
        public bool IsPotentialEventHandler([CanBeNull] IMethod method, bool isFindUsages = true)
        {
            if (method == null || !method.ReturnType.IsVoid())
                return false;

            // Type.GetMethods() returns public instance methods only
            if (method.GetAccessRights() != AccessRights.PUBLIC && !isFindUsages|| method.IsStatic)
                return false;

            return IsUnityType(method.GetContainingType()) &&
                   !method.HasAttributeInstance(PredefinedType.OBSOLETE_ATTRIBUTE_CLASS, true);
        }

        public bool IsPotentialEventHandler([CanBeNull] IProperty property, bool isFindUsages = true)
        {
            return IsPotentialEventHandler(property?.Setter, isFindUsages);
        }

        public IEnumerable<UnityEventFunction> GetEventFunctions(ITypeElement type, Version unityVersion)
        {
            var types = myTypes.Value;
            unityVersion = types.NormaliseSupportedVersion(unityVersion);
            foreach (var unityType in GetBaseUnityTypes(types, type, unityVersion))
            {
                foreach (var function in unityType.GetEventFunctions(unityVersion))
                    yield return function;
            }
        }

        public UnityEventFunction GetUnityEventFunction([NotNull] IMethod method)
        {
            return GetUnityEventFunction(method, out _);
        }

        public UnityEventFunction GetUnityEventFunction([NotNull] IMethod method, out MethodSignatureMatch match)
        {
            Assertion.Assert(method.IsValid(), "DeclaredElement is not valid");
            match = MethodSignatureMatch.NoMatch;

            if (!(method.Module is IProjectPsiModule projectPsiModule))
                return null;

            var unityVersion = GetNormalisedActualVersion(projectPsiModule.Project);
            return GetUnityEventFunction(method, unityVersion, out match);
        }

        public UnityEventFunction GetUnityEventFunction([NotNull] IMethod method, Version unityVersion,
                                                        out MethodSignatureMatch match)
        {
            match = MethodSignatureMatch.NoMatch;

            var containingType = method.GetContainingType();
            if (containingType == null) return null;

            foreach (var type in GetBaseUnityTypes(containingType, unityVersion))
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

        public Version GetNormalisedActualVersion(IProject project)
        {
            return myTypes.Value.NormaliseSupportedVersion(myUnityVersion.GetActualVersion(project));
        }

        private IEnumerable<UnityType> GetBaseUnityTypes(UnityTypes types, ITypeElement type, Version normalisedVersion)
        {
            return types.Types.Where(t =>
            {
                using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                    return t.SupportsVersion(normalisedVersion) && type.IsDescendantOf(t.GetTypeElement(myKnownTypesCache, type.Module));
            });
        }

        private static bool IsUnityBuiltinType(IType type)
        {
            return type is IDeclaredType declaredType && ourUnityBuiltinSerializedFieldTypes.Contains(declaredType.GetClrName());
        }
    }
}