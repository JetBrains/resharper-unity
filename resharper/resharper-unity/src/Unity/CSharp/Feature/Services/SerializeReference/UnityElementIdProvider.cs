#nullable enable
using System;
using JetBrains.Application.Parts;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    internal record TypeElementIdWrapper(string? FullyQualifiedName, IPsiModule? Module, bool IsCompiledType,
        TypeElementIdWrapper? OwnerType)
    {
    }

    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityElementIdProvider : IUnityElementIdProvider
    {
        ElementId? IUnityElementIdProvider.GetElementId(IDeclaredElement? element, ITypeElement? ownerType,
            int index)
        {
            if (element == null)
                return null;
            if (element is ITypeParameter typeParameter)
            {
                var owner = ownerType ?? typeParameter.OwnerType;
                var parameterIndex = index < 0 ? typeParameter.Index : index;

                var psiModule = owner?.Module;
                var ownerWrapper = new TypeElementIdWrapper(owner?.GetClrName().FullName, psiModule,
                    owner is ICompiledElement, null);

                var typeElementIdWrapper = new TypeElementIdWrapper(
                    typeParameter.ShortName + parameterIndex,
                    psiModule,
                    typeParameter is ICompiledElement,
                    ownerWrapper);

                return GetElementId(typeElementIdWrapper);
            }

            if (element is ITypeElement typeElement)
            {
                var elementIdWrapper = new TypeElementIdWrapper(typeElement?.GetClrName().FullName,
                    typeElement?.Module, typeElement is ICompiledElement, null);

                return GetElementId(elementIdWrapper);
            }

            return null;
        }

        ElementId? IUnityElementIdProvider.GetElementId(IMetadataEntity? metadataEntity, IPsiAssemblyFile assemblyFile)
        {
            if (metadataEntity == null)
                return null;
            
            switch (metadataEntity)
            {
                case IMetadataTypeInfo metadataTypeInfo:
                    return GetElementId(new TypeElementIdWrapper(metadataTypeInfo.FullyQualifiedName, assemblyFile.Module,
                        true, null));
                case IMetadataTypeParameter metadataTypeParameter:
                {
                    var typeOwnerFullyQualifiedName = metadataTypeParameter.TypeOwner.FullyQualifiedName;
                    var ownerWrapper = new TypeElementIdWrapper(typeOwnerFullyQualifiedName,
                        assemblyFile.Module, true, null);
                    return GetElementId(new TypeElementIdWrapper(
                        metadataTypeParameter.Name + metadataTypeParameter.Index,
                        assemblyFile.Module, true, ownerWrapper));
                }
                default:
                    throw new NotImplementedException(
                        $"Unknown {nameof(IMetadataEntity)} type: {metadataEntity.GetType()}");
            }
        }

        ElementId? IUnityElementIdProvider.GetElementId(IMetadataType metadataType, IPsiAssemblyFile assemblyFile)
        {
            switch (metadataType)
            {
                case IMetadataClassType classType:
                    return GetElementId(new TypeElementIdWrapper(classType.Type.FullyQualifiedName, assemblyFile.Module,
                        true, null));
                case IMetadataTypeParameterReferenceType parameterReferenceType:
                {
                    var typeOwnerFullyQualifiedName = parameterReferenceType.TypeParameter.TypeOwner.FullyQualifiedName;
                    var ownerWrapper = new TypeElementIdWrapper(typeOwnerFullyQualifiedName,
                        assemblyFile.Module, true, null);
                    return GetElementId(new TypeElementIdWrapper(
                        parameterReferenceType.TypeParameter.Name + parameterReferenceType.TypeParameter.Index,
                        assemblyFile.Module, true, ownerWrapper));
                }
                default:
                    throw new NotImplementedException(
                        $"Unknown {nameof(IMetadataType)} type: {metadataType.GetType()}");
            }
        }

        private ElementId? GetElementId(TypeElementIdWrapper? typeElementIdWrapper)
        {
            if (typeElementIdWrapper == null)
                return null;

            var hash = new Hash();
            var psiModule = typeElementIdWrapper.Module;
            if (psiModule != null)
                CalculatePsiModuleHash(ref hash, psiModule, typeElementIdWrapper.IsCompiledType);

            PutFullyQualifiedName(ref hash, typeElementIdWrapper.FullyQualifiedName, typeElementIdWrapper.OwnerType);
            hash.Finish();

            var id = new ElementId(hash.Value, typeElementIdWrapper.IsCompiledType);
            return id;
        }

        protected virtual void CalculatePsiModuleHash(ref Hash hash, IPsiModule psiModule, bool isCompiledType)
        {
            CLRUsageCheckingServices.PutPsiModule(ref hash, null, psiModule);
        }

        private void PutFullyQualifiedName(ref Hash hash, string? fullyQualifiedName, TypeElementIdWrapper? owner)
        {
            hash.PutString(fullyQualifiedName);

            if (owner == null)
                return;

            var elementId = GetElementId(owner);
            if (elementId.HasValue)
                hash.PutInt(elementId.Value.Value);
        }
    }
}