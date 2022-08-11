#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new[]
    {
        typeof(UnknownResourceWarning)
    })]
    public class LoadUnknownResourceAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private readonly ResourceLoadCache myResourceLoadCache;
        
        public LoadUnknownResourceAnalyzer(UnityApi unityApi,
            ResourceLoadCache resourceLoadCache)
            : base(unityApi)
        {
            myResourceLoadCache = resourceLoadCache;
        }

        internal static ICSharpArgument? FindArgument(TreeNodeCollection<ICSharpArgument> arguments, string argumentName, int possibleArgumentIndex = 0)
        {
            foreach (ICSharpArgument? argument in arguments)
            {
                if(argument.IsNamedArgument && argument.NameIdentifier != null && argument.NameIdentifier.Name.Equals(argumentName))
                    return argument;

                if (arguments.Count > possibleArgumentIndex && arguments[possibleArgumentIndex] == argument)
                    return argument;
                
            }
            
            return null;
        }
        
        protected override void Analyze(IInvocationExpression element, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if(data.GetDaemonProcessKind() != DaemonProcessKind.VISIBLE_DOCUMENT)
                return;

            if (!element.IsResourcesLoadMethod()) 
                return;
            
            var argument = FindArgument(element.Arguments, "path", 0);
                
            var literal = (argument?.Value as ICSharpLiteralExpression)?.ConstantValue.Value as string;
            if (literal == null)
                return;

            if (!myResourceLoadCache.HasResource(literal))
                consumer.AddHighlighting(new UnknownResourceWarning(argument));
        }
    }
}