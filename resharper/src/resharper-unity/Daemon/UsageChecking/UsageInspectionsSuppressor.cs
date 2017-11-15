using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.UsageChecking
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

            if (element is IClass cls)
            {
                if(unityApi.IsUnityType(cls))
                {
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;
                }
            }

            if (element is IMethod method)
            {
                var function = unityApi.GetUnityEventFunction(method, out var match);
                if (function != null && match == EventFunctionMatch.ExactMatch)
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
            }

            if (element is IField field && unityApi.IsUnityField(field))
            {
                // Public fields gets exposed to the Unity Editor and assigned from the UI.
                // But it still should be checked if the field is ever accessed from the code.
                flags = ImplicitUseKindFlags.Assign;
                return true;
            }

            flags = ImplicitUseKindFlags.Default;   // Value not used if we return false
            return false;
        }
    }
}
