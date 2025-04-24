using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes;

[TestUnity]
public class UnityObjectLifetimeCheckViaNullEqualityQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
{
    protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UnityObjectNullComparison\Availability";

    protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile, IContextBoundSettingsStore boundSettingsStore) => 
        highlighting is UnityObjectNullComparisonWarning or UnityObjectNullComparisonHintHighlighting;

    [Test] public void Test01() { DoNamedTest(); }
}


[TestUnity]
[TestCustomInspectionSeverity(UnityObjectNullPatternMatchingWarning.HIGHLIGHTING_ID, Severity.DO_NOT_SHOW)]
public class UnityObjectNullComparisonQuickFixTests : QuickFixTestBase<UnityObjectNullComparisonQuickFix>
{
    protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UnityObjectNullComparison";
    protected override bool AllowHighlightingOverlap => true;

    [Test] public void Test01() { DoNamedTest(); }
    [Test] public void Test02() { DoNamedTest(); }
    [Test] public void Test03() { DoNamedTest(); }
    [Test] public void Test04() { DoNamedTest(); }
    [Test] public void Test05() { DoNamedTest(); }
    [Test] public void Test06() { DoNamedTest(); }
    [Test] public void Test07() { DoNamedTest(); }
}

[TestUnity]
[TestCustomInspectionSeverity(UnityObjectNullPatternMatchingWarning.HIGHLIGHTING_ID, Severity.WARNING)]
public class UnityObjectNullComparisonQuickFixWithBypassCheckTests : QuickFixTestBase<UnityObjectNullComparisonQuickFix>
{
    protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UnityObjectNullComparison";
    protected override bool AllowHighlightingOverlap => true;
    
    [Test] public void TestWithBypassCheck01() { DoNamedTest(); }
    [Test] public void TestWithBypassCheck02() { DoNamedTest(); }
    [Test] public void TestWithBypassCheck03() { DoNamedTest(); }
    [Test] public void TestWithBypassCheck04() { DoNamedTest(); }
}