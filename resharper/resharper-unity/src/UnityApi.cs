using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityApi
    {
        private readonly UnityVersion myUnityVersion;
        private readonly Lazy<UnityTypes> myTypes;

        public UnityApi(UnityVersion unityVersion)
        {
            myUnityVersion = unityVersion;
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

        public bool IsUnityECSType([CanBeNull] ITypeElement type)
        {
            if (type == null)
                return false;
            
            var jobComponentSystem = TypeFactory.CreateTypeByCLRName(KnownTypes.JobComponentSystem, type.Module);
            if (type.IsDescendantOf(jobComponentSystem.GetTypeElement()))
                return true;
            
            var componentSystem = TypeFactory.CreateTypeByCLRName(KnownTypes.ComponentSystem, type.Module);
            if (type.IsDescendantOf(componentSystem.GetTypeElement()))
                return true;

            return false;
        }
        public bool IsSerializableType([CanBeNull] ITypeElement type)
        {
            // A class or struct with the `[System.Serializable]` attribute
            // Should not be abstract, static or generic
            // We'll ignore abstract or generic because it might be being used as a base class
            // TODO: Add a warning if the serializable class isn't inherited
            var clazz = type as IClass;
            if (clazz?.IsStaticClass() == true)
                return false;

            if (type?.IsClassLike() == true)
                return type.HasAttributeInstance(PredefinedType.SERIALIZABLE_ATTRIBUTE_CLASS, true);

            return false;
        }

        public bool IsEventFunction([NotNull] IMethod method)
        {
            return GetUnityEventFunction(method) != null;
        }

        public bool IsSerialisedField([CanBeNull] IField field)
        {
            if (field == null || field.IsStatic || field.IsConstant || field.IsReadonly)
                return false;

            var containingType = field.GetContainingType();
            if (!IsUnityType(containingType) && !IsSerializableType(containingType))
                return false;

            // [NonSerialized] trumps everything, even if there's a [SerializeField] as well
            if (field.HasAttributeInstance(PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS, false))
                return false;

            // TODO: This should also check the type of the field
            // Only allow serializable fields
            // See https://docs.unity3d.com/ScriptReference/SerializeField.html
            // This should probably also be an inspection

            if (field.HasAttributeInstance(KnownTypes.SerializeField, false))
                return true;

            return field.GetAccessRights() == AccessRights.PUBLIC;
        }

        public bool IsInjectedField([CanBeNull] IField field)
        {
            if (field == null || field.IsStatic || field.IsConstant || field.IsReadonly)
                return false;

            var containingType = field.GetContainingType();
            if (containingType == null || !IsUnityECSType(containingType))
                return false;

            return field.HasAttributeInstance(KnownTypes.InjectAttribute, false);
        }
        
        // Best effort attempt at preventing false positives for type members that are actually being used inside a
        // scene. We don't have enough information to do this by name, so we'll mark all potential event handlers as
        // implicitly used by Unity
        // See https://github.com/Unity-Technologies/UnityCsReference/blob/02f8e8ca594f156dd6b2088ad89451143ca1b87e/Editor/Mono/Inspector/UnityEventDrawer.cs#L397
        public bool IsPotentialEventHandler([CanBeNull] IMethod method)
        {
            if (method == null || !method.ReturnType.IsVoid())
                return false;

            // Type.GetMethods() returns public instance methods only
            if (method.GetAccessRights() != AccessRights.PUBLIC || method.IsStatic)
                return false;

            return IsUnityType(method.GetContainingType()) &&
                   !method.HasAttributeInstance(PredefinedType.OBSOLETE_ATTRIBUTE_CLASS, true);
        }

        public bool IsPotentialEventHandler([CanBeNull] IProperty property)
        {
            return IsPotentialEventHandler(property?.Setter);
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
            match = MethodSignatureMatch.NoMatch;

            var projectPsiModule = method.Module as IProjectPsiModule;
            var containingType = method.GetContainingType();
            if (containingType != null && projectPsiModule != null)
            {
                var unityVersion = GetNormalisedActualVersion(projectPsiModule.Project);
                foreach (var type in GetBaseUnityTypes(containingType, unityVersion))
                {
                    foreach (var function in type.GetEventFunctions(unityVersion))
                    {
                        match = function.Match(method);
                        if (function.Match(method) != MethodSignatureMatch.NoMatch)
                            return function;
                    }
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
                    return t.SupportsVersion(normalisedVersion) && type.IsDescendantOf(t.GetTypeElement(type.Module));
            });
        }
    }
}