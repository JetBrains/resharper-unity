using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public class UnityResolveErrorType : ResolveErrorType
    {
        private UnityResolveErrorType([NotNull] string name)
            : base(name)
        {
        }

        [NotNull] public static readonly ResolveErrorType UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_ERROR =
            new UnityResolveErrorType("UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_ERROR");

        [NotNull] public static readonly ResolveErrorType UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_WARNING =
            new UnityResolveErrorType("UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_WARNING");

        [NotNull] public static readonly ResolveErrorType EXPECTED_MONO_BEHAVIOUR =
            new UnityResolveErrorType("EXPECTED_MONO_BEHAVIOUR");

        [NotNull] public static readonly ResolveErrorType EXPECTED_COMPONENT =
            new UnityResolveErrorType("EXPECTED_COMPONENT");

        [NotNull] public static readonly ResolveErrorType EXPECTED_SCRIPTABLE_OBJECT =
            new UnityResolveErrorType("EXPECTED_SCRIPTABLE_OBJECT");
    }
}