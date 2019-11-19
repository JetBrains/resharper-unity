using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Intentions.CreateFromUsage;
using JetBrains.ReSharper.Intentions.Legacy;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.TextControl;
using JetBrains.Util.Special;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class CreateMethodFromUnityStringLiteralUsageTest
        : CSharpCreateFromUsageTestBase<UnityCreateMethodFromStringLiteralUsageAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\CreateFromUsage";

        [Test] public void TestCreateMethodForCoroutine() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine01() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine02() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine03() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine04() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine05() { DoNamedTest2(); }
    }
}