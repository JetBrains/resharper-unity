#nullable enable

using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using IShaderLabFile = JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.IShaderLabFile;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages
{
    internal class IdentifierHighlighterProcess : ShaderLabDaemonStageProcessBase
    {
        private readonly DaemonProcessKind myProcessKind;
        private readonly bool myInternalMode;
        private readonly VisualElementHighlighter myVisualElementHighlighter;
        private readonly ResolveProblemHighlighter myResolveProblemHighlighter;
        private readonly IReferenceProvider myReferenceProvider;

        public IdentifierHighlighterProcess(IDaemonProcess process, ResolveHighlighterRegistrar registrar,
                                            IContextBoundSettingsStore settingsStore, DaemonProcessKind processKind,
                                            IShaderLabFile file, bool internalMode)
            : base(process, file)
        {
            myProcessKind = processKind;
            myInternalMode = internalMode;
            myVisualElementHighlighter = new VisualElementHighlighter(ShaderLabLanguage.Instance, settingsStore);
            myResolveProblemHighlighter = new ResolveProblemHighlighter(registrar);
            myReferenceProvider = ((IFileImpl)file).ReferenceProvider;
        }

        public override void VisitNode(ITreeNode node, IHighlightingConsumer consumer)
        {
            var sourceFile = node.GetSourceFile();
            if (sourceFile == null)
                return;
            
            if (!myInternalMode && myProcessKind != DaemonProcessKind.VISIBLE_DOCUMENT)
                return;

            if (myProcessKind == DaemonProcessKind.VISIBLE_DOCUMENT)
            {
                // TODO: Highlight identifiers
                // if (myIdentifierHighlightingEnabled)

                var info = myVisualElementHighlighter.CreateColorHighlightingInfo(node);
                if (info != null)
                    consumer.AddHighlighting(info.Highlighting, info.Range);
            }


            var references = node.GetReferences(myReferenceProvider);
            myResolveProblemHighlighter.CheckForResolveProblems(node, consumer, references);

            // TODO: Move to ShaderLabSyntaxHighlightingStage
            // (Not Rider's syntax highlighting though!)
            // E.g. protobuf support has a SyntaxHighlightingStage that will do this,
            // plus look for simple syntax validation errors, e.g. enums must have at
            // least one value defined, correct value for `syntax "proto3"`, etc.
            // And then a separate identifier
            if (!sourceFile.PsiModule.IsMiscFilesProjectModule() && node is IErrorElement errorElement)
            {
                var range = GetErrorHighlightRange(errorElement);
                if (!range.IsValid())
                    range = node.Parent.GetDocumentRange();
                if (range.TextRange.IsEmpty)
                {
                    if (range.TextRange.EndOffset < range.Document.GetTextLength())
                        range = range.ExtendRight(1);
                    else if (range.TextRange.StartOffset > 0)
                        range = range.ExtendLeft(1);
                }
                consumer.AddHighlighting(new ShaderLabSyntaxError(errorElement.ErrorDescription, range));
            }

            base.VisitNode(node, consumer);
        }

        private DocumentRange GetErrorHighlightRange(IErrorElement errorElement)
        {
            var firstMeaningful = errorElement.FirstChild;
            // skip until first meaningful
            for (; firstMeaningful != null && firstMeaningful.IsFiltered(); firstMeaningful = firstMeaningful.NextSibling) { }
            if (firstMeaningful == null)
                return errorElement.GetDocumentRange();

            var lastNode = firstMeaningful;
            for (var nextSibling = firstMeaningful.NextSibling; nextSibling != null; nextSibling = nextSibling.NextSibling)
            {
                var nodeType = nextSibling.NodeType; 
                if (nodeType.Equals(ShaderLabTokenType.NEW_LINE) 
                    || nodeType.Equals(ShaderLabTokenType.LBRACE) 
                    || nextSibling.Children<ICgContent>().Any())
                    break;
                if (!nextSibling.IsFiltered())
                    lastNode = nextSibling;
            }
            
            return new DocumentRange(firstMeaningful.GetDocumentStartOffset(), lastNode.GetDocumentEndOffset());
        }
    }
}