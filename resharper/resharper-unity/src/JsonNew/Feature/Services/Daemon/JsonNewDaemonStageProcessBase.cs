using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.Services.Daemon
{
    public abstract class JsonNewDaemonStageProcessBase : TreeNodeVisitor<IHighlightingConsumer>,
        IRecursiveElementProcessor<IHighlightingConsumer>, IDaemonStageProcessWithPsiFile
    {
        private readonly IJsonNewFile myFile;
        private readonly IDaemonProcess myDaemonProcess;

        protected JsonNewDaemonStageProcessBase([NotNull] IDaemonProcess process, IJsonNewFile file)
        {
            myFile = file;
            myDaemonProcess = process;
        }

        public IFile File => myFile;
        public IDaemonProcess DaemonProcess => myDaemonProcess;

        protected void HighlightInFile(Action<IJsonNewFile, IHighlightingConsumer> fileHighlighter,
            Action<DaemonStageResult> commiter)
        {
            var consumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, myFile,
                DaemonProcess.ContextBoundSettingsStore);
            fileHighlighter(myFile, consumer);
            commiter(new DaemonStageResult(consumer.Highlightings));
        }

        public abstract void Execute(Action<DaemonStageResult> committer);

        public virtual bool InteriorShouldBeProcessed(ITreeNode element, IHighlightingConsumer context) => true;

        public bool IsProcessingFinished(IHighlightingConsumer context)
        {
            if (myDaemonProcess.InterruptFlag)
                throw new OperationCanceledException();
            return false;
        }

        public virtual void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
        }

        public virtual void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (element is IJsonNewTreeNode jsonElement)
            {
                var tokenNode = jsonElement as ITokenNode;
                if (tokenNode == null || !tokenNode.GetTokenType().IsWhitespace)
                    jsonElement.Accept(this, consumer);
            }
            else
            {
                VisitNode(element, consumer);
            }
        }
    }
}