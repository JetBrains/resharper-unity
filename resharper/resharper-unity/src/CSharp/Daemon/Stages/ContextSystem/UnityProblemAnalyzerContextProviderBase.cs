using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public abstract class UnityProblemAnalyzerContextProviderBase : IUnityProblemAnalyzerContextProvider
    {
        private readonly IElementIdProvider myElementIdProvider;
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtensionProvider;
        private readonly CallGraphRootMarksProviderBase myMarksProviderBase;

        protected UnityProblemAnalyzerContextProviderBase(IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            CallGraphRootMarksProviderBase marksProviderBase)
        {
            myElementIdProvider = elementIdProvider;
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            myMarksProviderBase = marksProviderBase;
        }

        public abstract UnityProblemAnalyzerContextElement Context { get; }

        public UnityProblemAnalyzerContextElement GetContext(ITreeNode node, DaemonProcessKind processKind, bool getCallee)
        {
            if (node == null)
                return UnityProblemAnalyzerContextElement.NONE;

            if (IsContextProhibitedFast(node))
                return UnityProblemAnalyzerContextElement.NONE;

            if (HasContextFast(node))
                return Context;

            IDeclaredElement declaredElement = null;

            if (node is IDeclaration declaration)
                declaredElement = declaration.DeclaredElement;

            if (getCallee && node is ICSharpExpression icSharpExpression)
                declaredElement = CallGraphUtil.GetCallee(icSharpExpression);

            return IsMarkedInternal(declaredElement, processKind)
                ? Context
                : UnityProblemAnalyzerContextElement.NONE;
        }

        public bool IsMarked(IDeclaredElement declaredElement,
            DaemonProcessKind processKind)
        {
            if (declaredElement == null)
                return false;

            if (IsMarkedFast(declaredElement))
                return true;

            if (IsBannedFast(declaredElement))
                return false;
            
            return IsMarkedInternal(declaredElement, processKind);
        }

        private bool IsMarkedInternal(IDeclaredElement declaredElement, DaemonProcessKind processKind)
        {
            var isGlobalStage = processKind == DaemonProcessKind.GLOBAL_WARNINGS;

            if (!isGlobalStage)
                return false;

            var id = myElementIdProvider.GetElementId(declaredElement);

            if (!id.HasValue)
                return false;

            return myCallGraphSwaExtensionProvider.IsMarkedByCallGraphRootMarksProvider(
                myMarksProviderBase.Id, isGlobalStage: true, id.Value);
        }

        public virtual bool IsProblemContextBound => true;

        protected abstract bool HasContextFast([NotNull] ITreeNode treeNode);

        protected abstract bool IsMarkedFast([NotNull] IDeclaredElement declaredElement);

        protected abstract bool IsBannedFast([NotNull] IDeclaredElement declaredElement);

        protected abstract bool IsContextProhibitedFast([NotNull] ITreeNode treeNode);
    }
}