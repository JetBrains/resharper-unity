using JetBrains.Application.InlayHints;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.InlayHints;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    public interface IAsmDefInlayHintHighlighting : IInlayHintWithDescriptionHighlighting, IHighlightingWithTestOutput
    {
        string Text { get; }
        InlayHintsMode Mode { get; }
        string ContextMenuTitle { get; }
    }

    public interface IAsmDefInlayHintContextActionHighlighting : IInlayHintContextActionHighlighting
    {
        IInlayHintBulbActionsProvider BulbActionsProvider { get; }
    }
}