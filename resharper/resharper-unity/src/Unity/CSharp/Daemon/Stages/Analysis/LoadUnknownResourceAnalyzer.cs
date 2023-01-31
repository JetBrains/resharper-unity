#nullable enable

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
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

        private static ICSharpArgument? FindArgument(TreeNodeCollection<ICSharpArgument> arguments, string argumentName, int possibleArgumentIndex = 0)
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
            if (!element.IsResourcesLoadMethod())
                return;

            var argument = FindArgument(element.Arguments, "path");

            if (argument?.Value is ICSharpLiteralExpression literalExpression &&
                literalExpression.ConstantValue.IsNotNullString(out var literal))
            {
                var psiSourceFile = element.GetSourceFile();
                if (psiSourceFile == null)
                    return;

                var dependencyStore = psiSourceFile.GetPsiServices().DependencyStore;
                if (dependencyStore.HasDependencySet)
                    dependencyStore.AddDependency(ResourceLoadCache.CreateDependency(psiSourceFile, literal));

                if (!myResourceLoadCache.HasResource(literal))
                    consumer.AddHighlighting(new UnknownResourceWarning(argument, literal));
            }
        }
    }
}
