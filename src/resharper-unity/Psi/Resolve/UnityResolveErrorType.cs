using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    public class UnityResolveErrorType : ResolveErrorType
    {
        private UnityResolveErrorType([NotNull] string name)
            : base(name)
        {
        }

        // We want to associate a highlight with various unresolved errors,
        // such as referencing a method with an incorrect signature in a
        // string literal (for SyncVar, etc). If we add a highlight, we need
        // to handle all instances of the resolve error, so we'll use our
        // own, rather than using ResolveErrorType.INCORRECT_SIGNATURE
        [NotNull] public static readonly ResolveErrorType UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE = new UnityResolveErrorType("UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE");
    }
}