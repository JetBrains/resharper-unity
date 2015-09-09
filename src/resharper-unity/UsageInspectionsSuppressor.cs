using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ShellComponent]
    public class UsageInspectionsSuppressor : IUsageInspectionsSuppressor
    {
        private static readonly IClrTypeName ourMonoBehaviourName = new ClrTypeName("UnityEngine.MonoBehaviour");

        public bool SuppressUsageInspectionsOnElement(IDeclaredElement element, out ImplicitUseKindFlags flags)
        {
            // TODO: Only do any work if the element belongs to a project that references Unity.Engine

            var @class = element as IClass;
            if (@class != null)
            {
                // TODO: Should the module + resolve context be for Unity.Engine.dll?
                // Then we could create a single type and reuse it
                var monoBehaviour = TypeFactory.CreateTypeByCLRName(ourMonoBehaviourName, @class.Module,
                    @class.ResolveContext).GetTypeElement();
                if (@class.IsDescendantOf(monoBehaviour))
                {
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;
                }
            }

            var method = element as IMethod;
            if (method != null)
            {
                // TODO: Only mark known methods as in use?
                var monoBehaviour = TypeFactory.CreateTypeByCLRName(ourMonoBehaviourName, method.Module,
                    method.ResolveContext).GetTypeElement();
                var containingType = method.GetContainingType();
                if (containingType != null && containingType.IsDescendantOf(monoBehaviour))
                {
                    flags = ImplicitUseKindFlags.Access;
                    return true;
                }
            }

            var field = element as IField;
            if (field != null && field.GetAccessRights() == AccessRights.PUBLIC)
            {
                var monoBehaviour = TypeFactory.CreateTypeByCLRName(ourMonoBehaviourName, field.Module,
                    field.ResolveContext).GetTypeElement();
                var containingType = field.GetContainingType();
                if (containingType != null && containingType.IsDescendantOf(monoBehaviour))
                {
                    // Fields get assigned automatically by Mono
                    flags = ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign;
                    return true;
                }
            }

            flags = ImplicitUseKindFlags.Default;
            return false;
        }
    }
}
