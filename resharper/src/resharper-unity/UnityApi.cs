using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [Flags]
    public enum EventFunctionMatch
    {
        NoMatch = 1,
        MatchingName = 2,
        MatchingStaticModifier = 4,
        MatchingSignature = 8,
        MatchingReturnType = 16,
        ExactMatch = MatchingName | MatchingStaticModifier | MatchingSignature | MatchingReturnType
    }

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
        public IEnumerable<UnityType> GetBaseUnityTypes([NotNull] ITypeElement type)
        {
            var projectPsiModule = type.Module as IProjectPsiModule;
            if (projectPsiModule == null)
                return EmptyArray<UnityType>.Instance;
            var unityVersion = myUnityVersion.GetActualVersion(projectPsiModule.Project);
            return GetBaseUnityTypes(type, unityVersion);
        }

        [NotNull]
        public IEnumerable<UnityType> GetBaseUnityTypes([NotNull] ITypeElement type, Version unityVersion)
        {
            var types = myTypes.Value;
            unityVersion = types.NormaliseSupportedVersion(unityVersion);
            return types.Types.Where(t => t.SupportsVersion(unityVersion) && type.IsDescendantOf(t.GetTypeElement(type.Module)));
        }

        public bool IsUnityType([NotNull] ITypeElement type)
        {
            return GetBaseUnityTypes(type).Any();
        }

        public bool IsEventFunction([NotNull] IMethod method)
        {
            return GetUnityEventFunction(method) != null;
        }

        public bool IsUnityField([NotNull] IField field)
        {
            if (field.IsStatic)
                return false;

            var containingType = field.GetContainingType();
            if (containingType == null || !IsUnityType(containingType))
                return false;

            // TODO: This should also check the type of the field
            // Only allow serializable fields
            // See https://docs.unity3d.com/ScriptReference/SerializeField.html
            // This should probably also be an inspection

            var accessRights = field.GetAccessRights();
            if (accessRights == AccessRights.PUBLIC)
            {
                return !field.HasAttributeInstance(KnownTypes.NonSerializedAttribute, false);
            }
            if (accessRights == AccessRights.PRIVATE)
            {
                return field.HasAttributeInstance(KnownTypes.SerializeField, false);
            }

            return false;
        }

        public UnityEventFunction GetUnityEventFunction([NotNull] IMethod method)
        {
            EventFunctionMatch match;
            return GetUnityEventFunction(method, out match);
        }

        public UnityEventFunction GetUnityEventFunction([NotNull] IMethod method, out EventFunctionMatch match)
        {
            match = EventFunctionMatch.NoMatch;

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
                        if (function.Match(method) != EventFunctionMatch.NoMatch)
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
    }
}