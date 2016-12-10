using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityApi
    {
        private readonly UnityVersion myUnityVersion;
        private readonly Lazy<List<UnityType>> myTypes;

        public UnityApi(UnityVersion unityVersion)
        {
            myUnityVersion = unityVersion;
            myTypes = Lazy.Of(() =>
            {
                var apiXml = new ApiXml();
                return apiXml.LoadTypes(myUnityVersion.Version);
            }, true);
        }

        [NotNull]
        public IEnumerable<UnityType> GetBaseUnityTypes([NotNull] ITypeElement type)
        {
            return myTypes.Value.Where(t => t.SupportsVersion(myUnityVersion.Version) && type.IsDescendantOf(t.GetType(type.Module)));
        }

        public bool IsUnityType([NotNull] ITypeElement type)
        {
            return GetBaseUnityTypes(type).Any();
        }

        public bool IsEventFunction([NotNull] IMethod method)
        {
            var containingType = method.GetContainingType();
            if (containingType != null)
            {
                return GetBaseUnityTypes(containingType).Any(type => type.HasEventFunction(method, myUnityVersion.Version));
            }
            return false;
        }

        public bool IsUnityField([NotNull] IField field)
        {
            if (field.GetAccessRights() != AccessRights.PUBLIC)
                return false;

            var containingType = field.GetContainingType();
            return containingType != null && IsUnityType(containingType);
        }

        public UnityEventFunction GetUnityEventFunction([NotNull] IMethod method)
        {
            var containingType = method.GetContainingType();
            if (containingType != null)
            {
                var eventFunctions = from t in GetBaseUnityTypes(containingType)
                    from m in t.GetEventFunctions(myUnityVersion.Version)
                    where m.Match(method)
                    select m;
                return eventFunctions.FirstOrDefault();
            }
            return null;
        }
    }
}