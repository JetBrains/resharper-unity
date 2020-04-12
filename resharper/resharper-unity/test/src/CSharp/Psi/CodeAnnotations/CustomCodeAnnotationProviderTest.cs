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
            return highlighting is MustUseReturnValueWarning || highlighting is IteratorMethodResultIsIgnoredWarning;
        }

        [Test] public void TestUnusedCoroutineReturnValue() { DoNamedTest2(); }
    }
}