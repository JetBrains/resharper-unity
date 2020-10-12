using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public sealed class CallGraphContext
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

        public void Rollback([NotNull] ITreeNode node)
        {
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

        public bool ContainAny(CallGraphContextElement another)
        {
            var context = myStack.Peek().Context;
            
            return (context & another) != CallGraphContextElement.NONE;
        }

        public bool IsSuperSetOf(CallGraphContextElement another)
        {
            var context = myStack.Peek().Context;
            
            // this byte trick check if subContext is subset of myContext
            return ((context & another) ^ another) == CallGraphContextElement.NONE;
        }
    }

}