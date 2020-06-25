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

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations
{
    [SolutionComponent]
    public class CustomCodeAnnotationProvider : ICustomCodeAnnotationProvider
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

        public CustomCodeAnnotationProvider(ExternalAnnotationsModuleFactory externalAnnotationsModuleFactory, IPredefinedTypeCache predefinedTypeCache, UnityApi unityApi)
        {
            myPredefinedTypeCache = predefinedTypeCache;
            myUnityApi = unityApi;
            myExternalAnnotationsModuleFactory = externalAnnotationsModuleFactory;
        }

        public CodeAnnotationNullableValue? GetNullableAttribute(IDeclaredElement element)
        {
            return null;
        }

        public CodeAnnotationNullableValue? GetContainerElementNullableAttribute(IDeclaredElement element)
        {
            return null;
        }

        public ICollection<IAttributeInstance> GetSpecialAttributeInstances(IClrDeclaredElement element)
        {
            if (GetCoroutineMustUseReturnValueAttribute(element, out var collection)) return collection;
            if (GetPublicAPIImplicitlyUsedAttribute(element, out collection)) return collection;
            if (GetValueRangeAttribute(element, out collection)) return collection;

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
            out ICollection<IAttributeInstance> collection)
        {
            collection = EmptyList<IAttributeInstance>.InstanceList;

            if (!(element is ITypeElement type) || !element.IsFromUnityProject()) return false;

            foreach (var attributeInstance in type.GetAttributeInstances(ourPublicAPIAttribute, false))
            {
                var attributeType = attributeInstance.GetAttributeType();
                if (!(attributeType.Resolve().DeclaredElement is ITypeElement typeElement)) continue;

                var meansImplicitUse = typeElement.GetAttributeInstances(ourMeansImplicitUseAttribute, false)
                    .FirstOrDefault();
                if (meansImplicitUse?.Constructor == null || !meansImplicitUse.Constructor.IsDefault) continue;

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

        // Treat Unity's RangeAttribute as ReSharper's ValueRangeAttribute annotation
        private bool GetValueRangeAttribute(IClrDeclaredElement element,
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

            foreach (var attributeInstance in field.GetAttributeInstances(KnownTypes.RangeAttribute, false))
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

            foreach (var attributeInstance in field.GetAttributeInstances(KnownTypes.MinAttribute, false))
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

        private SpecialAttributeInstance[] CreateRangeAttributeInstance(IClrDeclaredElement element,
                                                                        PredefinedType predefinedType,
                                                                        long from, long to)
        {
            var args = new[]
            {
                new AttributeValue(new ConstantValue(from, predefinedType.Long)),
                new AttributeValue(new ConstantValue(to, predefinedType.Long))
            };

            return new[] {new SpecialAttributeInstance(ourValueRangeAttributeFullName, GetModule(element), () => args)};
        }

        private IPsiModule GetModule(IClrDeclaredElement element)
        {
            return myExternalAnnotationsModuleFactory.GetPsiModule(element.Module.TargetFrameworkId) ?? element.Module;
        }
    }
}