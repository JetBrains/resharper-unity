using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public interface ICallGraphContextProvider
    {
        CallGraphContextElement Context { get; }
        
        /// <summary>
        /// Settings based
        /// </summary>
        bool IsContextAvailable { get; }
        
        /// <returns>true if <see cref="Context"/> can be changed by this <paramref name="node"/></returns>
        bool IsContextChangingNode(ITreeNode node);

        #region Stage
        
        /// <summary>
        /// Use this overload from <see cref="IDaemonStageProcess"/>
        /// Provide corresponding <see cref="DaemonProcessKind"/>, and you will get correct results for current call graph state
        /// </summary>
        bool IsMarkedStage([CanBeNull] IDeclaration declaration, DaemonProcessKind processKind);
        
        /// <summary>
        /// Use this overload from <see cref="IDaemonStageProcess"/>
        /// Provide corresponding <see cref="DaemonProcessKind"/>, and you will get correct results for current call graph state
        /// </summary>
        bool IsMarkedStage([CanBeNull] ICSharpExpression expression, DaemonProcessKind processKind);
        
        /// <summary>
        /// Use this overload from <see cref="IDaemonStageProcess"/>
        /// Provide corresponding <see cref="DaemonProcessKind"/>, and you will get correct results for current call graph state
        /// </summary>
        bool IsMarkedStage([CanBeNull] IDeclaredElement declaredElement, DaemonProcessKind processKind);

        #endregion

        #region Sync
        
        /// <summary>
        /// Very dangerous api
        /// synchronously ask call graph, answer 100% correct, but may cause great performance loss
        /// does not depend on swea
        /// </summary>
        bool IsMarkedSync([CanBeNull] IDeclaration declaration);
        
        /// <summary>
        /// Very dangerous api
        /// synchronously ask call graph, answer 100% correct, but may cause great performance loss
        /// does not depend on swea
        /// </summary>
        bool IsMarkedSync([CanBeNull] ICSharpExpression expression);
        
        /// <summary>
        /// Very dangerous api
        /// synchronously ask call graph, answer 100% correct, but may cause great performance loss
        /// does not depend on swea
        /// </summary>
        bool IsMarkedSync([CanBeNull] IDeclaredElement declaredElement);

        #endregion

        #region Swea

        /// <summary>
        /// Access call graph based on swea state.
        /// </summary>
        bool IsMarkedSwea([CanBeNull] IDeclaration declaration);
        
        /// <summary>
        /// Access call graph based on swea state.
        /// </summary>
        bool IsMarkedSwea([CanBeNull] ICSharpExpression expression);
        
        /// <summary>
        /// Access call graph based on swea state.
        /// </summary>
        bool IsMarkedSwea([CanBeNull] IDeclaredElement declaredElement);

        #endregion
    }
    
    public static class CallGraphContextProviderEx
    {
        /// <summary>
        /// Intended to use from <see cref="IDaemonStageProcess"/>
        /// </summary>
        /// <returns>true if <see cref="IDeclaredElement"/> of <paramref name="treeNode"/> is marked</returns>
        public static bool IsMarked(
            [CanBeNull] this ICallGraphContextProvider provider, 
            [CanBeNull] ITreeNode treeNode,
            DaemonProcessKind processKind)
        {
            if (provider == null)
                return false;

            var mark = false;
            
            switch (treeNode)
            {
                case ICSharpExpression expression:
                    mark = provider.IsMarkedStage(expression, processKind);
                    break;
                case IDeclaration declaration:
                    mark = provider.IsMarkedStage(declaration, processKind);
                    break;
            }

            return mark;
        }

        /// <summary>
        /// Intended to use from stage process
        /// </summary>
        /// <returns>node context if marked, <see cref="CallGraphContextElement.NONE"/> if not</returns>
        public static CallGraphContextElement GetNodeContext(
            [CanBeNull] this ICallGraphContextProvider provider,
            [CanBeNull] ITreeNode treeNode,
            DaemonProcessKind processKind)
        {
            return provider.IsMarked(treeNode, processKind)
                ? provider.NotNull().Context
                : CallGraphContextElement.NONE;
        }
    }
}