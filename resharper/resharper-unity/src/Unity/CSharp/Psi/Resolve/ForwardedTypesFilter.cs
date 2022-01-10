using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public class ForwardedTypesFilter : SimpleSymbolFilter
    {
        private readonly ISymbolCache mySymbolCache;

        public ForwardedTypesFilter(ISymbolCache symbolCache)
        {
            mySymbolCache = symbolCache;
        }

        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            // Accept anything, unless it's a class, in which case, make sure it's not been forwarded elsewhere
            var typeElement = declaredElement as ITypeElement;
            if (typeElement == null)
                return true;

            return !mySymbolCache.GetForwardedToAssemblies(typeElement.Module, typeElement.GetClrName()).Any();
        }

        public override ResolveErrorType ErrorType => ResolveErrorType.IGNORABLE;
    }
}