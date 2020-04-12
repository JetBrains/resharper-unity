using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.Resolve
{
    public class AsmDefNameReference : CheckedReferenceBase<IJsonNewLiteralExpression>, ICompletableReference,
        IReferenceFromStringLiteral
    {
        public AsmDefNameReference([NotNull] IJsonNewLiteralExpression owner)
            : base(owner)
        {
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;

            return new ResolveResultWithInfo(EmptyResolveResult.Instance,
                AsmDefResolveErrorType.ASMDEF_UNRESOLVED_REFERENCED_PROJECT_ERROR);
        }

        public override string GetName()
        {
            return myOwner.GetStringValue() ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var asmDefNameCache = myOwner.GetSolution().GetComponent<AsmDefNameCache>();
            var symbolTable = asmDefNameCache.GetSymbolTable();
            if (useReferenceName)
            {
                var name = GetName();
                return symbolTable.Filter(name, new ExactNameFilter(name));
            }
            return symbolTable;
        }

        public override TreeTextRange GetTreeTextRange()
        {
            return myOwner.GetInnerTreeTextRange();
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            var factory = JsonNewElementFactory.GetInstance(myOwner.GetPsiModule());
            var literalExpression = factory.CreateStringLiteral(element.ShortName);

            using (WriteLockCookie.Create(myOwner.IsPhysical()))
            {
                var newExpression = ModificationUtil.ReplaceChild(myOwner, literalExpression);
                return newExpression.FindReference<AsmDefNameReference>() ?? this;
            }
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
            return GetReferenceSymbolTable(false).Filter(GetSymbolFilters());
        }

        public override ISymbolFilter[] GetSymbolFilters()
        {
            return EmptyArray<ISymbolFilter>.Instance;
        }
    }
}