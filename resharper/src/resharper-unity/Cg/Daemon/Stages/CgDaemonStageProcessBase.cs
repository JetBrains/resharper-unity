using System;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    public abstract class CgDaemonStageProcessBase : TreeNodeVisitor<IHighlightingConsumer>, IRecursiveElementProcessor<IHighlightingConsumer>, IDaemonStageProcessWithPsiFile
    {
        private readonly IContextBoundSettingsStore mySettingsStore;
        private readonly ICgFile myFile;
        
        
        public IDaemonProcess DaemonProcess { get; }
        public IFile File => myFile;

        protected CgDaemonStageProcessBase(IDaemonProcess daemonProcess, IContextBoundSettingsStore settingsStore, ICgFile file)
        {
            mySettingsStore = settingsStore;
            DaemonProcess = daemonProcess;
            myFile = file;
        }

        protected void HighlightInFile(Action<ICgFile, IHighlightingConsumer> fileHighlighter, Action<DaemonStageResult> commiter)
        {
            var consumer = new FilteringHighlightingConsumer(this, mySettingsStore, myFile);
            fileHighlighter(myFile, consumer);
            commiter(new DaemonStageResult(consumer.Highlightings));
        }
        
        public virtual bool InteriorShouldBeProcessed(ITreeNode element, IHighlightingConsumer context)
        {
            return true;
        }

        public bool IsProcessingFinished(IHighlightingConsumer context)
        {
            if (DaemonProcess.InterruptFlag)
                throw new ProcessCancelledException();
            
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
    }
}