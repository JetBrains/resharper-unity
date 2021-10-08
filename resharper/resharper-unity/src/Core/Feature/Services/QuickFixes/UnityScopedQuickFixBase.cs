using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Feature.Services.QuickFixes.Scoped;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes
{
    public abstract class UnityScopedQuickFixBase : ScopedQuickFixBase
    {
        public override bool IsAvailable(IUserDataHolder cache)
        {
            var treeNode = TryGetContextTreeNode();
            return treeNode != null ? ValidUtils.Valid(treeNode) : base.IsAvailable(cache);
        }

        protected override IScopedFixingStrategy GetScopedFixingStrategy(ISolution solution, IHighlighting highlighting)
        {
            // These strategies are used to reduce the analysers being run on the files in scope, by looking at what
            // highlightings the QF can handle, and what highlightings an analyser advertises.
            // We want to run any analyser that advertises highlightings that this QF can handle. We will disable the
            // fallback support to run any analyser that doesn't advertise *any* highlightings. We know our analysers
            // are registered correctly.
            // We could also limit to same QF + same highlight, etc.
            return new SameQuickFixTypeStrategy(this, solution)
            {
                IncludeAnalyzersWithoutHighlightingTypesDefined = false
            };
        }
    }
}