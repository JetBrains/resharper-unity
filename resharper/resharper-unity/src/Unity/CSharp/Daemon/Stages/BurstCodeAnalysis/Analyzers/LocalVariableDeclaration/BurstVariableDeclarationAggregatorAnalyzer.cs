using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LocalVariableDeclaration
{
    
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public sealed class BurstVariableDeclarationAggregatorAnalyzer(IOrderedImmutableEnumerable<IBurstProblemSubAnalyzer<IMultipleLocalVariableDeclaration>> subAnalyzers)
        : BurstAggregatedProblemAnalyzer<IMultipleLocalVariableDeclaration>(subAnalyzers),
            IBurstBannedAnalyzer;
}