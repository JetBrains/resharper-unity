using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabLanguageSpecificDaemonBehaviour : ILanguageSpecificDaemonBehavior
    {
        public ErrorStripeRequestWithDescription InitialErrorStripe(IPsiSourceFile sourceFile)
        {
            if (sourceFile.PrimaryPsiLanguage.Is<ShaderLabLanguage>())
            {
                var properties = sourceFile.Properties;
                if (!properties.ShouldBuildPsi) return ErrorStripeRequestWithDescription.CreateNoneNoPsi(properties);
                if (!properties.ProvidesCodeModel) return ErrorStripeRequestWithDescription.CreateNoneNoCodeModel(properties);
                return ErrorStripeRequestWithDescription.StripeAndErrors;
            }

            return ErrorStripeRequestWithDescription.None("File's primary language in not ShaderLab");
        }

        public bool CanShowErrorBox => true;
        public bool RunInSolutionAnalysis => false;
        public bool RunInFindCodeIssues => true;
    }
}