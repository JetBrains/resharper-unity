#nullable enable
using System;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface IUnityVersion
    {
        public ViewableProperty<Version> ActualVersionForSolution { get; }
        public VirtualFileSystemPath GetActualAppPathForSolution();
        public Version GetActualVersion(IProject? project);
    }
}