using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    public interface IUnityMessageReference
    {
    }

    public class MonoBehaviourInvokeReference : CheckedReferenceBase<ILiteralExpression>, ICompletableReference, IReferenceFromStringLiteral, IUnityMessageReference
    {
        private readonly ITypeElement myTypeElement;
        private readonly ISymbolFilter myMethodFilter;

        public MonoBehaviourInvokeReference(ITypeElement typeElement, ILiteralExpression literal)
            : base(literal)
        {
            myTypeElement = typeElement;

#if WAVE07
            myMethodFilter = new DeclaredElementTypeFilter(ResolveErrorType.NOT_RESOLVED, CLRDeclaredElementType.METHOD);
#else
            myMethodFilter = new DeclaredElementTypeFilter(ResolveErrorType.NOT_RESOLVED_IN_STRING_LITERAL, CLRDeclaredElementType.METHOD);
#endif
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;
#if WAVE07
            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);

#else
            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED_IN_STRING_LITERAL);
#endif
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
                return newOwner.FindReference<MonoBehaviourInvokeReference>() ?? this;
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

        public IEnumerable<DeclaredElementType> ExpecteDeclaredElementTypes
        {
            get { yield return CLRDeclaredElementType.METHOD; }
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