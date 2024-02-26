using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using Lifetime = JetBrains.Lifetimes.Lifetime;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes;

[TestUnity]
public class UnityObjectLifetimeCheckViaNullEqualityQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
{
    protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UnityObjectLifetimeCheckViaNullEquality\Availability";

    protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile, IContextBoundSettingsStore boundSettingsStore) => 
        highlighting is UnityObjectLifetimeCheckViaNullEqualityWarning or UnityObjectLifetimeCheckViaNullEqualityHintHighlighting;

    [Test] public void Test01() { DoNamedTest(); }
}


[TestUnity]
public class UnityObjectLifetimeCheckViaNullEqualityQuickFixTests : QuickFixTestBase<UnityObjectLifetimeCheckViaNullEqualityQuickFix>
{
    protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UnityObjectLifetimeCheckViaNullEquality";
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
[TestSetting(typeof(UnitySettings), nameof(UnitySettings.ForceLifetimeChecks), true)]
public class UnityObjectLifetimeCheckViaNullEqualityQuickFixWithBypassCheckTests : QuickFixTestBase<UnityObjectLifetimeCheckViaNullEqualityQuickFix>
{
    protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UnityObjectLifetimeCheckViaNullEquality";
    protected override bool AllowHighlightingOverlap => true;
    
    [Test] public void TestWithBypassCheck01() { DoNamedTest(); }
    [Test] public void TestWithBypassCheck02() { DoNamedTest(); }
    [Test] public void TestWithBypassCheck03() { DoNamedTest(); }
    [Test] public void TestWithBypassCheck04() { DoNamedTest(); }
}