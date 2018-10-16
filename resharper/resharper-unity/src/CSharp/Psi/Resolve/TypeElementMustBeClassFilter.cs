using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public class TypeElementMustBeClassFilter : SimpleSymbolFilter
    {
        public static TypeElementMustBeClassFilter INSTANCE = new TypeElementMustBeClassFilter();

        public override ResolveErrorType ErrorType => ResolveErrorType.IGNORABLE;

        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            if (declaredElement is ITypeElement)
                return declaredElement is IClass;
            return true;
        }
    }
}