using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Pencils.Filters;
using JetBrains.ReSharper.Plugins.Unity.Feature.HighlightingEye;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Pencils
{
    public class UnityPencilsFilter : PencilsFilterSettingsBase<UnitySettings>
    {
        public UnityPencilsFilter(Lifetime lifetime, UnitySolutionTracker solutionTracker, UnityReferencesTracker referencesTracker, ISettingsStore store)
            : base("Unity", "Plugins", "Unity", "", solutionTracker.IsUnityProject.HasTrueValue(), store, s => s.EnablePerformanceCriticalCodeHighlighting)
        {
            referencesTracker.HasUnityReference.Advise(lifetime, b => IsVisible.Value = solutionTracker.IsUnityProject.HasTrueValue() || b);
        }

        public override string Kind => PencilsGroupKind.UnityPerformanceKind;
    }
    
    [SolutionComponent]
    public class UnityPencilsFilterProvider : IPencilsFiltersProvider
    {
        public IEnumerable<IPencilsFilter> GetFilters(Lifetime lifetime, ISolution solution, ISettingsStore store)
        {
            return new IPencilsFilter[] {new UnityPencilsFilter(lifetime, solution.GetComponent<UnitySolutionTracker>(), solution.GetComponent<UnityReferencesTracker>(), store)};
        }
    }
}