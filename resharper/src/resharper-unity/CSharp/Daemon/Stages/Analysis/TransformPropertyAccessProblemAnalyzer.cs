using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Util;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ControlFlow;
using JetBrains.ReSharper.Psi.CSharp.ControlFlow;
using JetBrains.ReSharper.Psi.CSharp.Impl.ControlFlow;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IBlock), HighlightingTypes = new[] { typeof(InefficientPropertyAccessWarning)})]
    public class TransformPropertyAccessProblemAnalyzer : UnityElementProblemAnalyzer<IBlock>
    {
        public TransformPropertyAccessProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IBlock block, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (block.GetContainingNode<IBlock>() != null)
                return;

            var result = new Dictionary<string, List<IReferenceExpression>>();

            var memoryCount = new Dictionary<string, int>();
            var canApplyFix = new Dictionary<string, bool>();

            AnalyzeBlock(block, data, consumer, result, memoryCount, canApplyFix);

            foreach (var key in result.Keys)
            {
                if (memoryCount[key] > 2)
                {
                    // the third usage is inefficient, so highlight it.
                    var toHighlight = result[key].Count >= 2 ? result[key][2] : result[key].Last();
                    consumer.AddHighlighting(new InefficientPropertyAccessWarning(toHighlight, result[key].ToArray(), canApplyFix[key]));
                }
            }
          
        }

        private bool AnalyzeBlock(IBlock block, ElementProblemAnalyzerData data, IHighlightingConsumer consumer,
            [NotNull] IDictionary<string, List<IReferenceExpression>> references, Dictionary<string, int> memoryOperationCount, 
            Dictionary<string, bool> canApplyFix, bool isBadStatementFound = false)
        {
            foreach (var statement in block.Statements)
            {
                if (statement is IBlock innerBlock)
                {
                    isBadStatementFound = AnalyzeBlock(innerBlock, data, consumer, references, 
                        memoryOperationCount, canApplyFix, isBadStatementFound);
                    continue;
                }

                var descendantsEnumerator = statement.Descendants();
                var anyReferenceFound = new HashSet<string>();
                
                while (descendantsEnumerator.MoveNext())
                {
                    switch (descendantsEnumerator.Current)
                    {
                        case ICSharpClosure _:
                            descendantsEnumerator.SkipThisNode();
                            continue;
                        case IReferenceExpression referenceExpression:
                            var info = referenceExpression.Reference.Resolve();
                    
                            if (info.ResolveErrorType != ResolveErrorType.OK) 
                                continue;

                            var property = info.DeclaredElement as IProperty;
                            var containingType = property?.GetContainingType();
                            if (containingType == null || !containingType.GetClrName().Equals(KnownTypes.Transform))
                                continue;

                            var name = property.ShortName;
                            
                            if (!references.ContainsKey(name))
                            {
                                references[name] = new List<IReferenceExpression>();
                                memoryOperationCount[name] = 0;
                            }
                            anyReferenceFound.Add(name);
                            references[name].Add(referenceExpression);
                    
                            memoryOperationCount[name]++;
                            if (statement is IExpressionStatement expressionStatement &&
                                expressionStatement.Expression is IAssignmentExpression assignment &&
                                assignment.IsCompoundAssignment) // write and read (+=, -= e.t.c)
                                memoryOperationCount[name]++;
                            break;
                    }
                } 
                
                isBadStatementFound |= IsStatementBad(statement);
                foreach (var name in anyReferenceFound)
                { 
                    canApplyFix[name] = !isBadStatementFound;
                }
            }

            return isBadStatementFound;
        }

        private bool IsStatementBad(IStatement statement)
        {
            var descendantsEnumerator = statement.Descendants();
            while (descendantsEnumerator.MoveNext())
            {
                switch (descendantsEnumerator.Current)
                {
                    case ICSharpClosure _:
                        descendantsEnumerator.SkipThisNode();
                        continue;
                    case IInvocationExpression invocationExpression:
                        return true;
                }
            }

            return false;
        }
    }
}