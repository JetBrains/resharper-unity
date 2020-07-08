using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    public class BurstDiscardAttributeAction : BurstAnalysisActionBase
    {
        protected BurstDiscardAttributeAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
        
        protected BurstDiscardAttributeAction(IBurstHighlighting burstHighlighting)
            : base(burstHighlighting)
        {
        }
        
        public BurstDiscardAttributeAction(IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        protected override IClrTypeName ProtagonistAttribute => KnownTypes.BurstDiscardAttribute;
        protected override IClrTypeName AntagonistAttribute => null;
        public override string Text => "Add Burst Discard attribute";
    }
}