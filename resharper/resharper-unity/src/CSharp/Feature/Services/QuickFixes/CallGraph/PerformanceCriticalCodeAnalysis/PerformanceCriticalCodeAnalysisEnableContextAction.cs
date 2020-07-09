using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.PerformanceCriticalCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public class PerformanceCriticalCodeAnalysisEnableContextAction : PerformanceCriticalCodeAnalysisActionBase
    {
        private const string MESSAGE = "Enable Performance critical code analysis";
        public PerformanceCriticalCodeAnalysisEnableContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IClrTypeName ProtagonistAttribute =>
            CallGraphActionUtil.PerformanceCriticalCodeAnalysisEnableAttribute;

        protected override IClrTypeName AntagonistAttribute =>
            CallGraphActionUtil.PerformanceCriticalCodeAnalysisDisableAttribute;

        public override string Text => MESSAGE;
    }
}