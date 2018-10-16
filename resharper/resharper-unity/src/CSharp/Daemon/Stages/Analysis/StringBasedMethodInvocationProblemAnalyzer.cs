using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new[] { typeof(StringBasedMethodInvocationProblemWarning) })]
    public class StringBasedMethodInvocationProblemAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        
        private static readonly IDictionary<string, IClrTypeName> ourKnownMethods = new Dictionary<string, IClrTypeName>()
        {
            {"SendMessage", KnownTypes.GameObject},
            {"SendMessageUpwards", KnownTypes.GameObject},
            {"BroadcastMessage", KnownTypes.GameObject},
            {"Invoke", KnownTypes.MonoBehaviour},
            {"InvokeRepeating", KnownTypes.MonoBehaviour}
        };
        
        public StringBasedMethodInvocationProblemAnalyzer(UnityApi unityApi)
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

            if (!ourKnownMethods.ContainsKey(method.ShortName))
                return;
            
            var containingType = method.GetContainingType();
            if (containingType == null) 
                return;
            
            if (containingType.GetClrName().Equals(ourKnownMethods[method.ShortName]))
            {
                consumer.AddHighlighting(new StringBasedMethodInvocationProblemWarning(invocation));
            }
        }
    }
}