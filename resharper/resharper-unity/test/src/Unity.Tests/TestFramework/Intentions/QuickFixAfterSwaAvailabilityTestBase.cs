using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.SolutionAnalysis;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.Unity;

namespace JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions
{
    public abstract class QuickFixAfterSwaAvailabilityTestBase : QuickFixAvailabilityTestBase
    {
        protected override bool ShouldGlobalWarnings => true;

        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            var swea = Solution.GetComponent<SolutionAnalysisService>();

            using (swea.RunAnalysisCookie())
            using (UnityProjectCookie.RunUnitySolutionCookie(Solution))
            {
                foreach (var file in swea.GetFilesToAnalyze())
                    swea.AnalyzeInvisibleFile(file);
                
                swea.AllFilesAnalyzed();
                using (SyncReanalyzeCookie.Create(Solution.Locks, SolutionAnalysisManager.GetInstance(Solution)))
                    base.DoTest(lifetime, testProject);
            }
        }

    }
}