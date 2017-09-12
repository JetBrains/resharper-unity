using System.Collections.Generic;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    public class UnityProjectsCollection
    {
        public FileSystemPath SolutionPath { get; }
        
        public Dictionary<IProject, Lifetime> UnityProjectLifetimes { get; }

        public UnityProjectsCollection(Dictionary<IProject, Lifetime> unityProjectLifetimes, FileSystemPath solutionPath)
        {
            UnityProjectLifetimes = unityProjectLifetimes;
            SolutionPath = solutionPath;
        }
    }
}