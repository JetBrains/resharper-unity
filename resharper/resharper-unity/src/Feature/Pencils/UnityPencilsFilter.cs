using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Pencils.Filters;
using JetBrains.ReSharper.Plugins.Unity.Feature.HighlightingEye;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Pencils
{
    public class UnityPencilsFilter : PencilsFilterSettingsBase<UnitySettings>
    {
        public UnityPencilsFilter(IContextBoundSettingsStore store)
            : base("Unityh", "Plugins", "Unity", "", true, store, s => s.EnablePerformanceCriticalCodeHighlighting)
        {
        }

        public override string Kind => PencilsGroupKind.UnityPerformanceKind;
    }
    
    [SolutionComponent]
    public class UnityPencilsFilterProvider : IPencilsFiltersProvider
    {
        public IEnumerable<IPencilsFilter> GetFilters(Lifetime lifetime, ISolution solution, IContextBoundSettingsStore store)
        {
            return new IPencilsFilter[] {new UnityPencilsFilter(store)};
        }
    }
}