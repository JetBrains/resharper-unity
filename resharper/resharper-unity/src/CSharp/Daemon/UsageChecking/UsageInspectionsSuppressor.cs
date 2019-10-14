using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.UsageChecking
{
    [ShellComponent]
    public class UsageInspectionsSuppressor : IUsageInspectionsSuppressor
    {
        private readonly ILogger myLogger;

        private readonly List<IClrTypeName> myImplicitlyUsedInterfaces = new List<IClrTypeName>()
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

        public UsageInspectionsSuppressor(ILogger logger)
        {
            myLogger = logger;
        }

        public bool SuppressUsageInspectionsOnElement(IDeclaredElement element, out ImplicitUseKindFlags flags)
        {
            flags = ImplicitUseKindFlags.Default;

            try
            {
                if (!element.IsFromUnityProject()) return false;
            }
            catch (Exception e)
            {
                /*
                 * TODO: radically rethink Unity / non-Unity project detection.
                 * Currently we check project's assemblies using extensions for IProject,
                 * with no way to log errors and/or react to targetFrameworkChanges should they happen on the fly.
                 * This should be replaced with something more stable and fast.
                 */
                myLogger.LogExceptionSilently(e);
                return false;
            }

            var solution = element.GetSolution();
            var unityApi = solution.GetComponent<UnityApi>();

            switch (element)
            {
                case IClass cls when unityApi.IsUnityType(cls) || unityApi.IsUnityECSType(cls):
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;


                case ITypeElement typeElement when IsImplicitlyUsedInterfaceType(typeElement):
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;

                case ITypeElement typeElement when unityApi.IsSerializableType(typeElement):
                    // TODO: We should only really mark it as in use if it's actually used somewhere
                    // That is, it should be used as a field in a Unity type, or another serializable type
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;

                case IMethod method when IsImplicitlyUsedInterfaceMethod(method):
                    flags = ImplicitUseKindFlags.Access;
                    return true;

                case IMethod method:
                    var function = unityApi.GetUnityEventFunction(method, out var match);
                    if (function != null)
                    {
                        if (match == MethodSignatureMatch.ExactMatch)
                        {
                            foreach (var parameter in function.Parameters)
                            {
                                if (parameter.IsOptional)
                                {
                                    // Allows optional parameters to be marked as unused
                                    // TODO: Might need to process IParameter if optional gets more complex
                                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                                    return true;
                                }
                            }

                            flags = ImplicitUseKindFlags.Access;
                            return true;
                        }

                        return false;
                    }

                    if (IsEventHandler(unityApi, method))
                    {
                        flags = ImplicitUseKindFlags.Access;
                        return true;
                    }

                    if (IsSettingsProvider(method))
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

        private static bool IsSettingsProvider(IMethod method)
        {
            if (method.HasAttributeInstance(KnownTypes.SettingsProviderAttribute, AttributesSource.All))
            {
                if (method.ReturnType.IsImplicitlyConvertibleTo(TypeFactory.CreateTypeByCLRName(KnownTypes.SettingsProvider, method.Module),
                    new XamlWinRTTypeConversionRule(method.Module)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsImplicitlyUsedInterfaceMethod(IMethod method)
        {
            foreach (var implicitlyUsedTypeName in myImplicitlyUsedInterfaces)
            {
                var type = TypeFactory.CreateTypeByCLRName(implicitlyUsedTypeName, method.Module).GetTypeElement();

                if (type == null)
                    return false;
                
                foreach (var overridableMemberInstance in method.GetRootSuperMembers())
                {
                    if (type.GetClrName().ShortName == overridableMemberInstance.DeclaringType.GetClrName().ShortName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsImplicitlyUsedInterfaceProperty(IProperty property)
        {
            foreach (var implicitlyUsedTypeName in myImplicitlyUsedInterfaces)
            {
                var type = TypeFactory.CreateTypeByCLRName(implicitlyUsedTypeName, property.Module).GetTypeElement();

                if (type == null)
                    continue;
                
                foreach (var overridableMemberInstance in property.GetRootSuperMembers())
                {
                    if (type.GetClrName().ShortName == overridableMemberInstance.DeclaringType.GetClrName().ShortName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsImplicitlyUsedInterfaceType(ITypeElement typeElement)
        {
            foreach (var implicitlyUsedTypeName in myImplicitlyUsedInterfaces)
            {
                var type = TypeFactory.CreateTypeByCLRName(implicitlyUsedTypeName, typeElement.Module).GetTypeElement();

                if (type == null)
                    return false;
                
                if (typeElement.DerivesFrom(type.GetClrName()))
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
            var yamlParsingEnabled = solution.GetComponent<UnityYamlSupport>().IsUnityYamlParsingEnabled;

            // TODO: These two are usually used together. Consider combining in some way
            if (!yamlParsingEnabled.Value || !assetSerializationMode.IsForceText)
                return unityApi.IsPotentialEventHandler(method, false); // if yaml parsing is disabled, we will consider private methods as unused

            return method.GetSolution().GetComponent<UnityEventHandlerReferenceCache>().IsEventHandler(method);
        }
    }
}