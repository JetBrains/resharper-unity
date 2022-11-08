#nullable enable

using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.UsageChecking
{
    [ShellComponent]
    public class UsageInspectionsSuppressor : IUsageInspectionsSuppressor
    {
        private readonly JetHashSet<IClrTypeName> myImplicitlyUsedInterfaces = new()
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

        private readonly JetHashSet<IClrTypeName> myUxmlFactoryBaseClasses = new()
        {
            new ClrTypeName("UnityEngine.UIElements.UxmlFactory`1"),
            new ClrTypeName("UnityEngine.UIElements.UxmlFactory`2"),
        };

        public bool SuppressUsageInspectionsOnElement(IDeclaredElement element, out ImplicitUseKindFlags flags)
        {
            flags = ImplicitUseKindFlags.Default;

            if (!element.IsFromUnityProject()) return false;

            var solution = element.GetSolution();
            var unityApi = solution.GetComponent<UnityApi>();

            switch (element)
            {
                case IClass cls when unityApi.IsUnityType(cls) ||
                                     UnityApi.IsDotsImplicitlyUsedType(cls) ||
                                     IsUxmlFactory(cls):
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;
                case IStruct @struct when unityApi.IsUnityType(@struct) ||
                                     UnityApi.IsDotsImplicitlyUsedType(@struct) :
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

                case IMethod method:
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

                    if (IsEventHandler(unityApi, method) ||
                        IsRequiredSignatureMethod(method) ||
                        IsAnimationEvent(solution, method) ||
                        IsImplicitlyUsedInterfaceMethod(method) ||
                        IsImplicitlyUsedByInputActions(solution, method))
                    {
                        flags = ImplicitUseKindFlags.Access;
                        return true;
                    }
                    break;

                case IField field when unityApi.IsSerialisedField(field):
                    flags = ImplicitUseKindFlags.Assign;
                    return true;

                case IProperty property when IsEventHandler(unityApi, property.Setter) ||
                                             IsImplicitlyUsedInterfaceProperty(property) ||
                                             IsAnimationEvent(solution, property) ||
                                             unityApi.IsSerialisedAutoProperty(property):
                    flags = ImplicitUseKindFlags.Assign;
                    return true;
            }

            flags = ImplicitUseKindFlags.Default; // Value not used if we return false
            return false;
        }

        private static bool IsImplicitlyUsedByInputActions(ISolution solution, IMethod method)
        {
            solution.GetComponent<InputActionsElementContainer>()
                .GetUsagesCountForFast(method, out var inputActionsUsagesResult);
            return inputActionsUsagesResult;
        }

        private bool IsUxmlFactory(IClass cls)
        {
            var baseClass = cls.GetBaseClassType();
            return baseClass != null && myUxmlFactoryBaseClasses.Contains(baseClass.GetClrName());
        }

        private static bool IsAnimationEvent(ISolution solution, IDeclaredElement property)
        {
            return solution
                .GetComponent<AnimationEventUsagesContainer>()
                .GetEventUsagesCountFor(property, out var isEstimatedResult) > 0 || isEstimatedResult;
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

        private static bool HasOptionalParameter(UnityEventFunction function)
        {
            foreach (var parameter in function.Parameters)
            {
                if (parameter.IsOptional)
                    return true;
            }

            return false;
        }

        private static bool IsEventHandler(UnityApi unityApi, IMethod? method)
        {
            if (method == null)
                return false;

            var type = method.ContainingType;
            if (!unityApi.IsUnityType(type))
                return false;

            var solution = method.GetSolution();
            var isForceText = solution.GetComponent<AssetSerializationMode>().IsForceText;
            var assetIndexingEnabled = solution.GetComponent<AssetIndexingSupport>().IsEnabled.Value;
            var deferredCacheCompletedOnce = solution.GetComponent<DeferredCacheController>().CompletedOnce.Value;

            if (!assetIndexingEnabled || !isForceText || !deferredCacheCompletedOnce)
                return unityApi.IsPotentialEventHandler(method, false); // if yaml parsing is disabled, we will consider private methods as unused

            var eventsCount = solution
                .GetComponent<UnityEventsElementContainer>()
                .GetAssetUsagesCount(method, out bool estimatedResult);
            return eventsCount > 0 || estimatedResult;
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
