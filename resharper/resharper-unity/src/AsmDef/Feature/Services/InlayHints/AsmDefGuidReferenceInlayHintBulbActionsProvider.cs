#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    public class AsmDefGuidReferenceInlayHintBulbActionsProvider : SimpleInlayHintBulbActionsProvider
    {
        public AsmDefGuidReferenceInlayHintBulbActionsProvider()
            : base((s => s.ShowAsmDefGuidReferenceNames))
        {
        }
    }
}