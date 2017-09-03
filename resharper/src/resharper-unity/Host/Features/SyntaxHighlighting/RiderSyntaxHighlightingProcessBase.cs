#if RIDER

using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

// TODO: Delete this once we have a real Rider SDK

// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Host.Features.SyntaxHighlighting
{
    internal class RiderSyntaxHighlightingProcessBase : IRecursiveElementProcessor<IHighlightingConsumer>, IDaemonStageProcessWithPsiFile
    {
        [NotNull] private readonly IContextBoundSettingsStore mySettingsStore;

        protected RiderSyntaxHighlightingProcessBase([NotNull] IDaemonProcess process, [NotNull] IContextBoundSettingsStore settingsStore, [NotNull] IFile file)
        {
            mySettingsStore = settingsStore;
            DaemonProcess = process;
            File = file;
        }

        public bool InteriorShouldBeProcessed(ITreeNode element, IHighlightingConsumer context) => true;

        public bool IsProcessingFinished(IHighlightingConsumer context)
        {
            if (DaemonProcess.InterruptFlag)
                throw new ProcessCancelledException();
            return false;
        }

        public void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer context)
        {
            var tokenNode = element as ITokenNode;
            if (tokenNode == null) return;
            var tokenNodeType = tokenNode.GetTokenType();
            if (tokenNodeType.IsWhitespace) return;
            var range = tokenNode.GetDocumentRange();
            if (range.TextRange.IsEmpty) return;
            var attributeId = GetAttributeId(tokenNodeType);
            if (!string.IsNullOrEmpty(attributeId))
            {
                context.AddHighlighting(new ReSharperSyntaxHighlighting(attributeId, null, range));
            }
        }

        public void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer context)
        {
        }

        public void Execute(Action<DaemonStageResult> committer)
        {
            var consumer = new DefaultHighlightingConsumer(this, mySettingsStore);
            File.ProcessDescendants(this, consumer);
            committer(new DaemonStageResult(consumer.Highlightings));
        }

        public IDaemonProcess DaemonProcess { get; }
        public IFile File { get; }

        [Pure,CanBeNull]
        protected virtual string GetAttributeId(TokenNodeType tokenType)
        {
            // Note that we get the default implementation of the real base class,
            // So we only want to override IsBlockComment and IsNumber (and indeed
            // GetAttributeId)
            if (IsBlockComment(tokenType)) return HighlightingAttributeIds.BLOCK_COMMENT;
            if (IsNumber(tokenType)) return HighlightingAttributeIds.NUMBER;
            if (IsKeyword(tokenType)) return HighlightingAttributeIds.KEYWORD;
            return null;
        }

        protected virtual bool IsBlockComment(TokenNodeType tokenType)
        {
            return false;
        }

        protected virtual bool IsNumber(TokenNodeType tokenType)
        {
            return false;
        }

        protected virtual bool IsKeyword(TokenNodeType tokenType)
        {
            return false;
        }

        [StaticSeverityHighlighting(Severity.INFO, "ReSharperSyntaxHighlighting", OverlapResolve = OverlapResolveKind.NONE, ShowToolTipInStatusBar = false)]
        private class ReSharperSyntaxHighlighting : ICustomAttributeIdHighlighting
        {
            private readonly DocumentRange myRange;

            public ReSharperSyntaxHighlighting(string attributeId, string toolTip, DocumentRange range)
            {
                AttributeId = attributeId;
                ToolTip = toolTip;
                myRange = range;
            }

            public string AttributeId { get; }
            public string ToolTip { get; }
            public string ErrorStripeToolTip => null;
            public bool IsValid() => true;
            public DocumentRange CalculateRange() => myRange;
        }
    }
}

#endif