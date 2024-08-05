using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LocalVariableDeclaration
{
    
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public sealed class BurstVariableDeclarationAggregatorAnalyzer : BurstAggregatedProblemAnalyzer<IMultipleLocalVariableDeclaration>,
        IBurstBannedAnalyzer
    {
        public BurstVariableDeclarationAggregatorAnalyzer(IEnumerable<IBurstProblemSubAnalyzer<IMultipleLocalVariableDeclaration>> subAnalyzers) 
            : base(subAnalyzers)
        {
        }
    }
}