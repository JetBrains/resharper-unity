using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.QuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class AddRequireForUpdateSingletonQuickFixAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return  highlighting is SingletonMustBeRequestedWarning && base.HighlightingPredicate(highlighting, psiSourceFile, boundSettingsStore);
        }
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