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
    public class ExpensiveCodeAnalysisDisableContextAction : ExpensiveCodeAnalysisActionBase
    {
        private const string MESSAGE = "Enable Expensive code analysis";

        public ExpensiveCodeAnalysisDisableContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IClrTypeName ProtagonistAttribute =>
            CallGraphActionUtil.ExpensiveCodeAnalysisDisableAttribute;

        protected override IClrTypeName AntagonistAttribute =>
            CallGraphActionUtil.ExpensiveCodeAnalysisEnableAttribute;

        public override string Text => MESSAGE;
    }
}