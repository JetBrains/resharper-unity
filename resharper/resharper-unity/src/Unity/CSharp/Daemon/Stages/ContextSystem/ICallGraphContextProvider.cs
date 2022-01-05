using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
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

        bool IsMarkedLocal([CanBeNull] IDeclaredElement declaredElement, [CanBeNull] CallGraphDataElement dataElement);
    }

    public static class CallGraphContextProviderEx
    {
        public static bool IsMarkedStage(
            [CanBeNull] this ICallGraphContextProvider contextProvider,
            [CanBeNull] IDeclaredElement declaredElement,
            [CanBeNull] IReadOnlyCallGraphContext context)
        {
            if (contextProvider == null || declaredElement == null || context == null)
                return false;

            switch (context.Kind)
            {
                case DaemonProcessKind.VISIBLE_DOCUMENT:
                    return contextProvider.IsMarkedLocal(declaredElement, context.DataElement);
                case DaemonProcessKind.GLOBAL_WARNINGS:
                    return contextProvider.IsMarkedGlobal(declaredElement);
                default:
                    return false;
            }
        }

        public static bool IsMarkedSweaDependent(
            [CanBeNull] this ICallGraphContextProvider contextProvider,
            [CanBeNull] IDeclaredElement declaredElement,
            [CanBeNull] SolutionAnalysisConfiguration configuration)
        {
            if (contextProvider == null || declaredElement == null || configuration == null)
                return false;

            return UnityCallGraphUtil.IsCallGraphReady(configuration)
                ? contextProvider.IsMarkedGlobal(declaredElement)
                : contextProvider.IsMarkedLocal(declaredElement);
        }

        public static bool IsMarkedSweaDependent(
        [CanBeNull] this ICallGraphContextProvider contextProvider,
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