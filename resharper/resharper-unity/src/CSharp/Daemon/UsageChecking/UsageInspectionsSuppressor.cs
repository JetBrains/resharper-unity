using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.UsageChecking
{
    [ShellComponent]
    public class UsageInspectionsSuppressor : IUsageInspectionsSuppressor
    {
        private readonly JetHashSet<IClrTypeName> myImplicitlyUsedInterfaces = new JetHashSet<IClrTypeName>
        {
            new ClrTypeName("UnityEditor.Build.IPreprocessBuild"),
            new ClrTypeName("UnityEditor.Build.IPostprocessBuild"),
            new ClrTypeName("UnityEditor.Build.IProcessScene"),
            new ClrTypeName("UnityEditor.Build.IProcessSceneWithReport"),
            new ClrTypeName("UnityEditor.Build.IActiveBuildTargetChanged"),
            new ClrTypeName("UnityEditor.Build.IFilterBuildAssemblies"),
            new ClrTypeName("UnityEditor.Build.IPostBuildPlayerScriptDLLs"),
            new ClrTypeName("UnityEditor.Build.IPostprocessBuildWithReport"),
            new ClrTypeName("UnityEditor.Build.IPreprocessBuildWithReport"),
            new ClrTypeName("UnityEditor.Build.IPreprocessShaders"),
            new ClrTypeName("UnityEditor.Build.IOrderedCallback"),
        };

        public bool SuppressUsageInspectionsOnElement(IDeclaredElement element, out ImplicitUseKindFlags flags)
        {
            flags = ImplicitUseKindFlags.Default;

            if (!element.IsFromUnityProject()) return false;

            var solution = element.GetSolution();
            var unityApi = solution.GetComponent<UnityApi>();

            switch (element)
            {
                case IClass cls when unityApi.IsUnityType(cls) || unityApi.IsUnityECSType(cls):
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;

                case ITypeElement typeElement when unityApi.IsSerializableTypeDeclaration(typeElement):
                    // TODO: We should only really mark it as in use if it's actually used somewhere
                    // That is, it should be used as a field in a Unity type, or another serializable type
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;

                case ITypeElement typeElement when IsImplicitlyUsedInterfaceType(typeElement):
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;

                case IMethod method when IsImplicitlyUsedInterfaceMethod(method):
                    flags = ImplicitUseKindFlags.Access;
                    return true;

                case IMethod method:
                    // flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    // return true;
                    var function = unityApi.GetUnityEventFunction(method, out var match);
                    if (function != null)
                    {
                        if (match == MethodSignatureMatch.ExactMatch)
                        {
                            flags = HasOptionalParameter(function)
                                ? ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature
                                : ImplicitUseKindFlags.Access;
                            return true;
                        }
                        
                        return false;
                    }
                    
                    if (IsEventHandler(unityApi, method) || IsRequiredSignatureMethod(method))
                    {
                        flags = ImplicitUseKindFlags.Access;
                        return true;
                    }
                    break;

                case IField field when unityApi.IsSerialisedField(field) || unityApi.IsInjectedField(field):
                    flags = ImplicitUseKindFlags.Assign;
                    return true;

                case IProperty property when IsEventHandler(unityApi, property.Setter) || IsImplicitlyUsedInterfaceProperty(property):
                    flags = ImplicitUseKindFlags.Assign;
                    return true;
            }

            flags = ImplicitUseKindFlags.Default; // Value not used if we return false
            return false;
        }

        private bool IsImplicitlyUsedInterfaceType(ITypeElement typeElement)
        {
            foreach (var implicitlyUsedTypeName in myImplicitlyUsedInterfaces)
            {
                if (typeElement.DerivesFrom(implicitlyUsedTypeName))
                    return true;
            }

            return false;
        }

        private bool IsImplicitlyUsedInterfaceMethod(IMethod method)
        {
            foreach (var overridableMemberInstance in method.GetRootSuperMembers())
            {
                if (myImplicitlyUsedInterfaces.Contains(overridableMemberInstance.DeclaringType.GetClrName()))
                    return true;
            }

            return false;
        }

        private bool IsImplicitlyUsedInterfaceProperty(IProperty property)
        {
            foreach (var overridableMemberInstance in property.GetRootSuperMembers())
            {
                if (myImplicitlyUsedInterfaces.Contains(overridableMemberInstance.DeclaringType.GetClrName()))
                    return true;
            }

            return false;
        }

        private bool HasOptionalParameter(UnityEventFunction function)
        {
            foreach (var parameter in function.Parameters)
            {
                if (parameter.IsOptional)
                    return true;
            }

            return false;
        }

        private bool IsEventHandler(UnityApi unityApi, [CanBeNull] IMethod method)
        {
            if (method == null)
                return false;

            var type = method.GetContainingType();
            if (!unityApi.IsUnityType(type))
                return false;

            var solution = method.GetSolution();
            var assetSerializationMode = solution.GetComponent<AssetSerializationMode>();
            var yamlParsingEnabled = solution.GetComponent<AssetIndexingSupport>().IsEnabled;

            // TODO: These two are usually used together. Consider combining in some way
            if (!yamlParsingEnabled.Value || !assetSerializationMode.IsForceText || !solution.GetComponent<DeferredCacheController>().CompletedOnce.Value)
                return unityApi.IsPotentialEventHandler(method, false); // if yaml parsing is disabled, we will consider private methods as unused

            var eventsCount = solution
                .GetComponent<UnityEventsElementContainer>()
                .GetAssetUsagesCount(method, out bool estimatedResult);
            var animationEventsCount = solution
                .GetComponent<AnimationEventUsagesContainer>()
                .GetEventUsagesCountFor(method);
            return eventsCount + animationEventsCount > 0 || estimatedResult;
        }

        // If the method is marked with an attribute that has a method that is itself marked with RequiredSignature,
        // then we know Unity will be implicitly using this method. Most attributes which mean implicit use are in the
        // external annotations, but this will capture attributes that we don't know about, as long as they follow the
        // pattern
        private static bool IsRequiredSignatureMethod(IMethod method)
        {
            foreach (var attributeInstance in method.GetAttributeInstances(AttributesSource.All))
            {
                var attributeType = attributeInstance.GetAttributeType().GetTypeElement();
                if (attributeType != null)
                {
                    foreach (var attributeMethod in attributeType.Methods)
                    {
                        if (attributeMethod.HasAttributeInstance(KnownTypes.RequiredSignatureAttribute,
                            AttributesSource.All))
                            return true;
                    }
                }
            }

            return false;
        }
    }
}