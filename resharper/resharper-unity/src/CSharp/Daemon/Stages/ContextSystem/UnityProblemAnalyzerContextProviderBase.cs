using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public abstract class UnityProblemAnalyzerContextProviderBase : IUnityProblemAnalyzerContextProvider
    {
        private readonly IElementIdProvider myElementIdProvider;
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtensionProvider;
        private readonly CallGraphRootMarksProviderBase myMarksProviderBase;

        protected UnityProblemAnalyzerContextProviderBase(IElementIdProvider elementIdProvider, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            CallGraphRootMarksProviderBase marksProviderBase)
        {
            myElementIdProvider = elementIdProvider;
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            myMarksProviderBase = marksProviderBase;
        }
        public abstract UnityProblemAnalyzerContextElement Context { get; }

        public UnityProblemAnalyzerContextElement CheckContext(ITreeNode node, DaemonProcessKind processKind)
        {
            var declaration = node as ICSharpDeclaration;
            
            if (declaration == null)
                return UnityProblemAnalyzerContextElement.NONE;

            var declaredElement = declaration.DeclaredElement;

            if (declaredElement == null)
                return UnityProblemAnalyzerContextElement.NONE;

            if (IsProhibitedFast(declaration))
                return UnityProblemAnalyzerContextElement.NONE;

            var isRooted = IsRootFast(declaration);
            var isGlobalStage = processKind == DaemonProcessKind.GLOBAL_WARNINGS;

            if (!isRooted && isGlobalStage)
            {
                var id = myElementIdProvider.GetElementId(declaredElement);

                if (!id.HasValue)
                    return UnityProblemAnalyzerContextElement.NONE;

                isRooted = myCallGraphSwaExtensionProvider.IsMarkedByCallGraphRootMarksProvider(
                    myMarksProviderBase.Id, isGlobalStage: true, id.Value);
            }

            return isRooted ? Context : UnityProblemAnalyzerContextElement.NONE;
        }

        /// <summary>
        /// Make this method as fast as possible.
        /// It is used at local stage to check if declaration is root mark.
        /// </summary>
        /// <param name="declaration"></param>
        /// <returns></returns>
        protected abstract bool IsRootFast(ICSharpDeclaration declaration);
        
        /// <summary>
        /// Make this method as fast as possible.
        /// It is used at local stage to check if declaration should not be analyzed at all.
        /// </summary>
        /// <param name="declaration"></param>
        /// <returns></returns>
        protected abstract bool IsProhibitedFast(ICSharpDeclaration declaration);
    }
}