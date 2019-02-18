using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    public abstract class CSharpHighlightingTestBase<T> : CSharpHighlightingTestBase
    {
        protected sealed override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is T;
        }
    }

    public abstract class CSharpHighlightingTestWithProductDependentGoldBase<T> : CSharpHighlightingTestBase
    {
        protected abstract string RelativeTestDataRoot { get; }

        private const string ProductGoldSuffix =
#if RIDER
                "rider"
#else
                "resharper"
#endif
            ;

        protected sealed override string RelativeTestDataPath => $@"{RelativeTestDataRoot}\{ProductGoldSuffix}";
        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is T;
        }
    }
}