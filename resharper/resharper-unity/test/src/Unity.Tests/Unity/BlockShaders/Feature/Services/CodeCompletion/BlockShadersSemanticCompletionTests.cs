using System;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.CodeCompletion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.BlockShaders.Feature.Services.CodeCompletion
{
    [TestUnity, RequireHlslSupport]
    public class BlockShadersSemanticCompletionTests : ShaderLabCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        
        protected override string RelativeTestDataPath => @"BlockShaders\CodeCompletion\Semantic";

        [TestCase("TestSemantic01.shaderFoundry")]
        [TestCase("TestSemantic02.shaderFoundry")]
        [TestCase("TestSemantic03.shaderFoundry")]
        [TestCase("TestSemantic04.shaderFoundry")]
        [TestCase("TestSemantic05.shaderFoundry")]
        [TestCase("TestSemantic06.shaderFoundry")]
        public void Test(string name) => DoTestSolution(name);

        protected override bool LookupItemFilter(ILookupItem item)
        {
            return item is LookupItem
            {
                ItemInfo: BlockShadersSemanticItemsProvider.AttributeSemanticTextualInfo
                          or HlslSemanticItemsProvider.SemanticTextualInfo
            };
        }
    }
}