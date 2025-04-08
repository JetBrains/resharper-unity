﻿using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    [TestUnity]
    public class FormerlySerializedAsRenameFieldTests : RenameTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Refactorings\Rename";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }
        [Test] public void Test05() { DoNamedTest(); }
        [Test] public void Test06() { DoNamedTest(); }
        [Test] public void Test07() { DoNamedTest(); }
    }
}