using System;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.CodeCompletion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [TestUnity, RequireHlslSupport, CgIncludesDirectory("ShaderLab/CGIncludes")]
    public class ShaderLabSemanticCompletionTests : ShaderLabCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        
        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\Semantic";

        [TestCase("TestSemantic01.shader")]
        [TestCase("TestSemantic02.shader")]
        [TestCase("TestSemantic03.shader")]
        [TestCase("TestSemantic04.shader")]
        [TestCase("TestSemantic05.shader")]
        [TestCase("TestSemantic06.shader")]
        public void Test(string name) => DoTestSolution(name);

        protected override Func<ILookupItem, bool> ItemSelector { get; } = item => item is LookupItem lookupItem && lookupItem.ItemInfo is HlslSemanticItemsProvider.SemanticTextualInfo;
    }
}