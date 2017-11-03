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

namespace JetBrains.ReSharper.Plugins.Unity.Psi.CodeAnnotations
{
    [SolutionComponent]
    public class CustomCodeAnnotationProvider : ICustomCodeAnnotationProvider
    {
        private static readonly IClrTypeName OurMustUseReturnValueAttributeFullName = new ClrTypeName(typeof(MustUseReturnValueAttribute).FullName);

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
            var method = element as IMethod;
            var type = method?.GetContainingType();
            if (type != null && myUnityApi.IsUnityType(type))
            {
                var returnType = method.ReturnType;
                var predefinedType = myPredefinedTypeCache.GetOrCreatePredefinedType(element.Module);
                if (Equals(returnType, predefinedType.IEnumerator))
                {
                    var @string = predefinedType.String;
                    return new[]
                    {
                        new SpecialAttributeInstance(
                            OurMustUseReturnValueAttributeFullName, myAnnotationsPsiModule, () => new[]
                            {
                                new AttributeValue(new ConstantValue("Coroutine will not continue if return value is ignored", @string)),
                            })
                    };
                }
            }
            return EmptyList<IAttributeInstance>.Instance;
        }
    }
}