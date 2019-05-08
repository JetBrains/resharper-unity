using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class UnityEventTargetReference : CheckedReferenceBase<IPlainScalarNode>, IUnityYamlReference
    {
        private readonly FileID myFileId;

        public UnityEventTargetReference([NotNull] IPlainScalarNode owner, FileID fileId)
            : base(owner)
        {
            myFileId = fileId;
        }

        public string EventHandlerName => myOwner.GetText();

        // The YAML document for the component that contains the reference - usually, but not necessarily, a MonoBehaviour
        public IYamlDocument ComponentDocument => myOwner.GetContainingNode<IYamlDocument>();

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            // If it's a reference to something other than MonoBehaviour, it shouldn't be a resolve error
            // E.g. setting the property on a light map when an event fires
            if (!IsMonoBehaviourReference())
                return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.IGNORABLE);

            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;

            // TODO: Support references to scripts/event handlers in external packages
            // Surprisingly, it's possible  to have a reference to a script asset defined in a read-only package. We
            // don't know anything about these assets, because read-only packages are not part of the C# project
            // structure - they are compiled and added as assembly references. So we don't currently have a way to map
            // an asset GUID back to a compiled class.
            // See also UnityEventTargetReference
//            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);
            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.IGNORABLE);
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var assetGuid = GetScriptAssetGuid();
            var targetType = UnityObjectPsiUtil.GetTypeElementFromScriptAssetGuid(myOwner.GetSolution(), assetGuid);
            if (targetType == null)
                return EmptySymbolTable.INSTANCE;

            var symbolTable =
                ResolveUtil.GetSymbolTableByTypeElement(targetType, SymbolTableMode.FULL, myOwner.GetPsiModule());

            if (useReferenceName)
            {
                var name = GetName();
                return symbolTable.Filter(name, IsMethodFilter.INSTANCE, OverriddenFilter.INSTANCE, new ExactNameFilter(name),
                    new StaticFilter(new NonStaticAccessContext(myOwner)));
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
            using (WriteLockCookie.Create(myOwner.IsPhysical()))
            {
                var text = YamlTokenType.NS_PLAIN_ONE_LINE.Create(element.ShortName);
                if (myOwner.Text != null)
                    LowLevelModificationUtil.ReplaceChildRange(myOwner.Text, myOwner.Text, text);
                else
                    LowLevelModificationUtil.AddChild(myOwner.Text, text);
            }

            return this;
        }

        private bool IsMonoBehaviourReference()
        {
            var yamlFile = (IYamlFile) myOwner.GetContainingFile();
            var document = yamlFile.FindDocumentByAnchor(myFileId.fileID);
            return document.GetUnityObjectTypeFromRootNode() == "MonoBehaviour";
        }

        // Note that this is the guid of the script asset of the instance that the method should be called on. It is not
        // necessarily the guid of the script asset that *declares* the method (property setter is a method). The method
        // might be declared on a base type, or might be a virtual override
        [CanBeNull]
        public string GetScriptAssetGuid()
        {
            var yamlFile = (IYamlFile) myOwner.GetContainingFile();
            var document = yamlFile.FindDocumentByAnchor(myFileId.fileID);
            var fileID = document.GetUnityObjectPropertyValue("m_Script").AsFileID();
            return fileID?.guid;
        }
    }
}