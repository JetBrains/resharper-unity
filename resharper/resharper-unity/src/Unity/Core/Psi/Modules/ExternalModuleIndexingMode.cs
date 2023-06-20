#nullable enable
namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    /// <summary>Indexing mode for external module.</summary>
    public enum ExternalModuleIndexingMode
    {
        None,   // never
        Assets, // when assets indexing enabled
        Always  // always
    }
}