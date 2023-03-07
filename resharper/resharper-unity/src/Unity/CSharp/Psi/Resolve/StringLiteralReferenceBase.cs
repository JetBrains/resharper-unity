#nullable enable

using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public abstract class StringLiteralReferenceBase : CheckedReferenceBase<ILiteralExpression>,
        IUnityReferenceFromStringLiteral
    {
        protected StringLiteralReferenceBase(ILiteralExpression owner)
            : base(owner)
        {
            Assertion.Assert(owner.ConstantValue.IsString());
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            // This will take the candidates from the reference symbol table and apply the filters from GetSymbolFilters
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            return !resolveResultWithInfo.Result.IsEmpty ? resolveResultWithInfo : ResolveResultWithInfo.Unresolved;
        }

        public override string GetName() => myOwner.ConstantValue.IsNotNullString(out var name)
            ? name
            : SharedImplUtil.MISSING_DECLARATION_NAME;

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
            var stringValue = myOwner.ConstantValue.StringValue.NotNull();
            literalAlterer.Replace(stringValue, element.ShortName);
            var newOwner = literalAlterer.Expression;
            if (!myOwner.Equals(newOwner))
                return newOwner.FindReference<StringLiteralReferenceBase>(r => r.GetType() == GetType()) ?? this;
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
