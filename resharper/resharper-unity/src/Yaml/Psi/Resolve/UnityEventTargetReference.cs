using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
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
        private readonly string myFileId;

        public UnityEventTargetReference([NotNull] IPlainScalarNode owner, string fileId)
            : base(owner)
        {
            myFileId = fileId;
        }

        public string EventHandlerName => myOwner.GetText();

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            if (!IsMonoBehaviourReference())
                return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.IGNORABLE);

            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;
            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var targetType = GetTypeFromFileId();
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
            var document = FindDocumentByAnchor(myFileId);

            // FileID points to nothing. Missing reference, treat as a MonoBehaviour reference
            if (document == null)
                return true;

            return GetTargetMonoBehaviourNode(document) != null;
        }

        [CanBeNull]
        private ITypeElement GetTypeFromFileId()
        {
            var assetGuid = GetAssetGuid();
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
            var document = FindDocumentByAnchor(myFileId);
            if (document == null)
                return null;

            // If it's a reference to something other than MonoBehaviour, it shouldn't be a resolve error
            var fileReference = GetScriptFileReference(document);
            return fileReference == null ? null : GetFileReferenceGuid(fileReference);
        }

        private IYamlDocument FindDocumentByAnchor(string anchor)
        {
            var yamlFile = (IYamlFile) myOwner.GetContainingFile();
            if (yamlFile == null)
                return null;

            foreach (var document in yamlFile.DocumentsEnumerable)
            {
                var properties = GetBlockNodeProperties(document.BlockNode);
                var text = properties?.AnchorProperty?.Text?.GetText() ?? string.Empty;
                if (text == anchor)
                    return document;
            }

            return null;
        }

        [CanBeNull]
        private INodeProperties GetBlockNodeProperties(INode documentBlockNode)
        {
            if (documentBlockNode is IBlockSequenceNode sequenceNode)
                return sequenceNode.Properties;
            if (documentBlockNode is IBlockMappingNode mappingNode)
                return mappingNode.Properties;
            return null;
        }

        [CanBeNull]
        private IFlowMappingNode GetScriptFileReference(IYamlDocument document)
        {
            var monoBehaviourNode = GetTargetMonoBehaviourNode(document);
            if (monoBehaviourNode == null)
                return null;

            if (!(monoBehaviourNode.Value is IBlockMappingNode blockMappingNode))
                return null;

            return blockMappingNode.FindChildBySimpleKey("m_Script")?.Value as IFlowMappingNode;
        }

        [CanBeNull]
        private IBlockMappingEntry GetTargetMonoBehaviourNode(IYamlDocument document)
        {
            if (!(document.BlockNode is IBlockMappingNode monoBehaviourBlockMappingNode))
                return null;

            var firstEntry = monoBehaviourBlockMappingNode.EntriesEnumerable.FirstOrDefault();
            if (firstEntry == null)
                return null;

            return firstEntry.Key.GetPlainScalarText() == "MonoBehaviour" ? firstEntry : null;
        }

        [CanBeNull]
        private string GetFileReferenceGuid(IFlowMappingNode fileReference)
        {
            foreach (var entry in fileReference.EntriesEnumerable)
            {
                if (entry.Key.GetPlainScalarText() == "guid")
                    return entry.Value.GetPlainScalarText();
            }

            return null;
        }
    }
}