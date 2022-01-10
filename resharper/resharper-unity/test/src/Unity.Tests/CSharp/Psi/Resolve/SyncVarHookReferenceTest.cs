using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Psi.Resolve
{
    [TestUnity(UnityVersion.Unity55, IncludeNetworking = true)]
    [IncludeMsCorLib]
    public class SyncVarHookReferenceTest : ReferenceTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Resolve\SyncVarHook";

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
        protected override string RelativeTestDataPath => @"CSharp\Resolve\SyncVarHook\CodeCompletion";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;

        [Test] public void SyncVarHook01() { DoNamedTest(); }
    }
}