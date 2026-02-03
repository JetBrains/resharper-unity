using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.ReSharper.Psi.Impl.Reflection2.ExternalAnnotations;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.Caches;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class CustomCodeAnnotationProvider : ICustomCodeAnnotationProvider, IInvalidatingCache
    {
        private static readonly IClrTypeName ourMeansImplicitUseAttribute = new ClrTypeName(typeof(MeansImplicitUseAttribute).FullName);
        private static readonly IClrTypeName ourPublicAPIAttribute = new ClrTypeName(typeof(PublicAPIAttribute).FullName);
        private static readonly IClrTypeName ourUsedImplicitlyAttributeFullName = new ClrTypeName(typeof(UsedImplicitlyAttribute).FullName);
        private static readonly IClrTypeName ourImplicitUseTargetFlags = new ClrTypeName(typeof(ImplicitUseTargetFlags).FullName);

        private readonly ExternalAnnotationsModuleFactory myExternalAnnotationsModuleFactory;
        private readonly KnownTypesCache myKnownTypesCache;

        private readonly DirectMappedCache<ITypeElement, bool> myCompiledElementsCache = new(10);
        private readonly DirectMappedCache<ITypeElement, bool> mySourceElementsCache = new(10);

        public CustomCodeAnnotationProvider(ExternalAnnotationsModuleFactory externalAnnotationsModuleFactory, KnownTypesCache knownTypesCache)
        {
            myKnownTypesCache = knownTypesCache;
            myExternalAnnotationsModuleFactory = externalAnnotationsModuleFactory;
        }

        public CodeAnnotationNullableValue? GetNullableAttribute(IDeclaredElement element) => null;
        public CodeAnnotationNullableValue? GetContainerElementNullableAttribute(IDeclaredElement element) => null;

        public ICollection<IAttributeInstance> GetSpecialAttributeInstances(IClrDeclaredElement element,
                                                                            AttributeInstanceCollection existingAttributes)
        {
            if (GetPublicAPIImplicitlyUsedAttribute(element, existingAttributes, out var collection)) return collection;

            return EmptyList<IAttributeInstance>.Instance;
        }

        // Unity ships annotations, but they're out of date. Specifically, the [PublicAPI] attribute is
        // defined with [MeansImplicitUse] instead of [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
        // So if applied to a class, only the class is marked as in use, while the members aren't. This
        // provider will apply [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)] to any type that
        // has the old [PublicAPI] applied
        private bool GetPublicAPIImplicitlyUsedAttribute(IClrDeclaredElement element,
            AttributeInstanceCollection attributeInstanceCollection,
            out ICollection<IAttributeInstance> collection)
        {
            collection = EmptyList<IAttributeInstance>.InstanceList;

            if (element is not ITypeElement || !element.IsFromUnityProject()) return false;

            foreach (var attributeInstance in attributeInstanceCollection.GetAllOwnAttributes().Where(t => t.GetClrName().Equals(ourPublicAPIAttribute)))
            {
                var attributeType = attributeInstance.GetAttributeType();
                if (!(attributeType.Resolve().DeclaredElement is ITypeElement typeElement)) continue;

                if (!CalculateMeansImplicitUse(typeElement))
                    continue;

                // The ctorArguments lambda result is not cached, so let's allocate everything up front
                var flagsType = myKnownTypesCache.GetByClrTypeName(ourImplicitUseTargetFlags, element.Module);
                var args = new[]
                {
                    new AttributeValue(ConstantValue.Enum(ConstantValue.Int(3, element.Module), flagsType))
                };
                collection = new[]
                {
                    new SpecialAttributeInstance(ourUsedImplicitlyAttributeFullName, GetModule(element), () => args)
                };
                return true;
            }

            return false;
        }

        private bool CalculateMeansImplicitUse(ITypeElement typeElement)
        {
            var resultMap = typeElement is ICompiledElement ? myCompiledElementsCache : mySourceElementsCache;
            if (resultMap.TryGetFromCache(typeElement, out var result))
                return result;

            result = CalculateMeansImplicitUseInner(typeElement);
            resultMap.AddToCache(typeElement, result);

            return result;
        }

        private bool CalculateMeansImplicitUseInner(ITypeElement typeElement)
        {
            var meansImplicitUse = typeElement.GetAttributeInstances(ourMeansImplicitUseAttribute, false)
                .FirstOrDefault();
            if (meansImplicitUse?.Constructor == null || !meansImplicitUse.Constructor.IsDefault)
            {
                return false;
            }

            return true;
        }

        private IPsiModule GetModule(IClrDeclaredElement element)
        {
            return myExternalAnnotationsModuleFactory.GetPsiModule(element.Module.TargetFrameworkId) ?? element.Module;
        }

        public void Invalidate(PsiChangedElementType changeType)
        {
            if (changeType == PsiChangedElementType.CompiledContentsChanged)
            {
                myCompiledElementsCache.Clear();
            }

            mySourceElementsCache.Clear();
        }
    }
}
