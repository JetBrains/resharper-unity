using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Feature.Services.CodeCompletion
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefVersionDefinesNameCompletionListTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\VersionDefines";

        protected override string ProjectName => "Assembly-CSharp";

        // TODO: Add some dummy packages for testing
        [Test] public void TestList01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefVersionDefinesNameCompletionActionTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\VersionDefines";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

        protected override string ProjectName => "Assembly-CSharp";

        [Test] public void TestAction01() { DoNamedTest(); }
    }
}