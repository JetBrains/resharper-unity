using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class UnityEventTargetReference : CheckedReferenceBase<IPlainScalarNode>
    {
        private readonly FileID myFileId;

        public UnityEventTargetReference([NotNull] IPlainScalarNode owner, FileID fileId)
            : base(owner)
        {
            myFileId = fileId;
        }

        public string EventHandlerName => myOwner.GetText();

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            // If it's a reference to something other than MonoBehaviour, it shouldn't be a resolve error
            // TODO: Find an example when we encounter this
            if (!IsMonoBehaviourReference())
                return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.IGNORABLE);

            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;

            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var assetGuid = GetAssetGuid();
            var targetType = GetTypeFromAssetGuid(assetGuid);
            if (targetType == null)
                return EmptySymbolTable.INSTANCE;

            var symbolTable =
                ResolveUtil.GetSymbolTableByTypeElement(targetType, SymbolTableMode.FULL, myOwner.GetPsiModule());

            if (useReferenceName)
            {
                var name = GetName();
                return symbolTable.Filter(name, new ExactNameFilter(name));
            }

            return symbolTable;
        }

        public override string GetName() => myOwner.Text?.GetText() ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();
        public override IAccessContext GetAccessContext() => new DefaultAccessContext(myOwner);
        public override ISymbolFilter[] GetSymbolFilters() => EmptyArray<ISymbolFilter>.Instance;
        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution) => BindTo(element);

        public override IReference BindTo(IDeclaredElement element)
        {
            // TODO: Handle rename
            throw new NotImplementedException();
        }

        private bool IsMonoBehaviourReference()
        {
            var yamlFile = (IYamlFile) myOwner.GetContainingFile();
            var document = yamlFile.FindDocumentByAnchor(myFileId.fileID);
            return document.GetUnityObjectTypeFromRootNode() == "MonoBehaviour";
        }

        [CanBeNull]
        private ITypeElement GetTypeFromAssetGuid([CanBeNull] string assetGuid)
        {
            if (assetGuid == null)
                return null;

            var solution = myOwner.GetSolution();
            var cache = solution.GetComponent<MetaFileGuidCache>();
            var assetPaths = cache.GetAssetFilePathsFromGuid(assetGuid);
            if (assetPaths == null || assetPaths.IsEmpty())
                return null;

            // TODO: Multiple candidates!
            // I.e. someone has copy/pasted a .meta file
            if (assetPaths.Count != 1)
                return null;

            var projectItems = myOwner.GetSolution().FindProjectItemsByLocation(assetPaths[0]);
            var assetFile = projectItems.FirstOrDefault() as IProjectFile;
            if (!(assetFile?.GetPrimaryPsiFile() is ICSharpFile csharpFile))
                return null;

            var typeDeclaration =
                csharpFile.TypeDeclarationsEnumerable.FirstOrDefault(d => d.DeclaredName == assetPaths[0].NameWithoutExtension);
            return typeDeclaration?.DeclaredElement;
        }

        [CanBeNull]
        public string GetAssetGuid()
        {
            var yamlFile = (IYamlFile) myOwner.GetContainingFile();
            var document = yamlFile.FindDocumentByAnchor(myFileId.fileID);
            var fileID = document.GetUnityObjectPropertyValue("m_Script").AsFileID();
            return fileID?.guid;
        }
    }
}