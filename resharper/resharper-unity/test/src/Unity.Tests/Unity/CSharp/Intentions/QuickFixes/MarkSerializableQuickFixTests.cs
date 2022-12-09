using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    //TODO - move to global stage or optimistic and pessimistic mode, add data provider for the mode
    [TestUnity]
    public class MarkSerializableQuickFixAvailabilityTests : QuickFixAvailabilityTestBase //TODO - add global test QuickFixAfterSwaAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MarkSerializable\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }
    
    [TestUnity]
    public class MarkSerializableQuickFixAvailabilityGlobalStageTests : QuickFixAfterSwaAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MarkSerializable\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return highlighting is RedundantSerializeFieldAttributeWarning 
                   && base.HighlightingPredicate(highlighting, psiSourceFile, boundSettingsStore);
        }
        
        protected override string GetGoldTestDataPath(string fileName)
        {
            return base.GetGoldTestDataPath(fileName + ".global");
        }
    }

    [TestUnity]
    public class MarkSerializableQuickFixTests : CSharpQuickFixAfterSwaTestBase<MarkSerializableQuickFix> 
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MarkSerializable";

        [Test] public void Test01() { DoNamedTest(); }
    }
}