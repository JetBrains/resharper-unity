#nullable enable
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    public interface IUnityElementIdProvider
    {
        ElementId? GetElementId(IDeclaredElement? element, ITypeElement? ownerType = null, int index = -1);
        ElementId? GetElementId(IMetadataEntity? metadataTypeInfo, IPsiAssemblyFile assemblyFile);
        ElementId? GetElementId(IMetadataType metadataType, IPsiAssemblyFile assemblyFile);
    }
}