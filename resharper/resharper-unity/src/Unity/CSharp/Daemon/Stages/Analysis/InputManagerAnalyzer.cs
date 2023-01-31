#nullable enable

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new []
    {
        typeof(UnknownInputAxesWarning)
    })]
    public class InputManagerAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private readonly UnityProjectSettingsCache myProjectSettingsCache;

        public InputManagerAnalyzer(UnityApi unityApi, UnityProjectSettingsCache unityProjectSettingsCache)
            : base(unityApi)
        {
            myProjectSettingsCache = unityProjectSettingsCache;
        }

        protected override void Analyze(IInvocationExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!myProjectSettingsCache.IsAvailable())
                return;

            if (element.IsInputAxisMethod() || element.IsInputButtonMethod())
            {
                // TODO: Use conditional access when the monorepo build uses a more modern C# compiler
                // Currently (as of 01/2023) the monorepo build for Unity uses C#9 compiler, which will complain that
                // the out variable is uninitialised when we use conditional access
                // See also https://youtrack.jetbrains.com/issue/RSRP-489147
                var argument = element.ArgumentList.Arguments.FirstOrDefault();
                if (argument?.Value != null && argument.Value.ConstantValue.IsNotNullString(out var literal) &&
                    !myProjectSettingsCache.HasInput(literal))
                {
                    consumer.AddHighlighting(new UnknownInputAxesWarning(argument));
                }
            }
        }
    }
}
