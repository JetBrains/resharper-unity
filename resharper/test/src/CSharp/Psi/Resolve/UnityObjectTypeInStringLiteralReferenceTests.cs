using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Psi.Resolve
{
    [TestUnity]
    public class UnityObjectTypeInStringLiteralReferenceTest : ReferenceTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Resolve\UnityObjectTypeInStringLiteral";

        protected override bool AcceptReference(IReference reference)
        {
            return reference is UnityObjectTypeOrNamespaceReference;
        }

        [Test] public void AddComponent01() { DoNamedTest(); }
        [Test] public void GetComponent01() { DoNamedTest(); }
        [Test] public void ScriptableObjectCreateInstance01() { DoNamedTest(); }
    }

    [TestUnity]
    public class UnityObjectTypeInStringLiteralCompletionTest : CodeCompletionTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Resolve\UnityObjectTypeInStringLiteral\CodeCompletion";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;

        [Test] public void AddComponent01() { DoNamedTest(); }
        [Test] public void AddComponent02() { DoNamedTest(); }
        [Test] public void AddComponent03() { DoNamedTest(); }
        [Test] public void AddComponent04() { DoNamedTest(); }
        [Test] public void AddComponent05() { DoNamedTest(); }
        [Test] public void AddComponent06() { DoNamedTest(); }
        [Test] public void AddComponent07() { DoNamedTest(); }
        [Test] public void AddComponent08() { DoNamedTest(); }

        [Test] public void GetComponent01() { DoNamedTest(); }
        [Test] public void GetComponent02() { DoNamedTest(); }
        [Test] public void GetComponent03() { DoNamedTest(); }
        [Test] public void GetComponent04() { DoNamedTest(); }
        [Test] public void GetComponent05() { DoNamedTest(); }
        [Test] public void GetComponent06() { DoNamedTest(); }
        [Test] public void GetComponent07() { DoNamedTest(); }
        [Test] public void GetComponent08() { DoNamedTest(); }

        [Test] public void ScriptableObjectCreateInstance01() { DoNamedTest(); }
        [Test] public void ScriptableObjectCreateInstance02() { DoNamedTest(); }
        [Test] public void ScriptableObjectCreateInstance03() { DoNamedTest(); }
        [Test] public void ScriptableObjectCreateInstance04() { DoNamedTest(); }
        [Test] public void ScriptableObjectCreateInstance05() { DoNamedTest(); }
        [Test] public void ScriptableObjectCreateInstance06() { DoNamedTest(); }
        [Test] public void ScriptableObjectCreateInstance07() { DoNamedTest(); }
        [Test] public void ScriptableObjectCreateInstance08() { DoNamedTest(); }
    }
}