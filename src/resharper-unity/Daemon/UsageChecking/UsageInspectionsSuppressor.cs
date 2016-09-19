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
            // TODO: Only do any work if the element belongs to a project that references Unity.Engine

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
            if (method != null && unityApi.IsUnityMessage(method))
            {
                flags = ImplicitUseKindFlags.Access;
                return true;
            }

            var field = element as IField;
            if (field != null && field.GetAccessRights() == AccessRights.PUBLIC)
            {
                var containingType = field.GetContainingType();
                if (containingType != null && unityApi.IsUnityType(containingType))
                {
                    // Public fields gets exposed to the Unity Editor and assigned from the UI. But it still should be checked if the field is ever accessed from the code.
                    flags = ImplicitUseKindFlags.Assign;
                    return true;
                }
            }

            flags = ImplicitUseKindFlags.Default;   // Value not used if we return false
            return false;
        }
    }
}
