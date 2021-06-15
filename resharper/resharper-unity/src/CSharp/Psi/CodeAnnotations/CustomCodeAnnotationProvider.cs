using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.ReSharper.Psi.Impl.Reflection2.ExternalAnnotations;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.Caches;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations
{
    [SolutionComponent]
    public class CustomCodeAnnotationProvider : ICustomCodeAnnotationProvider, IInvalidatingCache
    {
        // ReSharper disable AssignNullToNotNullAttribute
        private static readonly IClrTypeName ourMustUseReturnValueAttributeFullName = new ClrTypeName(typeof(MustUseReturnValueAttribute).FullName);
        private static readonly IClrTypeName ourMeansImplicitUseAttribute = new ClrTypeName(typeof(MeansImplicitUseAttribute).FullName);
        private static readonly IClrTypeName ourPublicAPIAttribute = new ClrTypeName(typeof(PublicAPIAttribute).FullName);
        private static readonly IClrTypeName ourUsedImplicitlyAttributeFullName = new ClrTypeName(typeof(UsedImplicitlyAttribute).FullName);
        private static readonly IClrTypeName ourValueRangeAttributeFullName = new ClrTypeName(typeof(ValueRangeAttribute).FullName);
        private static readonly IClrTypeName ourImplicitUseTargetFlags = new ClrTypeName(typeof(ImplicitUseTargetFlags).FullName);
        // ReSharper restore AssignNullToNotNullAttribute

        private readonly ExternalAnnotationsModuleFactory myExternalAnnotationsModuleFactory;
        private readonly IPredefinedTypeCache myPredefinedTypeCache;
        private readonly UnityApi myUnityApi;

        [NotNull] private readonly DirectMappedCache<ITypeElement, bool> myCompiledElementsCache = new DirectMappedCache<ITypeElement, bool>(10);
        [NotNull] private readonly DirectMappedCache<ITypeElement, bool> mySourceElementsCache = new DirectMappedCache<ITypeElement, bool>(10);
        
        public CustomCodeAnnotationProvider(ExternalAnnotationsModuleFactory externalAnnotationsModuleFactory,
            IPredefinedTypeCache predefinedTypeCache, UnityApi unityApi)
        {
            myPredefinedTypeCache = predefinedTypeCache;
            myUnityApi = unityApi;
            myExternalAnnotationsModuleFactory = externalAnnotationsModuleFactory;
        }

        public CodeAnnotationNullableValue? GetNullableAttribute(IDeclaredElement element) => null;
        public CodeAnnotationNullableValue? GetContainerElementNullableAttribute(IDeclaredElement element) => null;

        public ICollection<IAttributeInstance> GetSpecialAttributeInstances(IClrDeclaredElement element,
                                                                            AttributeInstanceCollection existingAttributes)
        {
            if (GetCoroutineMustUseReturnValueAttribute(element, out var collection)) return collection;
            if (GetPublicAPIImplicitlyUsedAttribute(element, existingAttributes, out collection)) return collection;
            if (GetValueRangeAttribute(element,existingAttributes, out collection)) return collection;

            return EmptyList<IAttributeInstance>.Instance;
        }

        private bool GetCoroutineMustUseReturnValueAttribute(IClrDeclaredElement element,
                                                             out ICollection<IAttributeInstance> collection)
        {
            collection = EmptyList<IAttributeInstance>.Instance;

            var method = element as IMethod;
            var type = method?.GetContainingType();
            if (type == null || !myUnityApi.IsUnityType(type)) return false;

            var returnType = method.ReturnType;
            var predefinedType = myPredefinedTypeCache.GetOrCreatePredefinedType(element.Module);
            if (!Equals(returnType, predefinedType.IEnumerator)) return false;

            // The ctorArguments lambda result is not cached, so let's allocate everything up front
            var args = new[]
            {
                new AttributeValue(
                    new ConstantValue("Coroutine will not continue if return value is ignored",
                        predefinedType.String))
            };
            collection = new[]
            {
                new SpecialAttributeInstance(ourMustUseReturnValueAttributeFullName, GetModule(element), () => args)
            };
            return true;
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

            if (!(element is ITypeElement type) || !element.IsFromUnityProject()) return false;

            foreach (var attributeInstance in attributeInstanceCollection.GetAllOwnAttributes().Where(t => t.GetClrName().Equals(ourPublicAPIAttribute)))
            {
                var attributeType = attributeInstance.GetAttributeType();
                if (!(attributeType.Resolve().DeclaredElement is ITypeElement typeElement)) continue;

                if (!CalculateMeansImplicitUse(typeElement))
                    continue;

                // The ctorArguments lambda result is not cached, so let's allocate everything up front
                var flagsType = TypeFactory.CreateTypeByCLRName(ourImplicitUseTargetFlags, element.Module);
                var args = new[] {new AttributeValue(new ConstantValue(3, flagsType))};
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

            if (!myUnityApi.IsSerialisedField(field))
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

            foreach (var attributeInstance in attributeInstanceCollection.GetAllOwnAttributes().Where(t => t.GetClrName().Equals(KnownTypes.RangeAttribute)))
            {
                // Values are floats, but applied to an integer field. Convert to integer values
                var unityFrom = attributeInstance.PositionParameter(0);
                var unityTo = attributeInstance.PositionParameter(1);

                if (!unityFrom.IsConstant || !unityFrom.ConstantValue.IsFloat() ||
                    !unityTo.IsConstant || !unityTo.ConstantValue.IsFloat())
                {
                    continue;
                }

                // The check above means this is not null. We take the floor, because that's how Unity works.
                // E.g. Unity's Inspector treats [Range(1.7f, 10.9f)] as between 1 and 10 inclusive
                var from = Convert.ToInt64(Math.Floor((float) unityFrom.ConstantValue.Value.NotNull()));
                var to = Convert.ToInt64(Math.Floor((float) unityTo.ConstantValue.Value.NotNull()));

                collection = CreateRangeAttributeInstance(element, predefinedType, from, to);
                return true;
            }

            foreach (var attributeInstance in attributeInstanceCollection.GetAllOwnAttributes().Where(t => t.GetClrName().Equals(KnownTypes.MinAttribute)))
            {
                var unityMinValue = attributeInstance.PositionParameter(0);

                if (!unityMinValue.IsConstant || !unityMinValue.ConstantValue.IsFloat())
                    continue;

                // Even though the constructor for ValueRange takes long, it only works with int.MaxValue
                var min = Convert.ToInt64(Math.Floor((float) unityMinValue.ConstantValue.Value.NotNull()));
                var max = int.MaxValue;

                collection = CreateRangeAttributeInstance(element, predefinedType, min, max);
                return true;
            }

            return false;
        }

        [NotNull]
        private IAttributeInstance[] CreateRangeAttributeInstance(IClrDeclaredElement element,
                                                                  PredefinedType predefinedType,
                                                                  long from, long to)
        {
            var args = new[]
            {
                new AttributeValue(new ConstantValue(from, predefinedType.Long)),
                new AttributeValue(new ConstantValue(to, predefinedType.Long))
            };

            // We need a project for the resolve context. It's not actually used, but we still need it. The requested
            // element will be a source element, so it will definitely have a project
            if (!(element.Module.ContainingProjectModule is IProject project))
                return EmptyArray<IAttributeInstance>.Instance;

            return new IAttributeInstance[]
            {
                new AnnotationAttributeInstance(ourValueRangeAttributeFullName, GetModule(element), project, args)
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

            public AnnotationAttributeInstance(IClrTypeName clrTypeName, IPsiModule externalAnnotationsModule,
                IProject project, AttributeValue[] ctorArguments)
            {
                myClrTypeName = clrTypeName;
                myExternalAnnotationsModule = externalAnnotationsModule;
                myProject = project;
                myCtorArguments = ctorArguments;
            }

            public IClrTypeName GetClrName() => myClrTypeName;
            public string GetAttributeShortName() => myClrTypeName.ShortName;

            public IDeclaredType GetAttributeType()
            {
                return TypeFactory.CreateTypeByCLRName(myClrTypeName, NullableAnnotation.NotNullable,
                    myExternalAnnotationsModule);
            }

            public IConstructor Constructor
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