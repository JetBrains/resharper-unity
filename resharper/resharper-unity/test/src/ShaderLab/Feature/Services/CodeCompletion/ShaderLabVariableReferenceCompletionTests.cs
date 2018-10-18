using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Feature.Services.CodeCompletion
{
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabVariableReferenceCompletionListTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\VariableReference";

        [Test] public void TestList01() { DoNamedTest(); }
    }

    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabVariableReferenceCompletionActionTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\VariableReference";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

        [Test] public void TestAction01() { DoNamedTest(); }
    }
}