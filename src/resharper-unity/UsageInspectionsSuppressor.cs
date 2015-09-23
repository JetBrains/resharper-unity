using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ShellComponent]
    public class UsageInspectionsSuppressor : IUsageInspectionsSuppressor
    {
        public bool SuppressUsageInspectionsOnElement(IDeclaredElement element, out ImplicitUseKindFlags flags)
        {
            // TODO: Only do any work if the element belongs to a project that references Unity.Engine

            var cls = element as IClass;
            if (cls != null)
            {
                if(MonoBehaviourUtil.IsMonoBehaviourType(cls, cls.Module, cls.ResolveContext))
                {
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;
                }
            }

            var method = element as IMethod;
            if (method != null && MonoBehaviourUtil.IsMonoBehaviourMessage(method.ShortName))
            {
                var containingType = method.GetContainingType();
                if (containingType != null && MonoBehaviourUtil.IsMonoBehaviourType(containingType, method.Module, method.ResolveContext))
                {
                    flags = ImplicitUseKindFlags.Access;
                    return true;
                }
            }

            var field = element as IField;
            if (field != null && field.GetAccessRights() == AccessRights.PUBLIC)
            {
                var containingType = field.GetContainingType();
                if (containingType != null && MonoBehaviourUtil.IsMonoBehaviourType(containingType, field.Module, field.ResolveContext))
                {
                    // Public fields gets exposed to the Unity Editor and assigned from the UI. But it still should be checked if the field is ever accessed from the code.
                    flags = ImplicitUseKindFlags.Assign;
                    return true;
                }
            }

            flags = ImplicitUseKindFlags.Default;
            return false;
        }
    }
}
