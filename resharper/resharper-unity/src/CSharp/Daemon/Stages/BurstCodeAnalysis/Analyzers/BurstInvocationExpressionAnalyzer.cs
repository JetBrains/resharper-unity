using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstInvocationExpressionAnalyzer : BurstProblemAnalyzerBase<IInvocationExpression>
    {
        protected override void Analyze(IInvocationExpression invocationExpression, IDaemonProcess daemonProcess,
            DaemonProcessKind kind, IHighlightingConsumer consumer)
        {
            //algorithm:
            //if conditional qualifier is open type
            //    return
            //    REASON: burst allows to instantiate generics only with structures. if it is instantiated with class -
            //    it would be highlighted where generics called. if it is instantiated with structure - 2 possible outcomes.
            //    if there are no generic constraints - only object methods can be called, they are handled in BurstReferenceExpressionAnalyzer.
            //    If there is some interface constraint - then it is ok, Burst allows to call interface methods if they are implemented with struct.
            //    CallGraph would have edges to interface through constraints, not instantiation.
            //else
            //    if condional qualifier is class
            //        if conditiinal qualifier is class instance
            //            return
            //            REASON: I will highlight refereceExpression, which would have ReadAccess. Burst can't access 
            //            managed objects. 
            //        else
            //            if function non static
            //                then it would be highlighted as error
            //            else
            //                ok. burst allows invoking static functions.
            //    else
            //        if conditional qualifier is struct instance
            //            if function is virtual/abstract/override
            //                HIGHLIGHT: invocation expressio WITHOUT parameters
            //                ALSO: struct's method are implicitle sealed!
            //                REASON: burst does not support any invocations that use virtual table.
            //                IMPORTANT: type parameters and open types may have some virtual invocations,
            //                but burst generic system allows only structures/primitives to instatiate generics. 
            //                Structure/primitives DOES support some virtual methods from System.Object.
            //            else
            //                it is ok. burst allows any method from structure
            //        else 
            //            if function non static
            //                then it would be highlighted as error
            //            else
            //                ok. burst alows invoking static functions.
            var invokedMethod =
                invocationExpression.InvocationExpressionReference.Resolve().DeclaredElement as IFunction;
            if (invokedMethod == null)
                return;
            if (invokedMethod.IsBurstProhibitedMethod())
            {
                consumer.AddHighlighting(new BC1001Error(invocationExpression.GetDocumentRange(),
                    invokedMethod.ShortName, invokedMethod.GetContainingType()?.ShortName));
            }
            else if (invokedMethod.IsReturnValueProhibited() ||
                     invocationExpression.ArgumentList.HasProhibitedArguments())
            {
                consumer.AddHighlighting(new BC1016Error(invocationExpression.GetDocumentRange(),
                    invokedMethod.ShortName));
            }
        }
    }
}