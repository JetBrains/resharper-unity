using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LocalVariableDeclaration
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class BurstStringAssignmentAnalyzer : IBurstProblemSubAnalyzer<IMultipleLocalVariableDeclaration>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IMultipleLocalVariableDeclaration t, IHighlightingConsumer consumer)
        {
            var multipleDeclarationMembers = t.Declarators;
            foreach (var multipleDeclarationMember in multipleDeclarationMembers)
            {
                if (multipleDeclarationMember is ILocalVariableDeclaration { IsVar: true,  } localVariableDeclaration
                    && localVariableDeclaration.Type.IsString())
                {
                    consumer?.AddHighlighting(new BurstLocalStringVariableDeclarationWarning(localVariableDeclaration.Initial, multipleDeclarationMember));
                }
            }

            return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
        }

        public int Priority => 1000;
    }
}