using System.Linq;
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
                if(MonoBehaviourUtil.IsMonoBehaviourType(cls, cls.Module))
                {
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;
                }
            }

            var method = element as IMethod;
            if (method != null && MonoBehaviourUtil.IsEventHandler(method.ShortName))
            {
                var containingType = method.GetContainingType();
                if (containingType != null && MonoBehaviourUtil.IsMonoBehaviourType(containingType, method.Module))
                {
                    flags = ImplicitUseKindFlags.Access;
                    return true;
                }
            }

            flags = ImplicitUseKindFlags.Default;
            return InspectField(element as IField, ref flags);
        }

        private static bool InspectField([CanBeNull] ITypeMember field, ref ImplicitUseKindFlags flags)
        {
            ITypeElement containingType = field?.GetContainingType();
            if (containingType == null) return false;

            bool serializable = containingType.HasAttribute("System.SerializableAttribute");
            bool unityObject = containingType.GetSuperTypes().Any(t => t.GetClrName().FullName == "UnityEngine.Object");
            if (!serializable && !unityObject) return false;

            if (field.GetAccessRights() == AccessRights.PUBLIC ||
                field.HasAttribute("UnityEngine.SerializeField"))
            {
                flags = ImplicitUseKindFlags.Assign;
                return true;
            }

            return false;
        }
    }
}
