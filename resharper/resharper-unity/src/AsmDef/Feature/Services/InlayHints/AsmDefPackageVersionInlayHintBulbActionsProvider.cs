#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    public class AsmDefPackageVersionInlayHintBulbActionsProvider : SimpleInlayHintBulbActionsProvider
    {
        public AsmDefPackageVersionInlayHintBulbActionsProvider()
            : base((s => s.ShowAsmDefVersionDefinePackageVersions))
        {
        }
    }
}