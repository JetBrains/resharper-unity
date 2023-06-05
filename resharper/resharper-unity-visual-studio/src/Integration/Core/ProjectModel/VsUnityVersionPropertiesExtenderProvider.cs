using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.PropertiesExtender;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Integration.Core.ProjectModel
{
    [SolutionComponent]
    public class VsUnityVersionPropertiesExtenderProvider : IPropertiesExtenderProvider
    {
        private readonly Lifetime myLifetime;
        private readonly IShellLocks myLocks;
        private readonly UnityVersion myUnityVersion;
        private readonly UnityApi myUnityApi;

        public VsUnityVersionPropertiesExtenderProvider(Lifetime lifetime, IShellLocks locks, UnityVersion unityVersion,
                                                        UnityApi unityApi)
        {
            myLifetime = lifetime;
            myLocks = locks;
            myUnityVersion = unityVersion;
            myUnityApi = unityApi;
        }

        public bool CanExtend(IProjectItem projectItem, PropertiesLocation location) =>
            projectItem is IProject project && project.IsUnityProject();

        public IEnumerable<PropertyDescriptor> GetPropertyDescriptors(IProjectItem projectItem)
        {
            var project = (IProject)projectItem;

            yield return new ReSharperPropertyDescriptor<string, IProject>(myLifetime, myLocks,
              name: "UnityVersion",
              defaultValue: "Unspecified",
              displayName: "Unity Version",
              description: "The version of Unity being targeted by the project. Used by ReSharper to validate APIs.",
              projectItem: project,
              getValueAction: _ =>
              {
                  var s = myUnityVersion.GetActualVersion(project).ToString(2);
                  var n = myUnityApi.GetNormalisedActualVersion(project).ToString(2);
                  if (s == n) return s;
                  return $"{s} (using API info for {n})";
              },
              setValueAction: (_, _) => { });
        }
    }
}