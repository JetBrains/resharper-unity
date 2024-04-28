using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions
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
                ChangeSettingsTemporarily(lifetime).BoundStore.SetValue((UnitySettings key) =>
                    key.EnableIconsForBurstCode, false);
                ChangeSettingsTemporarily(lifetime).BoundStore.SetValue((UnitySettings key) =>
                    key.EnableIconsForPerformanceCriticalCode, false);

                var files = new List<IPsiSourceFile>(swea.GetFilesToAnalyze());

                foreach (var file in files)
                    swea.AnalyzeInvisibleFile(file);

                swea.AllFilesAnalyzed();

                base.DoTest(lifetime, testProject);
            }
        }

        protected override bool IsAvailable(ContextActionInstance contextActionInstance, ITextControl textControl)
        {
            var isAvailable = base.IsAvailable(contextActionInstance, textControl);

            return isAvailable && contextActionInstance.CreateBulbItemsNoScoped(Solution).Any();
        }
    }
}