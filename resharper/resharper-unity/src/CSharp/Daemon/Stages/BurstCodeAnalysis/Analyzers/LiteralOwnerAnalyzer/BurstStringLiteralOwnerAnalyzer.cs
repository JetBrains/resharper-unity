using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LiteralOwnerAnalyzer
{
    [SolutionComponent]
    public sealed class BurstStringLiteralOwnerAnalyzer : BurstProblemAnalyzerBase<IStringLiteralOwner>
    {
        private readonly IEnumerable<IBurstStringSubAnalyzer> mySubAnalyzers;

        public BurstStringLiteralOwnerAnalyzer(IEnumerable<IBurstStringSubAnalyzer> subAnalyzers)
        {
            mySubAnalyzers = subAnalyzers;
        }

        public bool CheckAndAnalyze(ICSharpExpression startNode, IHighlighting highlighting, IHighlightingConsumer consumer)
        {
            ITreeNode currentNode = startNode;
            var result = false;
            var analyzed = false;

            while (!analyzed && currentNode is ICSharpExpression cSharpExpression)
            {
                foreach (var subAnalyzer in mySubAnalyzers)
                {
                    analyzed = subAnalyzer.TryAnalyze(cSharpExpression, out result);

                    if (analyzed)
                        break;
                }

                currentNode = currentNode.Parent;
            }

            if (!analyzed || result)
            {
                result = true;
                consumer?.AddHighlighting(highlighting);
            }

            return result;
        }

        protected override bool CheckAndAnalyze(IStringLiteralOwner stringLiteralOwner, IHighlightingConsumer consumer,
            IReadOnlyContext context)
        {
            bool isString;

            if (stringLiteralOwner is ICSharpLiteralExpression cSharpLiteralExpression)
                isString = cSharpLiteralExpression.Literal.GetTokenType().IsStringLiteral;
            else
                isString = true;

            return isString && CheckAndAnalyze(stringLiteralOwner, new BurstManagedStringWarning(stringLiteralOwner), consumer);
        }
    }
}