using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    // Amortise the cost of BindToContextLive across any component that wants to write purely to the Solution layer
    [SolutionComponent]
    public class SolutionWideWritableContextBoundSettingsStore
    {
        public SolutionWideWritableContextBoundSettingsStore(Lifetime lifetime, ISolution solution,
                                                             ISettingsStore settingsStore)
        {
            BoundSettingsStore = settingsStore.BindToContextLive(lifetime,
                ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()));
        }

        public IContextBoundSettingsStoreLive BoundSettingsStore { get; }
    }
}