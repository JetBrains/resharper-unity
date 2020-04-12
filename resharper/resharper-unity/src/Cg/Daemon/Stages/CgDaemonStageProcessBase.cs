using System;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    public abstract class CgDaemonStageProcessBase : TreeNodeVisitor<IHighlightingConsumer>, IRecursiveElementProcessor<IHighlightingConsumer>, IDaemonStageProcessWithPsiFile
    {
        private readonly ICgFile myFile;
        
        
        public IDaemonProcess DaemonProcess { get; }
        public IFile File => myFile;

        protected CgDaemonStageProcessBase(IDaemonProcess daemonProcess, ICgFile file)
        {
            DaemonProcess = daemonProcess;
            myFile = file;
        }
        
        public virtual bool InteriorShouldBeProcessed(ITreeNode element, IHighlightingConsumer context)
        {
            return true;
        }

        public bool IsProcessingFinished(IHighlightingConsumer context)
        {
            if (DaemonProcess.InterruptFlag)
                throw new OperationCanceledException();
            
            return false;
        }

        public virtual void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer context)
        {
        }

        public virtual void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer context)
        {
            if (element is ICgTreeNode cgElement)
            {
                var tokenNode = cgElement as ITokenNode;
                if (tokenNode == null || !tokenNode.GetTokenType().IsWhitespace)
                    cgElement.Accept(this, context);
            }
            else
            {
                VisitNode(element, context);
            }
        }

        public void Execute(Action<DaemonStageResult> committer)
        {
            HighlightInFile((file, consumer) => file.ProcessDescendants(this, consumer), committer);
        }
        
        private void HighlightInFile(Action<ICgFile, IHighlightingConsumer> fileHighlighter, Action<DaemonStageResult> committer)
        {
            var consumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, myFile, DaemonProcess.ContextBoundSettingsStore);
            fileHighlighter(myFile, consumer);
            committer(new DaemonStageResult(consumer.Highlightings));
        }

    }
}