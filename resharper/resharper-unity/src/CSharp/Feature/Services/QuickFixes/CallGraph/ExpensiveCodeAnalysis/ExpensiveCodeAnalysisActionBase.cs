using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    public abstract class ExpensiveCodeAnalysisActionBase : CallGraphActionBase
    {
        protected ExpensiveCodeAnalysisActionBase(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
        public override bool IsAvailable(IUserDataHolder cache)
        {
            var declaredElement = MethodDeclaration?.DeclaredElement;

            return MethodDeclaration != null && MethodDeclaration.IsValid() &&
                   declaredElement != null &&
                   !declaredElement.HasAttributeInstance(ProtagonistAttribute, AttributesSource.Self);
        }
    }
}