using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Feature.Services.CodeCompletion
{
    public class VersionSpecificCompletionListTest : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"codeCompletion\List";
        protected override bool CheckAutomaticCompletionDefault() => true;

        [Test, TestUnity(UnityVersion.Unity54, Inherits = false)]
        public void OnParticleTriggerWithOneArg54() { DoNamedTest(); }

        //[Test, TestUnity(UnityVersion.Unity55, Inherits = false)]
        //public void OnParticleTriggerWithNoArgs55() { DoNamedTest(); }
    }
}