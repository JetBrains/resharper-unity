using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Feature.Services.CodeCompletion
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    [TestCompilationSymbols("RESHARPER_UNITY_TEST_DEFINED_SYMBOL;RESHARPER_UNITY_TEST_DEFINED_SYMBOL_2")]
    public class AsmDefDefineConstraintCompletionListTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\DefineConstraints";

        protected override string ProjectName => "Assembly-CSharp";

        [Test] public void TestList01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    [TestCompilationSymbols("RESHARPER_UNITY_TEST_DEFINED_SYMBOL;RESHARPER_UNITY_TEST_DEFINED_SYMBOL_2")]
    public class AsmDefDefineConstraintCompletionActionTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\DefineConstraints";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

        protected override string ProjectName => "Assembly-CSharp";

        [Test] public void TestAction01() { DoNamedTest(); }
        [Test] public void TestAction02() { DoNamedTest(); }
        [Test] public void TestAction03() { DoNamedTest(); }
    }
}