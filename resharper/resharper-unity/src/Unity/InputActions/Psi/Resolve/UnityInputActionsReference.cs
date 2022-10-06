using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Resolve
{
    public class UnityInputActionsReference : CheckedReferenceBase<IJsonNewLiteralExpression>, ICompletableReference,
        IReferenceFromStringLiteral
    {
        public UnityInputActionsReference([NotNull] IJsonNewLiteralExpression owner)
            : base(owner)
        {
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;

            var name = GetName();
            
            var container = myOwner.GetSolution().GetComponent<InputActionsElementContainer>();
            
            //container.GetUsagesFor()
            //
            //     if (elements.Count > 1)
            //     {
            //         return new ResolveResultWithInfo(new CandidatesResolveResult(elements.ResultingList()),
            //             ResolveErrorType.MULTIPLE_CANDIDATES);
            //     }
            //
            //     if (elements.Count == 1)
            //         return new ResolveResultWithInfo(new SimpleResolveResult(elements[0]), ResolveErrorType.OK);
            //

            return new ResolveResultWithInfo(EmptyResolveResult.Instance,
                InputActionsResolveErrorType.UNRESOLVED_REFERENCED_INPUTACTIONS_ERROR);
        }

        public override string GetName()
        {
            return myOwner.GetStringValue()?.Substring(2) ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var asmDefCache = myOwner.GetSolution().GetComponent<InputActionsCache>();
            var symbolTable = asmDefCache.GetSymbolTable();
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
            throw new System.NotImplementedException();
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