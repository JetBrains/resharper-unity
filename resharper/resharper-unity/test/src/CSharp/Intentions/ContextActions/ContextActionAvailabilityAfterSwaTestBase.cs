using System.Collections.Generic;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Psi;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
{
    public abstract class
        ContextActionAvailabilityAfterSwaTestBase<TContextAction> : ContextActionAvailabilityTestBase<TContextAction>
        where TContextAction : class, IContextAction
    {
        protected override void DoTestSolution(params string[] fileSet)
        {
            using (TestPresentationMap.Cookie())
            {
                base.DoTestSolution(fileSet);
            }
        }

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

        protected override bool IsAvailable(TContextAction action, ITextControl textControl)
        {
            var isAvailable = base.IsAvailable(action, textControl);

            return isAvailable && action.CreateBulbItems().Any();
        }
    }
}