using System;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages
{
    public abstract class UnityCallGraphCodeProcessor : IRecursiveElementProcessor, IDisposable
    {
        protected ITreeNode StartTreeNode;
        protected readonly SeldomInterruptChecker SeldomInterruptChecker = new SeldomInterruptChecker();

        protected UnityCallGraphCodeProcessor(ITreeNode startTreeNode)
        {
            StartTreeNode = startTreeNode;
        }

        public virtual bool InteriorShouldBeProcessed(ITreeNode element)
        {
            SeldomInterruptChecker.CheckForInterrupt();

            if (element == StartTreeNode)
                return true;

            return !UnityCallGraphUtil.IsFunctionNode(element);
        }

        public abstract void ProcessBeforeInterior(ITreeNode element);

        public void ProcessAfterInterior(ITreeNode element)
        {
        }

        public bool ProcessingIsFinished { get; protected set; }

        public void Dispose()
        {
            StartTreeNode = null;
        }
    }
}