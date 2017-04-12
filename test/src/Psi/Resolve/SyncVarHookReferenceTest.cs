using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Psi.Resolve
{
    [TestUnity(UnityVersion.Unity55, IncludeNetworking = true)]
    [IncludeMsCorLib]
    public class SyncVarHookReferenceTest : ReferenceTestBase
    {
        protected override string RelativeTestDataPath => @"resolve\SyncVarHook";

        protected override bool AcceptReference(IReference reference)
        {
            return reference is SyncVarHookReference;
        }

        [Test] public void SyncVarHook01() { DoNamedTest(); }
        [Test] public void InvalidSignature01() { DoNamedTest(); }
    }

    [TestUnity(UnityVersion.Unity55, IncludeNetworking = true)]
    [IncludeMsCorLib]
    public class SyncVarHookCompletionTest : CodeCompletionTestBase
    {
        protected override string RelativeTestDataPath => @"resolve\SyncVarHook\CodeCompletion";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;

        [Test] public void SyncVarHook01() { DoNamedTest(); }
    }
}