using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.ReSharper.Psi.Impl.Reflection2.ExternalAnnotations;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

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
        private static readonly IClrTypeName ourImplicitUseTargetFlags = new ClrTypeName(typeof(ImplicitUseTargetFlags).FullName);
        // ReSharper restore AssignNullToNotNullAttribute

        private readonly IPredefinedTypeCache myPredefinedTypeCache;
        private readonly UnityApi myUnityApi;
        private readonly IPsiModule myAnnotationsPsiModule;

        public CustomCodeAnnotationProvider(ExternalAnnotationsModuleFactory externalAnnotationsModuleFactory, IPredefinedTypeCache predefinedTypeCache, UnityApi unityApi)
        {
            myPredefinedTypeCache = predefinedTypeCache;
            myUnityApi = unityApi;
            myAnnotationsPsiModule = externalAnnotationsModuleFactory
                .GetPsiModule(TargetFrameworkId.Default);
        }

        public CodeAnnotationNullableValue? GetNullableAttribute(IDeclaredElement element)
        {
            return null;
        }

        public CodeAnnotationNullableValue? GetContainerElementNullableAttribute(
            IDeclaredElement element)
        {
            return null;
        }

        public ICollection<IAttributeInstance> GetSpecialAttributeInstances(IClrDeclaredElement element)
        {
            if (GetCoroutineMustUseReturnValueAttribute(element, out var collection)) return collection;
            if (GetPublicAPIImplicitlyUsedAttribute(element, out collection)) return collection;
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

            collection = new[]
            {
                new SpecialAttributeInstance(
                    ourMustUseReturnValueAttributeFullName, myAnnotationsPsiModule, () => new[]
                    {
                        new AttributeValue(
                            new ConstantValue("Coroutine will not continue if return value is ignored",
                                predefinedType.String)),
                    })
            };
            return true;
        }

        // Unity ships annotations, but they're out of date. Specifically, the [PublicAPI] attribute is
        // defined with [MeansImplicitUse] instead of [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
        // So if applied to a class, only the class is marked as in use, while the members aren't. This
        // provider will apply [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)] to any type that
        // has the old [PublicAPI] applied
        private static bool GetPublicAPIImplicitlyUsedAttribute(IClrDeclaredElement element,
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

                var flagsType = TypeFactory.CreateTypeByCLRName(ourImplicitUseTargetFlags, element.Module);
                collection = new[]
                {
                    new SpecialAttributeInstance(
                        ourUsedImplicitlyAttributeFullName, element.Module, () => new[]
                            {new AttributeValue(new ConstantValue(3, flagsType))}
                    )
                };
                return true;
            }

            return false;
        }
    }
}