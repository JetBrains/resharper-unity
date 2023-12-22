using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.QuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class BurstConvertToFixedStringQuickFixAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return  highlighting is BurstLocalStringVariableDeclarationWarning && base.HighlightingPredicate(highlighting, psiSourceFile, boundSettingsStore);
        }

        protected override string RelativeTestDataPath => @"CSharp/Intentions/QuickFixes/BurstFixedString/Availability";

        [Test] public void Everything() { DoNamedTest(); }
    }
    
    [TestUnity]
    public class BurstConvertToFixedStringQuickFixTest : QuickFixTestBase<ConvertToFixedStringQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp/Intentions/QuickFixes/BurstFixedString";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void BurstFixedStringActionTest1() { DoNamedTest(); }
    }
}