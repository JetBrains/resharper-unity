using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [RequireHlslSupport]
    public class ShaderLabVariableReferenceCompletionListTests : ShaderLabCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\VariableReference";

        [Test] public void TestList01() { DoNamedTest(); }
    }

    [RequireHlslSupport]
    public class ShaderLabVariableReferenceCompletionActionTests : ShaderLabCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\VariableReference";
        protected override bool CheckAutomaticCompletionDefault() => true;

        [Test] public void TestAction01() { DoNamedTest(); }
    }
}