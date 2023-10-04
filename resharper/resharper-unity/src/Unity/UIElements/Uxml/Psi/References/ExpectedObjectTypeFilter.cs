using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
    public class ExpectedVisualElementTypeFilter : SimpleSymbolFilter
    {
        private readonly bool myMustBeClass;

        public ExpectedVisualElementTypeFilter(bool mustBeClass)
        {
            myMustBeClass = mustBeClass;
        }

        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            var typeElement = declaredElement as ITypeElement;
            if (typeElement == null)
                return !myMustBeClass;

            return typeElement.DerivesFrom(KnownTypes.VisualElement);
        }

        public override ResolveErrorType ErrorType => ResolveErrorType.NOT_RESOLVED;
    }
}