#nullable enable
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[SolutionComponent]
public class UnityCSharpAnalysisConfig
{
    public IProperty<bool> ForceLifetimeChecks { get; }

    public UnityCSharpAnalysisConfig(Lifetime lifetime, IApplicationWideContextBoundSettingStore store)
    {
        ForceLifetimeChecks = store.BoundSettingsStore.GetValueProperty(lifetime, (UnitySettings s) => s.ForceLifetimeChecks);
    }
}