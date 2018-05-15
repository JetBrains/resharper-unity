using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve
{
    public class AsmDefResolveErrorType : ResolveErrorType
    {
        private AsmDefResolveErrorType([NotNull] string name)
            : base(name)
        {
        }

        [NotNull] public static readonly ResolveErrorType ASMDEF_UNRESOLVED_REFERENCED_PROJECT_ERROR =
            new AsmDefResolveErrorType("ASMDEF_UNRESOLVED_REFERENCED_PROJECT_ERROR");
    }
}