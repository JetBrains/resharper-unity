using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Resolve
{
    public class InputActionsNameReference : CheckedReferenceBase<IJsonNewLiteralExpression>, ICompletableReference,
        IReferenceFromStringLiteral
    {
        public InputActionsNameReference([NotNull] IJsonNewLiteralExpression owner)
            : base(owner)
        {
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;

            var name = GetName();

            return new ResolveResultWithInfo(EmptyResolveResult.Instance,
                InputActionsResolveErrorType.UNRESOLVED_REFERENCED_INPUTACTIONS_ERROR);
        }

        public override string GetName()
        {
            return myOwner.GetStringValue() ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var inputActionsCache = myOwner.GetSolution().GetComponent<InputActionsCache>();
            var symbolTable = inputActionsCache.GetSymbolTable();
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
                return newExpression.FindReference<InputActionsNameReference>() ?? this;
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