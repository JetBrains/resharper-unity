#if !RIDER
// TODO: Sort out zoning properly so this Just Works when run in Rider

using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.PropertiesExtender;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.ProjectModel
{
    [SolutionComponent]
    public class VsUnityVersionPropertiesExtenderProvider : IPropertiesExtenderProvider
    {
        private readonly Lifetime myLifetime;
        private readonly IShellLocks myLocks;
        private readonly UnityVersion myUnityVersion;
        private readonly UnityApi myUnityApi;

        public VsUnityVersionPropertiesExtenderProvider(Lifetime lifetime, IShellLocks locks, UnityVersion unityVersion, UnityApi unityApi)
        {
            myLifetime = lifetime;
            myLocks = locks;
            myUnityVersion = unityVersion;
            myUnityApi = unityApi;
        }

        public bool CanExtend(IProjectItem projectItem, PropertiesLocation location)
        {
            var project = projectItem as IProject;
            return project != null && project.IsUnityProject();
        }

        public IEnumerable<PropertyDescriptor> GetPropertyDescriptors(IProjectItem projectItem)
        {
            var project = (IProject)projectItem;

            yield return new ReSharperPropertyDescriptor<string, IProject>(myLifetime, myLocks,
              name: "UnityVersion",
              defaultValue: "Unspecified",
              displayName: "Unity Version",
              description: "The version of Unity being targeted by the project. Used by ReSharper to validate APIs.",
              projectItem: project,
              getValueAction: p =>
              {
                  var s = myUnityVersion.GetActualVersion(project).ToString(2);
                  var n = myUnityApi.GetNormalisedActualVersion(project).ToString(2);
                  if (s == n) return s;
                  return $"{s} (using API info for {n})";
              },
              setValueAction: (p, value) => { });
        }
    }
}

#endif