using System.Collections.Generic;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    public interface IProjectChangeHandler
    {
        void OnProjectChanged(IProject unityProject, Lifetime projectLifetime);
        
        void OnSolutionLoaded(UnityProjectsCollection solution);
    }
}