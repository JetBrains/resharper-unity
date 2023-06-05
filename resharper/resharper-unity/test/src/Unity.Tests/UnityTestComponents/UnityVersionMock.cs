using System;
using JetBrains.Collections.Viewable;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    public class UnityVersionMock : IUnityVersion
    {
        public ViewableProperty<Version> ActualVersionForSolution { get; }
        public VirtualFileSystemPath ActualAppPathForSolution { get; set; } = FileSystemPath.Empty.ToVirtualFileSystemPath();

        public VirtualFileSystemPath GetActualAppPathForSolution() => ActualAppPathForSolution;

        public UnityVersionMock(Version version)
        {
            ActualVersionForSolution = new ViewableProperty<Version>(version);
        }
    }
}