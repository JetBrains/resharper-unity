using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve
{
    public class AsmDefResolveErrorType : ResolveErrorType
    {
        private AsmDefResolveErrorType([NotNull] string name)
            : base(name)
        {
        }

        [NotNull] public static readonly ResolveErrorType UNRESOLVED_REFERENCED_ASMDEF_ERROR =
            new AsmDefResolveErrorType("UNRESOLVED_REFERENCED_ASMDEF_ERROR");
    }
}