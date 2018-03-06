using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Exploration;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityTestExplorerFromFile : IUnitTestExplorerFromFile
    {
        public void ProcessFile(IFile psiFile, IUnitTestElementsObserver observer, Func<bool> interrupted)
        {
            
        }

        public IUnitTestProvider Provider
        {
            get { return null; }
        }
    }
}