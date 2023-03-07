using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class DotsPartialClassesQuickFixAvailabilityTest : QuickFixAvailabilityTestBase<DotsPartialClassesQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\Dots\DotsPartialClasses\Availability";

        protected override void DoNamedTest(params string[] otherFiles)
        {
            var files = otherFiles.Concat(new []{"../../DotsClasses.cs"}).ToArray();
            base.DoNamedTest(files);
        }
        
        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            using (UnityPackageCookie.RunUnityPackageCookie(Solution, PackageManager.UnityEntitiesPackageName))
                base.DoTest(lifetime, project);
        }

        [Test] public void Test01() { DoNamedTest(); }
    }
    
    [TestUnity]
    public class DotsPartialClassesQuickFixTest : QuickFixTestBase<DotsPartialClassesQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\Dots\DotsPartialClasses";
        protected override bool AllowHighlightingOverlap => true;
        protected override void DoNamedTest(params string[] otherFiles)
        {
            var files = otherFiles.Concat(new []{"../DotsClasses.cs"}).ToArray();
            base.DoNamedTest(files);
        }
        
        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            using (UnityPackageCookie.RunUnityPackageCookie(Solution, PackageManager.UnityEntitiesPackageName))
                base.DoTest(lifetime, project);
        }

        [Ignore("Unity's changing the api, waiting for pre3")][Test] public void Test01() { DoNamedTest(); }
        [Ignore("Unity's changing the api, waiting for pre3")][Test] public void Test02() { DoNamedTest(); }
        [Ignore("Unity's changing the api, waiting for pre3")][Test] public void Test03() { DoNamedTest(); }
        [Ignore("Unity's changing the api, waiting for pre3")][Test, ExecuteScopedActionInFile] public void Test04() { DoNamedTest(); }
        [Ignore("Unity's changing the api, waiting for pre3")][Test, ExecuteScopedActionInFile] public void Test05() { DoNamedTest(); }
    }
}