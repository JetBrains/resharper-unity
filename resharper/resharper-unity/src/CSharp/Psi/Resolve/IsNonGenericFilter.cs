using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public class IsNonGenericFilter : SimpleSymbolFilter
    {
        public static IsNonGenericFilter INSTANCE = new IsNonGenericFilter();

        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            if (!(declaredElement is ITypeElement typeElement))
                return true;
            return typeElement.TypeParameters.Count == 0;
        }

        public override ResolveErrorType ErrorType => ResolveErrorType.INCORRECT_TYPE_PARAMETER_NUMBER;
    }
}