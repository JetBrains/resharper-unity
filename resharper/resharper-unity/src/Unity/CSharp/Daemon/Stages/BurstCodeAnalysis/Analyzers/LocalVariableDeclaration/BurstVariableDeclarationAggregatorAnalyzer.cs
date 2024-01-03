using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LocalVariableDeclaration
{
    
    [SolutionComponent]
    public sealed class BurstVariableDeclarationAggregatorAnalyzer : BurstAggregatedProblemAnalyzer<IMultipleLocalVariableDeclaration>,
        IBurstBannedAnalyzer
    {
        public BurstVariableDeclarationAggregatorAnalyzer(IEnumerable<IBurstProblemSubAnalyzer<IMultipleLocalVariableDeclaration>> subAnalyzers) 
            : base(subAnalyzers)
        {
        }
    }
}