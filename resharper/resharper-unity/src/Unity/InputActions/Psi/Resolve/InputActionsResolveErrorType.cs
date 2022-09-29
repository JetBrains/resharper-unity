using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Resolve
{
    public class InputActionsResolveErrorType : ResolveErrorType
    {
        private InputActionsResolveErrorType([NotNull] string name)
            : base(name)
        {
        }

        [NotNull] public static readonly ResolveErrorType UNRESOLVED_REFERENCED_INPUTACTIONS_ERROR =
            new InputActionsResolveErrorType("UNRESOLVED_REFERENCED_INPUTACTIONS_ERROR");
    }
}