using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Psi.CodeAnnotations
{
    [TestUnity]
    public class CustomCodeAnnotationProviderTest : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\CodeAnnotations";

        // IteratorMethodResultIsIgnoredWarning very similar warning, given if an iterator
        // result isn't used. We'll override it. Hopefully.

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is MustUseReturnValueWarning || highlighting is IteratorMethodResultIsIgnoredWarning ||
                   highlighting is ConditionIsAlwaysTrueOrFalseWarning;
        }

        [Test] public void TestUnusedCoroutineReturnValue() { DoNamedTest2(); }

        // Note that this test includes a definition of the ValueRangeAttribute in the source. This is because the
        // external annotations module cannot load JetBrains.Annotations in a test context. Other annotation based tests
        // work because Unity.Engine includes a subset of our annotations, and it's enough to run the tests
        [Test] public void TestRangeAttributeAsValueRangeAttribute() { DoNamedTest2(); }
        [Test] public void TestMinAttributeAsValueRangeAttribute() { DoNamedTest2(); }
    }
}