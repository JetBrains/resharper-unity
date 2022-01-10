using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
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

        protected sealed override string RelativeTestDataPath => $@"{RelativeTestDataRoot}\{Utils.ProductGoldSuffix}";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is T;
        }
    }
}