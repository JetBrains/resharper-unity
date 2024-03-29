﻿using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantAttributeOnTargetQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantAttributeOnTarget\Availability";

        [Test] public void TestRedundantAssemblyAttribute() { DoNamedTest2(); }
        [Test] public void TestRedundantClassAttribute() { DoNamedTest2(); }
        [Test] public void TestRedundantFieldAttribute() { DoNamedTest2(); }
        [Test] public void TestRedundantDelegateAttribute() { DoNamedTest2(); }
        [Test] public void TestRedundantAttributesInScope() { DoNamedTest2(); }
    }

    [TestUnity]
    public class RedundantAttributeOnTargetQuickFixRemoveTests : CSharpQuickFixTestBase<RemoveRedundantAttributeQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantAttributeOnTarget";

        [Test] public void TestRedundantAssemblyAttribute() { DoNamedTest2(); }
        [Test] public void TestRedundantClassAttribute() { DoNamedTest2(); }
        [Test] public void TestRedundantFieldAttribute() { DoNamedTest2(); }
        [Test] public void TestRedundantDelegateAttribute() { DoNamedTest2(); }
        [Test] public void TestRedundantAttributesInScope() { DoNamedTest2(); }
    }
}