using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.UsageChecking
{
    [ShellComponent]
    public class UsageInspectionsSuppressor : IUsageInspectionsSuppressor
    {
        private readonly ILogger myLogger;

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

                case ITypeElement typeElement when unityApi.IsSerializableType(typeElement):
                    // TODO: We should only really mark it as in use if it's actually used somewhere
                    // That is, it should be used as a field in a Unity type, or another serializable type
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
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
                    break;

                case IField field when unityApi.IsSerialisedField(field) || unityApi.IsInjectedField(field):
                    // comment for serialized field:
                    // Public fields gets exposed to the Unity Editor and assigned from the UI.
                    // But it still should be checked if the field is ever accessed from the code.
                    flags = ImplicitUseKindFlags.Assign;
                    return true;
                
                case IProperty property when IsEventHandler(unityApi, property.Setter):
                    flags = ImplicitUseKindFlags.Assign;
                    return true;
            }

            flags = ImplicitUseKindFlags.Default;   // Value not used if we return false
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
                return unityApi.IsPotentialEventHandler(method);

            return method.GetSolution().GetComponent<UnityEventHandlerReferenceCache>().IsEventHandler(method);
        }
    }
}

