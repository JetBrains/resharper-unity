using System;
using JetBrains.Collections.Viewable;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    public interface IUnityVersion
    {
        public ViewableProperty<Version> ActualVersionForSolution { get; }
    }
}