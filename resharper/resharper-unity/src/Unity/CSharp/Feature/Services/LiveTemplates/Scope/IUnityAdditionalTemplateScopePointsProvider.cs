#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    /// TODO: this is a hack until Platform refactoring for grouping multiple UI categories in single templates tab  
    public interface IUnityAdditionalTemplateScopePointsProvider
    {
        public IEnumerable<ITemplateScopePoint> GetUnityScopePoints();
        public bool TryPresent(ITemplateScopePoint scopePoint, [MaybeNullWhen(false)] out string presentation);
        bool HaveOptionsUIFor(ITemplateScopePoint point);
        IScopeOptionsUIBase? CreateUI(ITemplateScopePoint point);
    }
}