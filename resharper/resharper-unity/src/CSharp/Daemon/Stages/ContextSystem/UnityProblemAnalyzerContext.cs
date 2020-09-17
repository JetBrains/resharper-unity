using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public sealed class UnityProblemAnalyzerContext
    {
        public readonly UnityProblemAnalyzerContextElement Context;
        public readonly UnityProblemAnalyzerContext PreviousContext;
        public readonly ITreeNode ContextTreeNode;

        private UnityProblemAnalyzerContext(UnityProblemAnalyzerContextElement context,
            UnityProblemAnalyzerContext previousContext, ITreeNode node)
        {
            Context = context;
            PreviousContext = previousContext;
            ContextTreeNode = node;
        }
        public UnityProblemAnalyzerContext Chain(UnityProblemAnalyzerContextElement context, ITreeNode node)
        {
            return new UnityProblemAnalyzerContext(context, this, node);
        }

        public UnityProblemAnalyzerContext Rollback([NotNull] ITreeNode node)
        {
            Assertion.AssertNotNull(node, "node != null");
            return node == ContextTreeNode ? PreviousContext : this;
        }

        public bool IsSuperSetOf(UnityProblemAnalyzerContextElement subContext)
        {
            // this byte trick check if subContext is subset of myContext
            return ((subContext & Context) ^ subContext) == UnityProblemAnalyzerContextElement.NONE;
        }
        
        public bool ContainAny(UnityProblemAnalyzerContextElement prohibitedContext)
        {
            return (Context & prohibitedContext) != UnityProblemAnalyzerContextElement.NONE;
        }

        public static UnityProblemAnalyzerContext EMPTY_INSTANCE = new UnityProblemAnalyzerContext(UnityProblemAnalyzerContextElement.NONE, null, null);
    }
}