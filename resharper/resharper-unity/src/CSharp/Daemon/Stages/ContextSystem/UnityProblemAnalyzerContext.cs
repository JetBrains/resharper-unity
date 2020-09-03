using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public sealed class UnityProblemAnalyzerContext
    {
        private readonly UnityProblemAnalyzerContextElement myContext;
        private readonly UnityProblemAnalyzerContext myPreviousContext;
        private readonly ITreeNode myTreeNode;

        private UnityProblemAnalyzerContext(UnityProblemAnalyzerContextElement context,
            UnityProblemAnalyzerContext previousContext, ITreeNode node)
        {
            myContext = context;
            myPreviousContext = previousContext;
            myTreeNode = node;
        }
        public UnityProblemAnalyzerContext Chain(UnityProblemAnalyzerContextElement context, ITreeNode node)
        {
            return new UnityProblemAnalyzerContext(context, this, node);
        }

        public UnityProblemAnalyzerContext Rollback([NotNull] ITreeNode node)
        {
            Assertion.AssertNotNull(node, "node != null");
            return node == myTreeNode ? myPreviousContext : this;
        }

        public bool IsSuperSetOf(UnityProblemAnalyzerContextElement subContext)
        {
            // this byte trick check if subContext is subset of myContext
            return ((subContext & myContext) ^ subContext) == UnityProblemAnalyzerContextElement.NONE;
        }
        
        public bool ContainAny(UnityProblemAnalyzerContextElement prohibitedContext)
        {
            return (myContext & prohibitedContext) != UnityProblemAnalyzerContextElement.NONE;
        }

        public static UnityProblemAnalyzerContext EMPTY_INSTANCE = new UnityProblemAnalyzerContext(UnityProblemAnalyzerContextElement.NONE, null, null);
    }
}