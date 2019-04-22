using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class MonoScriptReference : CheckedReferenceBase<IPlainScalarNode>, IUnityYamlReference
    {
        private readonly FileID myFileID;
        private readonly MetaFileGuidCache myMetaFileGuidCache;

        public MonoScriptReference(IPlainScalarNode owner, FileID fileID, MetaFileGuidCache metaFileGuidCache)
            : base(owner)
        {
            myFileID = fileID;
            myMetaFileGuidCache = metaFileGuidCache;
        }

        // The YAML document that contains the reference. This should always be a MonoBehaviour component
        public IYamlDocument ComponentDocument => myOwner.GetContainingNode<IYamlDocument>();

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;

            // TODO: Support references to scripts/event handlers in external packages
            // Surprisingly, it's possible to have a reference to a script asset defined in a read-only package. We
            // don't know anything about these assets, because read-only packages are not part of the C# project
            // structure - they are compiled and added as assembly references. So we don't currently have a way to map
            // an asset GUID back to a compiled class.
            // See also UnityEventTargetReference
//            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);
            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.IGNORABLE);
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var assetGuid = myFileID.guid;
            var targetType = UnityObjectPsiUtil.GetTypeElementFromScriptAssetGuid(myOwner.GetSolution(), assetGuid);
            if (targetType == null)
                return EmptySymbolTable.INSTANCE;

            // We don't need to do anything about useReferenceName, we only ever have one candidate

            var symbolTable = new SymbolTable(myOwner.GetPsiServices());
            symbolTable.AddSymbol(assetGuid, targetType);
            return symbolTable;
        }

        public override ISymbolFilter[] GetSymbolFilters() => EmptyArray<ISymbolFilter>.Instance;
        public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();
        public override IAccessContext GetAccessContext() => new DefaultAccessContext(myOwner);

        // Note that the guid is the (primary) reference name
        public override string GetName() => myOwner.Text?.GetText() ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        public override bool HasMultipleNames => true;

        public override HybridCollection<string> GetAllNames()
        {
            var assetNames = myMetaFileGuidCache.GetAssetNames(myFileID.guid);
            switch (assetNames.Count)
            {
                case 0:
                    return new HybridCollection<string>(myFileID.guid);
                case 1:
                    return new HybridCollection<string>(myFileID.guid, assetNames[0]);
                default:
                    return new HybridCollection<string>(myFileID.guid).Add(assetNames);
            }
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            // We don't need to update any source, as a rename doesn't change the guid that we have. But we do need to
            // invalidate our cached result, or a rename will fail because we'll still be pointing at an old element
            CurrentResolveResult = null;
            return this;
        }

        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution) => BindTo(element);
    }
}