using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public sealed class BurstInvocationExpressionAnalyzer(IOrderedImmutableEnumerable<IBurstProblemSubAnalyzer<IInvocationExpression>> subAnalyzers)
        : BurstAggregatedProblemAnalyzer<IInvocationExpression>(subAnalyzers),
            IBurstBannedAnalyzer
    {
        // if method invoked as instance - warning placed on instance
        // in struct we can't virtual function aside abstract-interface and object methods - so there are no virtual call check
        // invoked on returned value from another function call - we have edge to it function, check it insides and invocation for signature
        // if method is static - it can be called if it has correct signature
        // so even if there is generic constrain to class with correct function - it can be invoked from instance, and instance should be checked
        // virtual method can be only invoked on class - warning on class usage
    }
}