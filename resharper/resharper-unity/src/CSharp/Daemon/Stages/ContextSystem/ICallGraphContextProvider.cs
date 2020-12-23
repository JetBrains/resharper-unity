using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public interface ICallGraphContextProvider
    {
        CallGraphContextTag ContextTag { get; }
        
        /// <summary>
        /// corresponding marks provider id
        /// </summary>
        CallGraphRootMarkId MarkId { get; }
        
        /// <summary>
        /// Settings based
        /// </summary>
        bool IsContextAvailable { get; }
        
        /// <returns>true if <see cref="ContextTag"/> can be changed by this <paramref name="node"/></returns>
        bool IsContextChangingNode([CanBeNull] ITreeNode node);

        bool IsMarkedGlobal([CanBeNull] IDeclaredElement declaredElement);

        bool IsMarkedLocal([CanBeNull] IDeclaredElement declaredElement);
    }
    
    public static class CallGraphContextProviderEx
    {
        public static bool IsMarkedStage([CanBeNull] this ICallGraphContextProvider contextProvider, 
            [CanBeNull] IDeclaredElement declaredElement,
            [CanBeNull] IReadOnlyContext context)
        {
            if (contextProvider == null || declaredElement == null || context == null)
                return false;
            
            if (context.Kind == DaemonProcessKind.VISIBLE_DOCUMENT)
                return contextProvider.IsMarkedLocal(declaredElement, context.DataElement);
            else if (context.Kind == DaemonProcessKind.GLOBAL_WARNINGS)
                return contextProvider.IsMarkedGlobal(declaredElement);

            return false;
        }
        
        public static bool IsMarkedLocal([CanBeNull] this ICallGraphContextProvider contextProvider, 
                                         [CanBeNull] IDeclaredElement declaredElement,
                                         [CanBeNull] CallGraphDataElement dataElement)
        {
            if (contextProvider == null || declaredElement == null || dataElement == null)
                return false;
            
            // CGTD 
            

            return contextProvider.IsMarkedLocal(declaredElement);
        }

        public static bool IsMarkedSweaDependent([CanBeNull] this ICallGraphContextProvider contextProvider,
                                                 [CanBeNull] IDeclaredElement declaredElement,
                                                 [CanBeNull] SolutionAnalysisConfiguration configuration)
        {
            if (contextProvider == null || declaredElement == null || configuration == null)
                return false;

            if (configuration.CompletedOnceAfterStart.Value && configuration.Loaded.Value)
                return contextProvider.IsMarkedGlobal(declaredElement);

            return contextProvider.IsMarkedLocal(declaredElement);
        }

        public static bool IsMarkedSweaDependent([CanBeNull] this ICallGraphContextProvider contextProvider,
                                                 [CanBeNull] IDeclaredElement declaredElement,
                                                 [CanBeNull] SolutionAnalysisService swea)
        {
            return IsMarkedSweaDependent(contextProvider, declaredElement, swea?.Configuration);
        }

        [CanBeNull]
        public static IDeclaredElement ExtractDeclaredElementForProvider([CanBeNull] ITreeNode node)
        {
            switch (node)
            {
                case IDeclaration declaration:
                    return declaration.DeclaredElement;
                case ICSharpExpression expression:
                    return CallGraphUtil.GetCallee(expression);
            }

            return null;
        }
    }
}