using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    public class UnityEventFunctionReference : CheckedReferenceBase<ILiteralExpression>, ICompletableReference, IReferenceFromStringLiteral
    {
        private readonly ITypeElement myTypeElement;
        private readonly ISymbolFilter myMethodFilter;

        public UnityEventFunctionReference(ITypeElement typeElement, ILiteralExpression literal)
            : base(literal)
        {
            myTypeElement = typeElement;

            myMethodFilter = new DeclaredElementTypeFilter(ResolveErrorType.NOT_RESOLVED, CLRDeclaredElementType.METHOD);
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;
            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);
        }

        public override string GetName()
        {
            return (string) myOwner.ConstantValue.Value ?? "???";
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var symbolTable = ResolveUtil.GetOwnMembersSymbolTable(myTypeElement, SymbolTableMode.FULL)
                .Filter(myMethodFilter);

            if (useReferenceName)
            {
                var name = GetName();
                return symbolTable.Filter(name, new ExactNameFilter(name));
            }
            return symbolTable;
        }

        public override TreeTextRange GetTreeTextRange()
        {
            var csharpLiteral = myOwner as ICSharpLiteralExpression;
            if (csharpLiteral != null)
            {
                var range = csharpLiteral.GetStringLiteralContentTreeRange();
                if (range.Length != 0)
                    return range;
            }

            return TreeTextRange.InvalidRange;
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            var literalAlterer = StringLiteralAltererUtil.CreateStringLiteralByExpression(myOwner);
            literalAlterer.Replace((string)myOwner.ConstantValue.Value, element.ShortName, myOwner.GetPsiModule());
            var newOwner = literalAlterer.Expression;
            if (!myOwner.Equals(newOwner))
                return newOwner.FindReference<UnityEventFunctionReference>() ?? this;
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

        public ISymbolTable GetCompletionSymbolTable()
        {
            return GetReferenceSymbolTable(false).Filter(myMethodFilter);
        }

        public override ISymbolFilter[] GetSymbolFilters()
        {
            return new[]
            {
                new ExactNameFilter(GetName()),
                myMethodFilter
            };
        }
    }
}