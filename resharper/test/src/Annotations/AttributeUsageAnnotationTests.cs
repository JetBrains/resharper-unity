using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Annotations
{
    [TestUnity]
    public class AttributeUsageAnnotationTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"Annotations";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is InvalidAttributeUsageError;
        }

        [Test] public void TestAttributeUsageAnnotations() { DoNamedTest2(); }
    }
}