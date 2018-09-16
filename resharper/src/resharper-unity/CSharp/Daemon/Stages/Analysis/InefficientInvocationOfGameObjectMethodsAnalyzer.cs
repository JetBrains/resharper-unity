using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new[] { typeof(InefficientInvocationOfGameObjectMethodsWarning) })]
    public class InefficientInvocationOfGameObjectMethodsAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        
        private static readonly ISet<string> ourKnownMethods = new HashSet<string>()
        {
            "SendMessage",
            "SendMessageUpwards",
            "BroadcastMessage",
        };
        
        public InefficientInvocationOfGameObjectMethodsAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var reference = expression.Reference;
            if (reference == null) 
                return;
            
            var info = reference.Resolve();
            var invocation = (expression.InvokedExpression as IReferenceExpression)?.Reference;

            if (info.ResolveErrorType != ResolveErrorType.OK)
                return;

            var method = (info.DeclaredElement as IMethod).NotNull("info.DeclaredElement as IMethod != null");

            if (!ourKnownMethods.Contains(method.ShortName))
                return;
            
            var containingType = method.GetContainingType();
            if (containingType == null) 
                return;
            
            if (containingType.GetClrName().Equals(KnownTypes.GameObject))
            {
                consumer.AddHighlighting(new InefficientInvocationOfGameObjectMethodsWarning(invocation));
            }
        }
    }
}