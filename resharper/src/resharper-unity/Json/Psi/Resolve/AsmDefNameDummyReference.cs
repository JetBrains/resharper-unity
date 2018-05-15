using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve
{
    public class AsmDefNameDummyReference : TreeReferenceBase<IJavaScriptLiteralExpression>
    {
        public AsmDefNameDummyReference([NotNull] IJavaScriptLiteralExpression owner)
            : base(owner)
        {
        }

        public override ResolveResultWithInfo ResolveWithoutCache() => ResolveResultWithInfo.Ignore;
        public override string GetName() => myOwner.GetStringValue() ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName) => EmptySymbolTable.INSTANCE;
        public override TreeTextRange GetTreeTextRange() => myOwner.GetInnerTreeTextRange();
        public override IReference BindTo(IDeclaredElement element) => this;
        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution) => BindTo(element);
        public override IAccessContext GetAccessContext() => new DefaultAccessContext(myOwner);
    }
}