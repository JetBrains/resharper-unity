﻿using System;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using IShaderLabFile = JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.IShaderLabFile;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages
{
    public abstract class ShaderLabDaemonStageProcessBase : TreeNodeVisitor<IHighlightingConsumer>,
        IRecursiveElementProcessor<IHighlightingConsumer>, IDaemonStageProcess
    {
        private readonly IShaderLabFile myFile;

        public IDaemonProcess DaemonProcess { get; }

        protected ShaderLabDaemonStageProcessBase(IDaemonProcess process, IShaderLabFile file)
        {
            DaemonProcess = process;
            myFile = file;
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
            if (element is IShaderLabTreeNode shaderLabElement && !shaderLabElement.IsWhitespaceToken())
                shaderLabElement.Accept(this, consumer);
            else
                VisitNode(element, consumer);
        }

        public virtual void Execute(Action<DaemonStageResult> committer)
        {
            HighlightInFile((file, consumer) => file.ProcessDescendants(this, consumer), committer);
        }

        private void HighlightInFile(Action<IShaderLabFile, IHighlightingConsumer> fileHighlighter,
            Action<DaemonStageResult> commiter)
        {
            var consumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, myFile, DaemonProcess.ContextBoundSettingsStore);
            fileHighlighter(myFile, consumer);
            commiter(new DaemonStageResult(consumer.CollectHighlightings()));
        }
    }
}