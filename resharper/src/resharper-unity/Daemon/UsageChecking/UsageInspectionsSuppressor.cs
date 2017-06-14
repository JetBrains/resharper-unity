using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.UsageChecking
{
    [ShellComponent]
    public class UsageInspectionsSuppressor : IUsageInspectionsSuppressor
    {
        public bool SuppressUsageInspectionsOnElement(IDeclaredElement element, out ImplicitUseKindFlags flags)
        {
            flags = ImplicitUseKindFlags.Default;

            if (!element.IsFromUnityProject()) return false;

            var solution = element.GetSolution();
            var unityApi = solution.GetComponent<UnityApi>();

            var cls = element as IClass;
            if (cls != null)
            {
                if(unityApi.IsUnityType(cls))
                {
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;
                }
            }

            var method = element as IMethod;
            if (method != null)
            {
                EventFunctionMatch match;
                var function = unityApi.GetUnityEventFunction(method, out match);
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

            var field = element as IField;
            if (field != null && unityApi.IsUnityField(field))
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
