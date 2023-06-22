#nullable enable
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Psi.Resolve
{
    public abstract class ReferenceWithOrigin<TElement> : CheckedReferenceBase<TElement>, ICompletableReference where TElement : ITreeNode
    {
        private readonly IReferenceOrigin<TElement> myOrigin;

        protected ReferenceWithOrigin(TElement owner, IReferenceOrigin<TElement> origin) : base(owner)
        {
            myOrigin = origin;
        }

        private ResolveResultWithInfo ResolveFromSymbolTable() => CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true).Filter(GetSymbolFilters()));

        protected virtual ResolveResultWithInfo ResolveByName(string name) => new(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var resolveResultWithInfo = ResolveFromSymbolTable();
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;

            return ResolveByName(GetName());
        }

        public override string GetName() => myOrigin.GetReferenceName(myOwner) is { Length: > 0 } name ? name : SharedImplUtil.MISSING_DECLARATION_NAME;

        protected abstract ISymbolTable GetLookupSymbolTable();
        
        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var symbolTable = GetLookupSymbolTable();
            if (useReferenceName)
            {
                var name = GetName();
                return symbolTable.Filter(name, new ExactNameFilter(name));
            }
            return symbolTable;
        }

        public override TreeTextRange GetTreeTextRange() => myOrigin.GetReferenceNameRange(myOwner);

        public override IReference BindTo(IDeclaredElement element) => myOrigin.RenameFromReference(this, myOwner, element.ShortName, null);

        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution) => myOrigin.RenameFromReference(this, myOwner, element.ShortName, substitution);

        public override IAccessContext GetAccessContext() => new DefaultAccessContext(myOwner);

        public ISymbolTable GetCompletionSymbolTable() => GetReferenceSymbolTable(false).Filter(GetSymbolFilters());

        public override ISymbolFilter[] GetSymbolFilters() => EmptyArray<ISymbolFilter>.Instance;
    }
}