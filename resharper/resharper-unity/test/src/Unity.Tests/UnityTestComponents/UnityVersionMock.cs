using System;
using JetBrains.Collections.Viewable;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    public class UnityVersionMock : IUnityVersion
    {
        public ViewableProperty<Version> ActualVersionForSolution { get; }

        public UnityVersionMock(Version version)
        {
            ActualVersionForSolution = new ViewableProperty<Version>(version);
        }
    }
}