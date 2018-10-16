using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public abstract class StringLiteralReferenceBase : CheckedReferenceBase<ILiteralExpression>,
        IUnityReferenceFromStringLiteral
    {
        protected StringLiteralReferenceBase([NotNull] ILiteralExpression owner)
            : base(owner)
        {
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            // This will take the candidates from the reference symbol table and apply the filters from GetSymbolFilters
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;
            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);
        }

        public override string GetName()
        {
            return myOwner.ConstantValue.Value as string ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        }

        public override TreeTextRange GetTreeTextRange()
        {
            if (myOwner is ICSharpLiteralExpression literal)
            {
                var range = literal.GetStringLiteralContentTreeRange();
                if (range.Length != 0)
                    return range;
            }

            return TreeTextRange.InvalidRange;
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            var literalAlterer = StringLiteralAltererUtil.CreateStringLiteralByExpression(myOwner);
            var constantValue = (string)myOwner.ConstantValue.Value;
            Assertion.AssertNotNull(constantValue, "constantValue != null");
            literalAlterer.Replace(constantValue, element.ShortName);
            var newOwner = literalAlterer.Expression;
            if (!myOwner.Equals(newOwner))
                return newOwner.FindReference<SyncVarHookReference>() ?? this;
            return this;
        }

        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
        {
            return BindTo(element);
        }

        public override IAccessContext GetAccessContext()
        {
            return new DefaultAccessContext(myOwner);
        }
    }
}