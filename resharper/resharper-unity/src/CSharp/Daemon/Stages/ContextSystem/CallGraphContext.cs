using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public interface IReadOnlyContext
    {
        CallGraphContextElement CurrentContext { get; }
        bool ContainAny(CallGraphContextElement another);
        bool IsSuperSetOf(CallGraphContextElement another);
    }

    public sealed class CallGraphContext : IReadOnlyContext
    {
        private readonly struct BoundContextElement
        {
            public readonly CallGraphContextElement Context;
            public readonly ITreeNode Node;

            public BoundContextElement(CallGraphContextElement context, ITreeNode node)
            {
                Context = context;
                Node = node;
            }
        }
        
        private readonly Stack<BoundContextElement> myStack = new Stack<BoundContextElement>();

        public CallGraphContext()
        {
            myStack.Push(new BoundContextElement(CallGraphContextElement.NONE, null));
        }

        public void Rollback([NotNull] ITreeNode node)
        {
            // null node can occasionally broke first stack entry.
            var boundElement = myStack.Peek();

            if (boundElement.Node == node)
                myStack.Pop();
        }
        
        public void AdvanceContext([NotNull] ITreeNode node, DaemonProcessKind processKind, [NotNull] IEnumerable<ICallGraphContextProvider> providers)
        {
            var newContext = CallGraphContextElement.NONE;
            var context = myStack.Peek().Context;
            var shouldChange = false;
    
            foreach (var provider in providers)
            {
                if (provider.IsContextChangingNode(node))
                {
                    newContext |= provider.GetNodeContext(node, processKind);
                    shouldChange = true;
                }
                else if (context.HasFlag(provider.Context))
                    newContext |= provider.Context;
            }
    
            if (shouldChange)
                myStack.Push(new BoundContextElement(newContext, node));
        }

        public CallGraphContextElement CurrentContext => myStack.Peek().Context;

        public bool ContainAny(CallGraphContextElement another)
        {
            var context = CurrentContext;
            
            return (context & another) != CallGraphContextElement.NONE;
        }

        public bool IsSuperSetOf(CallGraphContextElement another)
        {
            var context = CurrentContext;
            
            // this byte trick checks if `context` is superset of `another`
            // context - we have only `context` bits
            // context & another - `context` bits which are present at `another`
            // (context & another) ^ another - `context` bits which are present at `another` became 0,
            // if `another` have any bits that are not present in `context` then expression won't be 0
            // it means that `context` is not superset of `another`
            return ((context & another) ^ another) == CallGraphContextElement.NONE;
        }
    }

}