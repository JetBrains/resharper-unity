using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    [DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
                 StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class CgSyntaxHighlightingStage : CgDaemonStageBase
    {
        // TODO: not sure if this the correct way for context-sensitive keywords
        private static readonly string[] ourParametrizedSemantics =  {
            // d3d9+, vertex, input
            "BINORMAL",
            "BLENDINDICES",
            "BLENDWEIGHT",
            "COLOR", // vp, io
            "NORMAL",
            "POSITION", // io
            "PSIZE", // listed as fixed for vo and parametrized for vi
            "TANGENT",
            "TEXCOORD", // v io, pi
                
            // output
            "TESSFACTOR",
                
            // pixel, output
            "DEPTH",
                
            // d3d10 System-Value semantics
            "SV_ClipDistance",
            "SV_CullDistance",
            "SV_Target"
        };

        private static readonly string[] ourFixedSemantics = {
            // d3d9+, vertex, input
            "POSITIONT",
            // output
            "FOG",
                
            // pixel, input
            "VFACE",
            "VPOS",
            // pixel, output
                
            // d3d10 System-Value semantics
            "SV_Coverage",
            "SV_Depth",
            "SV_DepthGreaterEqual", // ? 
            "SV_DepthLessEqual",    // not sure how these two are used properly
            "SV_DispatchThreadID",
            "SV_DomainLocation",
            "SV_GroupID",
            "SV_GroupIndex",
            "SV_GroupThreadID",
            "SV_GSInstanceID",
            "SV_InnerCoverage",
            "SV_InsideTessFactor",
            "SV_InstanceID",
            "SV_IsFrontFace",
            "SV_OutputControlPointID",
            "SV_Position",
            "SV_PrimitiveID",
            "SV_RenderTargetArrayIndex",
            "SV_SampleIndex",
            "SV_StencilRef",
            "SV_TessFactor",
            "SV_VertexID",
            "SV_ViewportArrayIndex"
        };
        
        private readonly CgSupportSettings myCgSupportSettings;
        private readonly JetHashSet<string> mySemantics;
        
        public CgSyntaxHighlightingStage(CgSupportSettings cgSupportSettings)
        {
            myCgSupportSettings = cgSupportSettings;
            mySemantics = CreateSemanticsSet();
        }

        private JetHashSet<string> CreateSemanticsSet()
        {
            var result = new JetHashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var semantic in ourParametrizedSemantics)
            {
                result.Add(semantic);
                for (int i = 0; i < 9; i++)
                {
                    result.Add($"{semantic}{i}");
                }
            }

            foreach (var semantic in ourFixedSemantics)
            {
                result.Add(semantic);
            }

            return result;
        }

        protected override IDaemonStageProcess CreateProcess(
            IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind,
            ICgFile file)
        {
            return new CgSyntaxHighlightingProcess(process, file, myCgSupportSettings.IsErrorHighlightingEnabled.Value, mySemantics);
        }

        private class CgSyntaxHighlightingProcess : CgDaemonStageProcessBase
        {
            private readonly bool myIsErrorHighlightingEnabled;
            private readonly JetHashSet<string> mySemantics;

            public CgSyntaxHighlightingProcess(IDaemonProcess daemonProcess, ICgFile file, bool isErrorHighlightingEnabled, JetHashSet<string> semantics)
                : base(daemonProcess, file)
            {
                myIsErrorHighlightingEnabled = isErrorHighlightingEnabled;
                mySemantics = semantics;
            }
            
            public override void VisitConstantValueNode(IConstantValue constantValueParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.NUMBER, constantValueParam.GetDocumentRange()));
                base.VisitConstantValueNode(constantValueParam, context);
            }

            public override void VisitAsmStatementNode(IAsmStatement asmStatementParam, IHighlightingConsumer context)
            {
                // TODO: custom HighlightingAttributeId
                context.AddHighlighting(new CgHighlighting(HighlightingAttributeIds.INJECT_STRING_BACKGROUND, asmStatementParam.ContentNode.GetDocumentRange()));
                base.VisitAsmStatementNode(asmStatementParam, context);
            }

            public override void VisitSemanticNode(ISemantic semanticParam, IHighlightingConsumer context)
            {
                if (semanticParam.NameNode is CgIdentifierTokenNode id && !id.Name.IsNullOrEmpty())
                {
                    if (mySemantics.Contains(id.Name))
                    {
                        context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD,
                            id.GetDocumentRange()));
                    }
                    else if (myIsErrorHighlightingEnabled)
                    {
                        var range = GetErrorRange(id);
                        context.AddHighlighting(new CgSyntaxError("Semantic, packoffset or register expected", range));
                    }
                }
                
                base.VisitSemanticNode(semanticParam, context);
            }

            public override void VisitNode(ITreeNode node, IHighlightingConsumer context)
            {   
                if (myIsErrorHighlightingEnabled && node is IErrorElement errorElement)
                {
                    var range = GetErrorRange(errorElement);
                    context.AddHighlighting(new CgSyntaxError(errorElement.ErrorDescription, range), range);
                }

                base.VisitNode(node, context);
            }

            private static DocumentRange GetErrorRange(ITreeNode node)
            {
                var range = node.GetDocumentRange();
                if (!range.IsValid())
                    range = node.Parent.GetDocumentRange();
                if (range.TextRange.IsEmpty)
                {
                    if (range.TextRange.EndOffset < range.Document.GetTextLength())
                        range = range.ExtendRight(1);
                    else
                        range = range.ExtendLeft(1);
                }

                return range;
            }
        }
    }
}