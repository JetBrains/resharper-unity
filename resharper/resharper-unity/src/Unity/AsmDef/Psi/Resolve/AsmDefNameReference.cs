using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve
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
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true).Filter(GetSymbolFilters()));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;

            var name = GetName();
            if (AsmDefUtils.TryParseGuidReference(name, out var guid))
            {
                var metaFileCache = myOwner.GetSolution().GetComponent<MetaFileGuidCache>();
                var asmDefCache = myOwner.GetSolution().GetComponent<AsmDefCache>();

                // We should only get a single asset, but beware of copy/paste files
                var elements = new FrugalLocalList<IDeclaredElement>();
                foreach (var path in metaFileCache.GetAssetFilePathsFromGuid(guid))
                {
                    var nameElement = asmDefCache.GetNameDeclaredElement(path);
                    if (nameElement != null)
                        elements.Add(nameElement);
                }

                if (elements.Count > 1)
                {
                    return new ResolveResultWithInfo(new CandidatesResolveResult(elements.ResultingList()),
                        ResolveErrorType.MULTIPLE_CANDIDATES);
                }

                if (elements.Count == 1)
                    return new ResolveResultWithInfo(new SimpleResolveResult(elements[0]), ResolveErrorType.OK);
            }

            return new ResolveResultWithInfo(EmptyResolveResult.Instance,
                AsmDefResolveErrorType.UNRESOLVED_REFERENCED_ASMDEF_ERROR);
        }

        public override string GetName()
        {
            return myOwner.GetStringValue() ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var asmDefCache = myOwner.GetSolution().GetComponent<AsmDefCache>();
            var symbolTable = asmDefCache.GetAssemblyNamesSymbolTable();
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
            // Don't rename a GUID: reference
            if (AsmDefUtils.IsGuidReference(myOwner.GetUnquotedText()))
                return this;

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