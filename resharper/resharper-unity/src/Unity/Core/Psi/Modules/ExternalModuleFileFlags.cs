#nullable enable
using System;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    /// <summary>Flags for external module file.</summary>
    [Flags]
    public enum ExternalModuleFileFlags
    {
        None,
        IndexWhenAssetsEnabled  = 0x01, // when assets indexing enabled
        IndexWhenAssetsDisabled = 0x02, // when assets indexing disabled
        IndexAlways = IndexWhenAssetsEnabled | IndexWhenAssetsDisabled,
        TreatAsNonGenerated     = 0x04  // when asset should be treated as non-generated file
    }
}