#nullable enable
using System;
using JetBrains.Application.Parts;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class CollidingElementIdProviderMock : UnityElementIdProviderMock
    {
        private static readonly ElementId ourFixedTypeParamId = new ElementId(42, false);

        [ThreadStatic]
        public static bool ForceCollisions;

        protected override ElementId? GetElementIdInternal(IDeclaredElement? element, ITypeElement? ownerType, int index)
        {
            if (ForceCollisions && element is ITypeParameter)
                return ourFixedTypeParamId;
            return base.GetElementIdInternal(element, ownerType, index);
        }

        protected override ElementId? GetElementIdInternal(IMetadataEntity? metadataEntity, IPsiAssemblyFile assemblyFile)
        {
            if (ForceCollisions && metadataEntity is IMetadataTypeParameter)
                return ourFixedTypeParamId;
            return base.GetElementIdInternal(metadataEntity, assemblyFile);
        }

        protected override ElementId? GetElementIdInternal(IMetadataType metadataType, IPsiAssemblyFile assemblyFile)
        {
            if (ForceCollisions && metadataType is IMetadataTypeParameterReferenceType)
                return ourFixedTypeParamId;
            return base.GetElementIdInternal(metadataType, assemblyFile);
        }
    }
}
