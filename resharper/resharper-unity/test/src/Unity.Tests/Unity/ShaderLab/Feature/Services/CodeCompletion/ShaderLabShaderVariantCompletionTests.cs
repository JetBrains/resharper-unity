using System;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Psi.Resources;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [TestUnity, RequireHlslSupport, CgIncludesDirectory("ShaderLab/CGIncludes")]
    public class ShaderLabShaderVariantCompletionTests : ShaderLabCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
        
        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\ShaderVariant";

        [TestCase("ShaderVariant01.shader")]
        [TestCase("ShaderVariant02.shader")]
        [TestCase("ShaderVariant03.shader")]
        [TestCase("ShaderVariant04.shader")]
        [TestCase("ShaderVariant05.shader")]
        [TestCase("ShaderVariant06.shader")]
        [TestCase("ShaderVariant07.shader")]
        [TestCase("ShaderVariant08.shader")]
        public void Test(string name) => DoTestSolution(name);

        protected override bool LookupItemFilter(ILookupItem item)
        {
            return item is LookupItem lookupItem
                   && lookupItem.Presentation.Image == PsiSymbolsThemedIcons.Macro.Id
                   && !lookupItem.ItemInfo.Text.StartsWith("__");
        }
    }
}