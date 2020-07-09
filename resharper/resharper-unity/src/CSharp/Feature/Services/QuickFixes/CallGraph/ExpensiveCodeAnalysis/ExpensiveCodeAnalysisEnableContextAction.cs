using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public class ExpensiveCodeAnalysisEnableContextAction : ExpensiveCodeAnalysisActionBase
    {
        private const string MESSAGE = "Disable Expensive code analysis";
        public ExpensiveCodeAnalysisEnableContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IClrTypeName ProtagonistAttribute =>
            CallGraphActionUtil.ExpensiveCodeAnalysisEnableAttribute;

        protected override IClrTypeName AntagonistAttribute =>
            CallGraphActionUtil.ExpensiveCodeAnalysisDisableAttribute;

        public override string Text => MESSAGE;
    }
}