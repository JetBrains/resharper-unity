using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public abstract class CallGraphContextProviderBase : ICallGraphContextProvider
    {
        protected readonly IElementIdProvider myElementIdProvider;
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtensionProvider;
        private readonly CallGraphRootMarksProviderBase myMarksProviderBase;

        protected CallGraphContextProviderBase(
            IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            CallGraphRootMarksProviderBase marksProviderBase)
        {
            myElementIdProvider = elementIdProvider;
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            myMarksProviderBase = marksProviderBase;
        }

        public abstract CallGraphContextTag ContextTag { get; }
        public CallGraphRootMarkId MarkId => myMarksProviderBase.Id;
        public abstract bool IsContextAvailable { get; }
        public virtual bool IsContextChangingNode(ITreeNode node) => UnityCallGraphUtil.IsFunctionNode(node);

        public virtual bool IsMarkedGlobal(IDeclaredElement declaredElement)
        {
            return IsMarkedInternal(declaredElement, shouldPropagate: true);
        }

        public virtual bool IsMarkedLocal(IDeclaredElement declaredElement)
        {
            return IsMarkedInternal(declaredElement, shouldPropagate: false);
        }

        public virtual bool IsMarkedLocal(IDeclaredElement declaredElement, CallGraphDataElement dataElement)
        {
            if (IsContextAvailable == false)
                return false;

            if (declaredElement == null || dataElement == null)
                return false;

            var vertex = myElementIdProvider.GetElementId(declaredElement);

            if (vertex == null)
                return false;

            if (!dataElement.Vertices.Contains(vertex.Value) || dataElement.BanMarks.GetOrEmpty(MarkId).Contains(vertex.Value))
                return false;
                
            if (dataElement.RootMarks.GetOrEmpty(MarkId).Contains(vertex.Value))
                return true;

            return IsMarkedInternal(declaredElement, shouldPropagate:false, vertex);
        }

        protected bool IsMarkedInternal([CanBeNull] IDeclaredElement declaredElement, bool shouldPropagate, ElementId? knownId = null)
        {
            if (IsContextAvailable == false)
                return false;

            if (declaredElement == null)
                return false;

            var elementId = knownId ?? myElementIdProvider.GetElementId(declaredElement);

            if (!elementId.HasValue)
                return false;

            var elementIdValue = elementId.Value;
            var markId = myMarksProviderBase.Id;

            return shouldPropagate
                ? myCallGraphSwaExtensionProvider.IsMarkedGlobal(markId, elementIdValue)
                : myCallGraphSwaExtensionProvider.IsMarkedLocal(markId, elementIdValue);
        }
    }
}