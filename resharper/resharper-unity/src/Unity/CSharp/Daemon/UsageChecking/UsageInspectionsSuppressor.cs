#nullable enable

using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Explicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.UsageChecking
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
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

        public ImplicitUseFlags SuppressUsageInspectionsOnElement(IDeclaredElement element)
        {
            if (!element.IsFromUnityProject()) return ImplicitUseFlags.Empty;

            var solution = element.GetSolution();
            var unityApi = solution.GetComponent<UnityApi>();

            switch (element)
            {
                case IClass cls when unityApi.IsUnityType(cls) ||
                                     cls.IsDotsImplicitlyUsedType() ||
                                     IsUxmlFactory(cls) ||
                                     unityApi.IsOdinType(cls):
                    return new ImplicitUseFlags(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature);

                case IStruct @struct when unityApi.IsUnityType(@struct) ||
                                     @struct.IsDotsImplicitlyUsedType() :
                    return new ImplicitUseFlags(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature);

                case ITypeElement typeElement when unityApi.IsSerializableTypeDeclaration(typeElement).HasFlag(SerializedFieldStatus.SerializedField):
                    // TODO: We should only really mark it as in use if it's actually used somewhere
                    // That is, it should be used as a field in a Unity type, or another serializable type
                    return new ImplicitUseFlags(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature);

                case ITypeElement typeElement when IsImplicitlyUsedInterfaceType(typeElement):
                    return new ImplicitUseFlags(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature);

                case IMethod method:
                    var function = unityApi.GetUnityEventFunction(method, out var match);
                    if (function != null)
                    {
                        if (match == MethodSignatureMatch.ExactMatch)
                        {
                            return HasOptionalParameter(function)
                                ? new ImplicitUseFlags(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)
                                : new ImplicitUseFlags(ImplicitUseKindFlags.Access);
                        }

                        return ImplicitUseFlags.Empty;
                    }

                    if (IsEventHandler(unityApi, method) ||
                        IsRequiredSignatureMethod(method) ||
                        IsAnimationEvent(solution, method) ||
                        IsImplicitlyUsedInterfaceMethod(method) ||
                        IsImplicitlyUsedByInputActions(solution, method))
                    {
                        return new ImplicitUseFlags(ImplicitUseKindFlags.Access);
                    }
                    break;

                case IField field when unityApi.IsSerialisedField(field).HasFlag(SerializedFieldStatus.SerializedField):
                    return new ImplicitUseFlags(ImplicitUseKindFlags.Assign);

                case IField field when unityApi.IsOdinInspectorField(field):
                    return new ImplicitUseFlags(ImplicitUseKindFlags.Assign);

                case IProperty property when IsEventHandler(unityApi, property.Setter) ||
                                             IsImplicitlyUsedInterfaceProperty(property) ||
                                             IsAnimationEvent(solution, property) ||
                                             unityApi.IsSerialisedAutoProperty(property, useSwea:true).HasFlag(SerializedFieldStatus.SerializedField):
                    return new ImplicitUseFlags(ImplicitUseKindFlags.Assign);

                case IProperty property when unityApi.IsOdinInspectorProperty(property):
                    return new ImplicitUseFlags(ImplicitUseKindFlags.Assign);

                case IParameter parameter
                    when parameter.IsRefMember() && parameter.GetContainingType().IsDotsImplicitlyUsedType():
                    return new ImplicitUseFlags(ImplicitUseKindFlags.Assign);
            }

            return ImplicitUseFlags.Empty;
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

        private static bool IsAnimationEvent(ISolution solution, IDeclaredElement element)
        {
            return solution
                .GetComponent<AnimExplicitUsagesContainer>()
                .GetEventUsagesCountFor(element, out var isEstimatedResult) > 0 || isEstimatedResult
                ||
                (element is IMethod method && solution.GetComponent<AnimImplicitUsagesContainer>()
                .LikelyUsed(method));
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
