using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Components;
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
        private static readonly IClrTypeName ourValueRangeAttributeFullName = new ClrTypeName(typeof(ValueRangeAttribute).FullName);
        private static readonly IClrTypeName ourImplicitUseTargetFlags = new ClrTypeName(typeof(ImplicitUseTargetFlags).FullName);

        private readonly ExternalAnnotationsModuleFactory myExternalAnnotationsModuleFactory;
        private readonly IPredefinedTypeCache myPredefinedTypeCache;
        private readonly KnownTypesCache myKnownTypesCache;
        private readonly UnityApi myUnityApi;
        private readonly IImmutableEnumerable<IUnityRangeAttributeProvider> myUnityRangeAttributeProviders;

        private readonly DirectMappedCache<ITypeElement, bool> myCompiledElementsCache = new(10);
        private readonly DirectMappedCache<ITypeElement, bool> mySourceElementsCache = new(10);

        public CustomCodeAnnotationProvider(ExternalAnnotationsModuleFactory externalAnnotationsModuleFactory,
            IPredefinedTypeCache predefinedTypeCache, KnownTypesCache knownTypesCache, UnityApi unityApi,
            IImmutableEnumerable<IUnityRangeAttributeProvider> unityRangeAttributeProviders)
        {
            myPredefinedTypeCache = predefinedTypeCache;
            myKnownTypesCache = knownTypesCache;
            myUnityApi = unityApi;
            myUnityRangeAttributeProviders = unityRangeAttributeProviders;
            myExternalAnnotationsModuleFactory = externalAnnotationsModuleFactory;
        }

        public CodeAnnotationNullableValue? GetNullableAttribute(IDeclaredElement element) => null;
        public CodeAnnotationNullableValue? GetContainerElementNullableAttribute(IDeclaredElement element) => null;

        public ICollection<IAttributeInstance> GetSpecialAttributeInstances(IClrDeclaredElement element,
                                                                            AttributeInstanceCollection existingAttributes)
        {
            if (GetPublicAPIImplicitlyUsedAttribute(element, existingAttributes, out var collection)) return collection;
            if (GetValueRangeAttribute(element, existingAttributes, out collection)) return collection;

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

        // Treat Unity's RangeAttribute as ReSharper's ValueRangeAttribute annotation
        private bool GetValueRangeAttribute(IClrDeclaredElement element,
            AttributeInstanceCollection attributeInstanceCollection,
            out ICollection<IAttributeInstance> collection)
        {
            collection = EmptyList<IAttributeInstance>.InstanceList;

            if (!(element is IField field) || !element.IsFromUnityProject()) return false;

            if (myUnityApi.IsSerialisedField(field).HasFlag(SerializedFieldStatus.NonSerializedField))
                return false;

            // Integer value analysis only works on integers, but it will make use of annotations applied to values that
            // are convertible to int, such as byte/sbyte and short/ushort. It doesn't currently use values applied to
            // uint, or long/ulong, but it is planned, so we'll apply to all sizes of integer.
            var predefinedType = myPredefinedTypeCache.GetOrCreatePredefinedType(element.Module);
            if (!Equals(field.Type, predefinedType.Int) && !Equals(field.Type, predefinedType.Uint) &&
                !Equals(field.Type, predefinedType.Long) && !Equals(field.Type, predefinedType.Ulong) &&
                !Equals(field.Type, predefinedType.Short) && !Equals(field.Type, predefinedType.Ushort) &&
                !Equals(field.Type, predefinedType.Byte) && !Equals(field.Type, predefinedType.Sbyte))
            {
                return false;
            }

            foreach (var attributeInstance in attributeInstanceCollection.GetAllOwnAttributes())
            {
                foreach (var unityRangeAttributeProvider in myUnityRangeAttributeProviders)
                {
                    if (unityRangeAttributeProvider.IsApplicable(attributeInstance))
                    {
                        var from = unityRangeAttributeProvider.GetMinValue(attributeInstance);
                        var to = unityRangeAttributeProvider.GetMaxValue(attributeInstance);
                        
                        collection = CreateRangeAttributeInstance(element, from, to);
                        return true;
                    }
                }
            }

            return false;
        }

        private IAttributeInstance[] CreateRangeAttributeInstance(IClrDeclaredElement element, long from, long to)
        {
            var args = new[]
            {
                new AttributeValue(ConstantValue.Long(from, element.Module)),
                new AttributeValue(ConstantValue.Long(to, element.Module))
            };

            // We need a project for the resolve context. It's not actually used, but we still need it. The requested
            // element will be a source element, so it will definitely have a project
            if (element.Module.ContainingProjectModule is not IProject project)
                return EmptyArray<IAttributeInstance>.Instance;

            return new IAttributeInstance[]
            {
                new AnnotationAttributeInstance(ourValueRangeAttributeFullName, GetModule(element), project, args, myKnownTypesCache)
            };
        }

        private IPsiModule GetModule(IClrDeclaredElement element)
        {
            return myExternalAnnotationsModuleFactory.GetPsiModule(element.Module.TargetFrameworkId) ?? element.Module;
        }

        // We can't always use SpecialAttributeInstance because it resolves the IClrTypeName against the default
        // (project) context. As luck would have it, Unity ship *some* annotations, so it will work for some
        // annotations. However, if we need to use any annotations that aren't shipped (e.g. ValueRangeAttribute) then
        // it will fail to resolve. We need to resolve first against the project, and then fallback to the
        // ExternalAnnotations PsiModule.
        private class AnnotationAttributeInstance : IAttributeInstance
        {
            private readonly IClrTypeName myClrTypeName;
            private readonly IPsiModule myExternalAnnotationsModule;
            private readonly IProject myProject;
            private readonly AttributeValue[] myCtorArguments;
            private readonly KnownTypesCache myKnownTypesCache;

            public AnnotationAttributeInstance(IClrTypeName clrTypeName,
                                               IPsiModule externalAnnotationsModule,
                                               IProject project,
                                               AttributeValue[] ctorArguments,
                                               KnownTypesCache knownTypesCache)
            {
                myClrTypeName = clrTypeName;
                myExternalAnnotationsModule = externalAnnotationsModule;
                myProject = project;
                myCtorArguments = ctorArguments;
                myKnownTypesCache = knownTypesCache;
            }

            public IClrTypeName GetClrName() => myClrTypeName;
            public string GetAttributeShortName() => myClrTypeName.ShortName;

            public IDeclaredType GetAttributeType() =>
                myKnownTypesCache.GetByClrTypeName(myClrTypeName, myExternalAnnotationsModule);

            public IConstructor? Constructor
            {
                get
                {
                    var typeElement = GetAttributeType().GetTypeElement();
                    if (typeElement == null)
                    {
                        using (CompilationContextCookie.OverrideOrCreate(
                            myExternalAnnotationsModule.GetResolveContextEx(myProject)))
                        {
                            typeElement = GetAttributeType().GetTypeElement();
                        }

                        if (typeElement == null)
                            return null;
                    }

                    foreach (var constructor in typeElement.Constructors)
                    {
                        if (myCtorArguments.Length == 0)
                        {
                            if (constructor.IsDefault) return constructor;

                            continue;
                        }

                        var parameters = constructor.Parameters;
                        if (parameters.Count == myCtorArguments.Length)
                        {
                            var typesMatch = true;
                            for (var index = 0; index < myCtorArguments.Length; index++)
                            {
                                var parameterType = parameters[index].Type;
                                var argumentType = myCtorArguments[index].GetType(myExternalAnnotationsModule);
                                if (!parameterType.Equals(argumentType))
                                {
                                    typesMatch = false;
                                    break;
                                }
                            }

                            if (typesMatch) return constructor;
                        }
                    }

                    return null;
                }
            }

            public int PositionParameterCount => myCtorArguments.Length;
            public IEnumerable<AttributeValue> PositionParameters() => myCtorArguments;
            public AttributeValue PositionParameter(int paramIndex)
            {
                return paramIndex < myCtorArguments.Length ? myCtorArguments[paramIndex] : AttributeValue.BAD_VALUE;
            }

            public int NamedParameterCount => 0;
            public IEnumerable<Pair<string, AttributeValue>> NamedParameters() =>
                EmptyList<Pair<string, AttributeValue>>.Enumerable;
            public AttributeValue NamedParameter(string name) => AttributeValue.BAD_VALUE;
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
