#nullable enable
using System;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    public interface IUnityVersion
    {
        public ViewableProperty<Version> ActualVersionForSolution { get; }
        public VirtualFileSystemPath GetActualAppPathForSolution();
        public Version GetActualVersion(IProject? project);
    }
}