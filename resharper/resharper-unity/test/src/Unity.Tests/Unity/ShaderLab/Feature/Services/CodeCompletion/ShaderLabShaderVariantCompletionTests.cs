using System;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.CodeCompletion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [TestUnity, RequireHlslSupport, CgIncludesDirectory("ShaderLab/CGIncludes")]
    public class ShaderLabShaderVariantCompletionTests : ShaderLabCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        
        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\ShaderVariant";

        [TestCase("ShaderVariant01.shader")]
        [TestCase("ShaderVariant02.shader")]
        [TestCase("ShaderVariant03.shader")]
        [TestCase("ShaderVariant04.shader")]
        public void Test(string name) => DoTestSolution(name);

        protected override Func<ILookupItem, bool> ItemSelector { get; } = item => item is LookupItem { ItemInfo: ShaderVariantDefineSymbolsProvider.MyTextualInfo };
    }
}