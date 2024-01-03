using System.Linq;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class AddRequireForUpdateSingletonQuickFixAvailabilityTest : QuickFixAvailabilityTestBase<AddRequireForUpdateSingletonQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\Dots\AddRequireForUpdateSingletonQuickFix\Availability";

        protected override void DoNamedTest(params string[] otherFiles)
        {
            var files = otherFiles.Concat(new []{"../../DotsClasses.cs"}).ToArray();
            base.DoNamedTest(files);
        }

        [Test] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    public class AddRequireForUpdateSingletonQuickFixTest : QuickFixTestBase<AddRequireForUpdateSingletonQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\Dots\AddRequireForUpdateSingletonQuickFix";
        protected override bool AllowHighlightingOverlap => true;
        protected override void DoNamedTest(params string[] otherFiles)
        {
            var files = otherFiles.Concat(new []{"../DotsClasses.cs"}).ToArray();
            base.DoNamedTest(files);
        }

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test, ExecuteScopedActionInFile] public void Test03() { DoNamedTest(); }
        [Test, ExecuteScopedActionInFile] public void Test04() { DoNamedTest(); }
    }
}