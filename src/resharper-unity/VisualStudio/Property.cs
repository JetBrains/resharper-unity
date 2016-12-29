using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.PsiFeatures.VisualStudio.Core.ProjectModel.PropertiesExtender;
using JetBrains.VsIntegration.ProjectModel.PropertiesExtender;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio
{
    [SolutionComponent]
    public class VsUnityVersionPropertiesExtenderProvider : IPropertiesExtenderProvider
    {
        private readonly Lifetime myLifetime;
        private readonly IShellLocks myLocks;
        private readonly UnityVersion myUnityVersion;

        public VsUnityVersionPropertiesExtenderProvider(Lifetime lifetime, IShellLocks locks, UnityVersion unityVersion)
        {
            myLifetime = lifetime;
            myLocks = locks;
            myUnityVersion = unityVersion;
        }

        public bool CanExtend(IProjectItem projectItem)
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
              getValueAction: p => myUnityVersion.Version.ToString(2),
              setValueAction: (p, value) => { });
        }
    }
}