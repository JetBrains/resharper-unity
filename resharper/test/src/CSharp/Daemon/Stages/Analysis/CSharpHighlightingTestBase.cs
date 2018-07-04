using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    public class CSharpHighlightingTestBase<T> : CSharpHighlightingTestBase
    {
        protected sealed override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is T;
        }
    }
}