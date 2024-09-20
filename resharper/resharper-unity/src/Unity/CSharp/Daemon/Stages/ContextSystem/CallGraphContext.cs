using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public interface IReadOnlyCallGraphContext
    {
        DaemonProcessKind Kind { get; }
        [NotNull]
        IDaemonProcess DaemonProcess { get; }
        [CanBeNull]
        CallGraphDataElement DataElement { get; }
        CallGraphContextTag CurrentContext { get; }
        bool ContainAny(CallGraphContextTag another);
        bool IsSuperSetOf(CallGraphContextTag another);
    }

    public sealed class CallGraphContext : IReadOnlyCallGraphContext
    {
        private readonly DaemonProcessKind myProcessKind;
        [NotNull] private readonly IDaemonProcess myDaemonProcess;
        [CanBeNull] private readonly CallGraphDataElement myGraphDataElement;
        [NotNull] private readonly Stack<BoundContextTag> myStack = new Stack<BoundContextTag>();

        public CallGraphContext(DaemonProcessKind processKind, [NotNull] IDaemonProcess daemonProcess)
        {
            myProcessKind = processKind;
            myDaemonProcess = daemonProcess;

            if (processKind == DaemonProcessKind.VISIBLE_DOCUMENT)
            {
                var collectUsageProcess = daemonProcess.GetStageProcess<CollectUsagesStageProcess>().NotNull();
                if (collectUsageProcess.SwaExtensionsUsageDataInfo.TryGetValue(CallGraphSwaExtensionProvider.Id, out var dataElement))
                    myGraphDataElement = dataElement as CallGraphDataElement;
            }

            myStack.Push(new BoundContextTag(CallGraphContextTag.NONE, null));
        }

        public void Rollback([NotNull] ITreeNode node)
        {
            // null node can occasionally broke first stack entry.
            var boundElement = myStack.Peek();

            if (boundElement.Node == node)
                myStack.Pop();
        }

        private CallGraphContextTag GetNodeContext([NotNull] ITreeNode node, [NotNull] ICallGraphContextProvider contextProvider)
        {
            var declaredElement = CallGraphContextProviderEx.ExtractDeclaredElementForProvider(node);
            
            if (declaredElement == null)
                return CallGraphContextTag.NONE;
            
            switch (myProcessKind)
            {
                case DaemonProcessKind.VISIBLE_DOCUMENT:
                {
                    if (contextProvider.IsMarkedLocal(declaredElement, myGraphDataElement))
                        return contextProvider.ContextTag;
                    
                    break;
                }
                case DaemonProcessKind.GLOBAL_WARNINGS:
                {
                    if (contextProvider.IsMarkedGlobal(declaredElement))
                        return contextProvider.ContextTag;
                    
                    break;
                }
            }

            return CallGraphContextTag.NONE;
        }
        
        public void AdvanceContext([NotNull] ITreeNode node, [NotNull] IImmutableEnumerable<ICallGraphContextProvider> providers)
        {
            var newContext = CallGraphContextTag.NONE;
            var context = myStack.Peek().Context;
            var shouldChange = false;
    
            foreach (var provider in providers)
            {
                if (provider.IsContextChangingNode(node))
                {
                    newContext |= GetNodeContext(node, provider);
                    shouldChange = true;
                }
                else if (context.HasFlag(provider.ContextTag))
                    newContext |= provider.ContextTag;
            }
    
            if (shouldChange)
                myStack.Push(new BoundContextTag(newContext, node));
        }

        public DaemonProcessKind Kind => myProcessKind;
        public IDaemonProcess DaemonProcess => myDaemonProcess;
        public CallGraphDataElement DataElement => myGraphDataElement;
        public CallGraphContextTag CurrentContext => myStack.Peek().Context;

        public bool ContainAny(CallGraphContextTag another)
        {
            var context = CurrentContext;
            
            return (context & another) != CallGraphContextTag.NONE;
        }

        public bool IsSuperSetOf(CallGraphContextTag another)
        {
            var context = CurrentContext;
            
            // this byte trick checks if `context` is superset of `another`
            // context - we have only `context` bits
            // context & another - `context` bits which are present at `another`
            // (context & another) ^ another - `context` bits which are present at `another` became 0,
            // if `another` have any bits that are not present in `context` then expression won't be 0
            // it means that `context` is not superset of `another`
            return ((context & another) ^ another) == CallGraphContextTag.NONE;
        }

        private readonly struct BoundContextTag
        {
            public readonly CallGraphContextTag Context;
            public readonly ITreeNode Node;

            public BoundContextTag(CallGraphContextTag context, ITreeNode node)
            {
                Context = context;
                Node = node;
            }
        }
    }

}