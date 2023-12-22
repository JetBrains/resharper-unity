#nullable enable

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.TextControl.DocumentMarkup.Adornments;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    public interface IAsmDefInlayHintHighlighting : IInlayHintWithDescriptionHighlighting, IHighlightingWithTestOutput
    {
        string Text { get; }
        PushToHintMode Mode { get; }
        string ContextMenuTitle { get; }
    }

    public interface IAsmDefInlayHintContextActionHighlighting : IInlayHintContextActionHighlighting
    {
        IInlayHintBulbActionsProvider BulbActionsProvider { get; }
    }
}