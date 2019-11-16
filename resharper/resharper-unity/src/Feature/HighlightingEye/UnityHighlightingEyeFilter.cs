using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.HighlightingEye.Filters;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.HighlightingEye
{
    public class UnityHighlightingEyeFilter : HighlightingEyeFilterSettingsBase<UnitySettings>
    {
        public UnityHighlightingEyeFilter(IContextBoundSettingsStore store)
            : base("Unityh", "Plugins", "Unity", "", true, store, s => s.EnablePerformanceCriticalCodeHighlighting)
        {
        }

        public override string Kind => HighlightingEyeGroupKind.UnityPerformanceKind;
    }
    
    [SolutionComponent]
    public class UnityHighlightingEyeFilterProvider : IHighlightingEyeFiltersProvider
    {
        public IEnumerable<IHighlightingEyeFilter> GetFilters(Lifetime lifetime, ISolution solution, IContextBoundSettingsStore store)
        {
            return new IHighlightingEyeFilter[] {new UnityHighlightingEyeFilter(store)};
        }
    }
}