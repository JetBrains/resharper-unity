using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.JavaScript.Tree.JsDoc;
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

        public bool IsDescendantOf([NotNull] IClrTypeName unityTypeClrName, [CanBeNull] ITypeElement type)
        {
            if (type == null)
                return false;
            var mb = TypeFactory.CreateTypeByCLRName(unityTypeClrName, type.Module);
            return type.IsDescendantOf(mb.GetTypeElement());
        }
        
        public bool IsDescendantOfMonoBehaviour([CanBeNull] ITypeElement type)
        {
            return IsDescendantOf(KnownTypes.MonoBehaviour, type);
        }
        
        
        public bool IsDescendantOfScriptableObject([CanBeNull] ITypeElement type)
        {
            return IsDescendantOf(KnownTypes.ScriptableObject, type);
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