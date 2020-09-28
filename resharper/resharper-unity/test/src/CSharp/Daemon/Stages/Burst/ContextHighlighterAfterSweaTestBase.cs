using System.Collections.Generic;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Burst
{
    public abstract class ContextHighlighterAfterSweaTestBase : ContextHighlighterTestBase
    {
        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {       
            var swea = SolutionAnalysisService.GetInstance(Solution);
            using (swea.RunAnalysisCookie())
            {
                var files = new List<IPsiSourceFile>(swea.GetFilesToAnalyze());
                
                foreach (var file in files)
                    swea.AnalyzeInvisibleFile(file);

                swea.AllFilesAnalyzed();

                base.DoTest(lifetime, testProject);
            }
        }
    }
}