﻿using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity(UnityVersion.Unity55, IncludeNetworking = true)]
    public class SyncVarUsageErrorTests : CSharpHighlightingTestBase<SyncVarUsageError>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test]
        public void TestSyncVarUsageError() { DoNamedTest2(); }
    }
}