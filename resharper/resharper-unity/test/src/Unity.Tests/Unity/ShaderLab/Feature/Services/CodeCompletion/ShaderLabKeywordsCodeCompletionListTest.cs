using System;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [RequireHlslSupport]
    [TestSetting(typeof(CodeCompletionSettingsKey), nameof(CodeCompletionSettingsKey.ReplaceKeywordsWithTemplates), false)]
    public class ShaderLabKeywordsCodeCompletionListTest : ShaderLabCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;

        protected override bool CheckAutomaticCompletionDefault() => true;

        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\Keywords\List";

        protected override Func<ILookupItem, bool> ItemSelector => KeywordsOnly;

        private static bool KeywordsOnly(ILookupItem lookupItem)
        {
            if (lookupItem.IsKeyword()) return true;

            switch (lookupItem)
            {
                case LookupItem aspectItem:
                    return aspectItem.ItemInfo is KeywordInfo;
                default:
                    return false;
            }
        }

        [TestCase("TestTopLevelStatement01")]
        [TestCase("TestTopLevelStatement02")]
        [TestCase("TestTopLevelStatement03")]
        [TestCase("TestShaderCommandContent01")]
        [TestCase("TestShaderCommandContent02")]
        [TestCase("TestShaderCommandContent03")]
        [TestCase("TestShaderCommandContent04")]
        [TestCase("TestShaderCommandContent05")]        
        [TestCase("TestCategoryCommandContent01")]
        [TestCase("TestCategoryCommandContent02")]
        [TestCase("TestSubShaderCommandContent01")]
        [TestCase("TestSubShaderCommandContent02")]
        [TestCase("TestSubShaderCommandContent03")]
        [TestCase("TestPropertyValueType")]
        [TestCase("TestGrabPassCommandContent01")]
        [TestCase("TestGrabPassCommandContent02")]
        [TestCase("TestGrabPassCommandContent03")]
        [TestCase("TestTexturePassCommandContent01")]
        [TestCase("TestTexturePassCommandContent02")]
        [TestCase("TestTexturePassCommandContent03")]
        [TestCase("TestSetTextureCommandContent01")]
        [TestCase("TestSetTextureCommandContent02")]
        [TestCase("TestSetTextureCombine01")]
        [TestCase("TestSetTextureCombine02")]
        [TestCase("TestSetTextureCombine03")]
        [TestCase("TestSetTextureCombine04")]
        [TestCase("TestSetTextureCombine05")]
        [TestCase("TestSetTextureCombine06")]
        [TestCase("TestSetTextureCombine07")]
        [TestCase("TestSetTextureCombine08")]
        [TestCase("TestFogValue01")]
        [TestCase("TestFogValue02")]
        [TestCase("TestFogValue03")]
        [TestCase("TestBindChannelsValue01")]
        [TestCase("TestBindChannelsValue02")]
        [TestCase("TestColorMaterial")]
        [TestCase("TestMaterialValue01")]
        [TestCase("TestMaterialValue02")]
        [TestCase("TestStencilValue01")]
        [TestCase("TestStencilValue02")]
        [TestCase("TestComparisonFunctionValue")]
        [TestCase("TestStencilOperation")]
        [TestCase("TestCullValue")]
        [TestCase("TestTexturePropertyValue01")]
        [TestCase("TestTexturePropertyValue02")]
        [TestCase("TestTexturePropertyValue03")]
        public void TestCompletion(string testName) => DoOneTest(testName);
    }
}