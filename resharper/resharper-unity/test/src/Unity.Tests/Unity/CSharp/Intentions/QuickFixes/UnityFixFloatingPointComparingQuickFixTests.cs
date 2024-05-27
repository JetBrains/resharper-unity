using System.IdentityModel.Protocols.WSTrust;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;
using Lifetime = JetBrains.Lifetimes.Lifetime;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes;

public class UnityFixFloatingPointComparingQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
{
    protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UnityFixFloatingPointComparing\Availability";

    protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
        IContextBoundSettingsStore boundSettingsStore)
    {
        return highlighting is FloatingPointEqualityComparisonWarning
               && base.HighlightingPredicate(highlighting, psiSourceFile, boundSettingsStore);
    }
    
    protected override void DoTest(Lifetime lifetime)
    {
        using (UnityProjectCookie.RunUnitySolutionCookie(Solution))
            base.DoTest(lifetime);
    }

    [Test] public void Test01() { DoNamedTest(); }
   
}


[TestUnity]
public class UnityFixFloatingPointComparingQuickFixTests : QuickFixTestBase<UnityFixFloatingPointComparingQuickFix>
{
    protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UnityFixFloatingPointComparing";
    protected override bool AllowHighlightingOverlap => true;
    
    protected override void DoTest(Lifetime lifetime)
    {
        using (UnityProjectCookie.RunUnitySolutionCookie(Solution))
            base.DoTest(lifetime);
    }

    [Test] public void Test01() { DoNamedTest(); }
    [Test] public void Test02() { DoNamedTest(); }
}