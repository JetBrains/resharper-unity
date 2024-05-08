using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Pencils.Filters;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Pencils
{
    public class UnityPencilsFilter : PencilsFilterSettingsBase<UnitySettings>
    {
        public UnityPencilsFilter(Lifetime lifetime, UnitySolutionTracker solutionTracker, ISettingsStore store)
            : base("Unity", "Plugins", "Unity", "", solutionTracker.IsUnityProject.HasTrueValue(), store,
                s => s.EnablePerformanceCriticalCodeHighlighting)
        {
            solutionTracker.HasUnityReference.Advise(lifetime,
                b => IsVisible.Value = solutionTracker.IsUnityProject.HasTrueValue() || b);
        }

        public override string Kind => PencilsGroupKind.UnityPerformanceKind;
    }

    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityPencilsFilterProvider : IPencilsFiltersProvider
    {
        private readonly UnitySolutionTracker mySolutionTracker;

        public UnityPencilsFilterProvider(UnitySolutionTracker solutionTracker)
        {
            mySolutionTracker = solutionTracker;
        }

        public IEnumerable<IPencilsFilter> GetFilters(Lifetime lifetime, ISolution solution, ISettingsStore store)
        {
            return new IPencilsFilter[] {new UnityPencilsFilter(lifetime, mySolutionTracker, store)};
        }
    }
}