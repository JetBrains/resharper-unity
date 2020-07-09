using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    public class BurstCodeAnalysisAddDiscardAttributeAction : BurstCodeAnalysisActionBase
    {
        protected const string Message = "Add BurstDiscard attribute";

        protected BurstCodeAnalysisAddDiscardAttributeAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
        
        protected BurstCodeAnalysisAddDiscardAttributeAction(IBurstHighlighting burstHighlighting)
            : base(burstHighlighting)
        {
        }
        
        public BurstCodeAnalysisAddDiscardAttributeAction(IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        protected override IClrTypeName ProtagonistAttribute => KnownTypes.BurstDiscardAttribute;
        protected override IClrTypeName AntagonistAttribute => null;
        public override string Text => Message;
    }
}