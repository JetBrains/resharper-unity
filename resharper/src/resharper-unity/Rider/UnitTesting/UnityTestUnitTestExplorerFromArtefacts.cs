using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Exploration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityTestUnitTestExplorerFromArtefacts : IUnitTestExplorerFromArtefacts
    {
        private readonly UnityTestProvider myUnityTestProvider;

        public UnityTestUnitTestExplorerFromArtefacts(UnityTestProvider unityTestProvider)
        {
            myUnityTestProvider = unityTestProvider;
        }
        
        public bool IsSupported(IProject project, TargetFrameworkId targetFrameworkId)
        {
            return true;
        }

        public Task ProcessProject(IProject project, FileSystemPath outputPath, IUnitTestElementsObserver observer,
            CancellationToken token)
        {
            return Task.Run(() =>
            {
                    
            }, token);
        }

        public IUnitTestProvider Provider
        {
            get { return myUnityTestProvider; }
        }

        public bool RemoveDynamicTests
        {
            get { return false; }
        }
    }
}