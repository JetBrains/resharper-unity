using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public abstract class CallGraphContextProviderBase : ICallGraphContextProvider
    {
        private readonly IElementIdProvider myElementIdProvider;
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtensionProvider;
        private readonly CallGraphRootMarksProviderBase myMarksProviderBase;

        protected CallGraphContextProviderBase(IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            CallGraphRootMarksProviderBase marksProviderBase)
        {
            myElementIdProvider = elementIdProvider;
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            myMarksProviderBase = marksProviderBase;
        }

        public abstract CallGraphContextElement Context { get; }
        public abstract bool IsContextAvailable { get; }
        
        public virtual bool HasContext(IDeclaration declaration, DaemonProcessKind processKind)
        {
            return IsMarked(declaration?.DeclaredElement, processKind);
        }

        public virtual bool IsCalleeMarked(ICSharpExpression expression, DaemonProcessKind processKind)
        {
            return IsMarked(CallGraphUtil.GetCallee(expression), processKind);
        }

        public virtual bool IsMarked(IDeclaredElement declaredElement, DaemonProcessKind processKind)
        {
            if (IsContextAvailable == false)
                return false;
            
            var isGlobalStage = processKind == DaemonProcessKind.GLOBAL_WARNINGS;

            if (!isGlobalStage)
                return false;

            var id = myElementIdProvider.GetElementId(declaredElement);

            if (!id.HasValue)
                return false;

            return myCallGraphSwaExtensionProvider.IsMarkedByCallGraphRootMarksProvider(
                myMarksProviderBase.Id, isGlobalStage: true, id.Value);
        }

        public virtual bool IsContextChangingNode([NotNull] ITreeNode node) => UnityCallGraphUtil.IsFunctionNode(node);
    }
}