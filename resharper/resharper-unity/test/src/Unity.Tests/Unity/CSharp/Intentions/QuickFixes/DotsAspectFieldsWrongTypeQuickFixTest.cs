using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class AspectFieldsWrongAvailabilityTest : QuickFixAvailabilityTestBase<AspectFieldsWrongTypeQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\Dots\AspectFieldsType\Availability";

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
    public class DotsAspectFieldsWrongTypeQuickFixTest : QuickFixTestBase<AspectFieldsWrongTypeQuickFix>
    {
        
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\Dots\AspectFieldsType";
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

        [Test] public void ROTest() { DoNamedTest(); }
        [Test] public void RWTest() { DoNamedTest(); }
        [Test] public void EnabledROTest() { DoNamedTest(); }
        [Test] public void EnabledRWTest() { DoNamedTest(); }
    }
}