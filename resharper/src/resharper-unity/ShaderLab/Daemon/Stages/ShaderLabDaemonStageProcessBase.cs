using System;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages
{
    public abstract class ShaderLabDaemonStageProcessBase : TreeNodeVisitor<IHighlightingConsumer>,
        IRecursiveElementProcessor<IHighlightingConsumer>, IDaemonStageProcess
    {
        private readonly IContextBoundSettingsStore mySettingsStore;
        private readonly IShaderLabFile myFile;
        private IDocument myDocument;

        protected ShaderLabDaemonStageProcessBase(IDaemonProcess process, IContextBoundSettingsStore settingsStore, IShaderLabFile file)
        {
            DaemonProcess = process;
            mySettingsStore = settingsStore;
            myFile = file;
            myDocument = process.Document;
        }

        public bool IsProcessingFinished(IHighlightingConsumer context)
        {
            if (DaemonProcess.InterruptFlag)
                throw new OperationCanceledException();
            return false;
        }

        public virtual bool InteriorShouldBeProcessed(ITreeNode element, IHighlightingConsumer consumer)
        {
            return !IsProcessingFinished(consumer);
        }

        public virtual void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
        }

        public virtual void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            var shaderLabElement = element as IShaderLabTreeNode;

            if (shaderLabElement != null && !shaderLabElement.IsWhitespaceToken())
                shaderLabElement.Accept(this, consumer);
            else
                VisitNode(element, consumer);
        }

        public virtual void Execute(Action<DaemonStageResult> committer)
        {
            HighlightInFile((file, consumer) => file.ProcessDescendants(this, consumer), committer);
        }

        public IDaemonProcess DaemonProcess { get; }

        protected void HighlightInFile(Action<IShaderLabFile, IHighlightingConsumer> fileHighlighter,
            Action<DaemonStageResult> commiter)
        {
            var consumer = new FilteringHighlightingConsumer(myFile.GetSourceFile(), myFile);
            fileHighlighter(myFile, consumer);
            commiter(new DaemonStageResult(consumer.Highlightings));
        }
    }
}