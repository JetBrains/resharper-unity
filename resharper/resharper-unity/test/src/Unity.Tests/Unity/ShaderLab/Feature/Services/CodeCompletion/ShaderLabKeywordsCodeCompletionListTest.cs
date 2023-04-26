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

        [Ignore("Re-enable when decide on keywords suggestion implementation")]
        [TestCase("TestTopLevelStatement01")]
        [TestCase("TestTopLevelStatement02")]
        [TestCase("TestTopLevelStatement03")]
        [TestCase("TestShaderCommandContent01")]
        [TestCase("TestShaderCommandContent02")]
        [TestCase("TestShaderCommandContent03")]
        [TestCase("TestShaderCommandContent04")]
        [TestCase("TestShaderCommandContent05")]
        [TestCase("TestCategoryCommandContent01")]
        public void TestCompletion(string testName) => DoOneTest(testName);
    }
}