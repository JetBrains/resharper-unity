using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Psi.Resolve
{
    [TestUnity]
    public class UnityEventFunctionReferenceTest : ReferenceTestBase
    {
        protected override string RelativeTestDataPath => @"resolve";

        protected override bool AcceptReference(IReference reference)
        {
            return reference is UnityEventFunctionReference;
        }

        [Test] public void Invoke01() { DoNamedTest(); }
        [Test] public void Invoke02() { DoNamedTest(); }
        [Test] public void InvokeRepeating01() { DoNamedTest(); }
        [Test] public void InvokeRepeating02() { DoNamedTest(); }
        [Test] public void CancelInvoke01() { DoNamedTest(); }
        [Test] public void CancelInvoke02() { DoNamedTest(); }
        [Test] public void IsInvoking01() { DoNamedTest(); }
        [Test] public void InvokeOtherType01() { DoNamedTest(); }
        [Test] public void StartCoroutine01() { DoNamedTest(); }
        [Test] public void StopCoroutine01() { DoNamedTest(); }
        [Test] public void CoroutineInOtherType01() { DoNamedTest(); }
    }

    [TestUnity]
    public class UnityEventFunctionCompletionTest : CodeCompletionTestBase
    {
        protected override string RelativeTestDataPath => @"resolve\CodeCompletion";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;

        [Test] public void Invoke01() { DoNamedTest(); }
        [Test] public void InvokeRepeating01() { DoNamedTest(); }
        [Test] public void CancelInvoke01() { DoNamedTest(); }
        [Test] public void IsInvoking01() { DoNamedTest(); }
        [Test] public void InvokeOtherType01() { DoNamedTest(); }
        [Test] public void StartCoroutine01() { DoNamedTest(); }
        [Test] public void StopCoroutine01() { DoNamedTest(); }
        [Test] public void CoroutineInOtherType01() { DoNamedTest(); }
    }
}