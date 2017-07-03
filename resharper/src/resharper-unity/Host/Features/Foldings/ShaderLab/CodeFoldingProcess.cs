#if RIDER

using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Host.Features.Foldings.ShaderLab
{
    // TODO: Implement ICodeFoldingProcessorFactory and add tests once JetBrains.ReSharper.Host is part of SDK
    internal class CodeFoldingProcess : ShaderLabDaemonStageProcessBase
    {
        private readonly IContextBoundSettingsStore mySettingsStore;
        private readonly IShaderLabFile myFile;

        public CodeFoldingProcess(IDaemonProcess process, IContextBoundSettingsStore settingsStore, IShaderLabFile file)
            : base(process, settingsStore, file)
        {
            mySettingsStore = settingsStore;
            myFile = file;
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            var consumer = new DefaultHighlightingConsumer(this, mySettingsStore);
            myFile.ProcessDescendants(this, consumer);
            var foldings = AppendRangeWithOverlappingResolve(consumer.Highlightings);

            committer(new DaemonStageResult(foldings));
        }

        public override void VisitNode(ITreeNode node, IHighlightingConsumer consumer)
        {
            if (node.NodeType == ShaderLabTokenType.MULTI_LINE_COMMENT)
                AddCodeFolding(consumer, node, "/*...*/");
        }

        public override void VisitBlockValueNode(IBlockValue blockValue, IHighlightingConsumer consumer)
        {
            AddCodeFolding(consumer, blockValue);
        }

        public override void VisitGrabPassValueNode(IGrabPassValue grabPassValue, IHighlightingConsumer consumer)
        {
            AddCodeFolding(consumer, grabPassValue);
        }

        public override void VisitRegularPassValueNode(IRegularPassValue regularPassValue, IHighlightingConsumer consumer)
        {
            AddCodeFolding(consumer, regularPassValue);
        }

        public override void VisitCgContentNode(ICgContent cgContent, IHighlightingConsumer consumer)
        {
            AddCodeFolding(consumer, cgContent);
        }

        private void AddCodeFolding(IHighlightingConsumer consumer, ITreeNode target, string placeholder = "{..}")
        {
            var highlighting = CreateCodeFolding(placeholder, target.GetHighlightingRange());
            if (highlighting != null)
                consumer.AddHighlighting(highlighting);
        }

        private IHighlighting CreateCodeFolding(string placeholderText, DocumentRange range)
        {
            return IsNotEmptyNormalized(range)
                ? CodeFoldingHighlightingCreator.Create("ReSharper Default Folding", placeholderText, range,
                    10 /* Default priority */)
                : null;
        }

        private static bool IsNotEmptyNormalized(DocumentRange range)
        {
            var textRange = range.TextRange;
            return range.IsValid() && textRange.StartOffset < textRange.EndOffset;
        }

        internal static IList<HighlightingInfo> AppendRangeWithOverlappingResolve(IList<HighlightingInfo> range)
        {
            var result = new List<HighlightingInfo>();
            // Can be done in one loop if needed, but a lot less readable
            // Sort by folding-priority and range
            range.StableSort(FoldingComparer.Instance);
            foreach (var h in range)
            {
                InsertFolding(result, h);
            }
            return result;
        }

        private static void InsertFolding(IList<HighlightingInfo> result, HighlightingInfo highlightingInfo)
        {
            var textRange = highlightingInfo.Highlighting.CalculateRange().TextRange;
            var start = textRange.StartOffset;
            var end = textRange.EndOffset;

            var insertIndex = result.Count;
            for (var i = 0; i < result.Count; i++)
            {
                var range = result[i].Highlighting.CalculateRange().TextRange;
                var rStart = range.StartOffset;
                var rEnd = range.EndOffset;
                if (rStart < start)
                {
                    if (start < rEnd && rEnd < end)
                        return;
                }
                else if (rStart == start)
                {
                    if (rEnd == end)
                        return;
                    if (rEnd > end)
                        insertIndex = Math.Min(insertIndex, i);
                }
                else
                {
                    insertIndex = Math.Min(insertIndex, i);
                    if (rStart > end) break;
                    if (rStart < end && end < rEnd)
                        return;
                }
            }
            result.Insert(insertIndex, highlightingInfo);
        }

        internal class FoldingComparer : IComparer<HighlightingInfo>
        {
            public static readonly FoldingComparer Instance = new FoldingComparer();

            public int Compare(HighlightingInfo x, HighlightingInfo y)
            {
                var startOffset = x.Range.TextRange.StartOffset.CompareTo(y.Range.TextRange.StartOffset);
                if (startOffset != 0) return startOffset;
                var endOffset = x.Range.TextRange.EndOffset.CompareTo(y.Range.TextRange.EndOffset);
                return endOffset;
            }
        }
    }
}

#endif