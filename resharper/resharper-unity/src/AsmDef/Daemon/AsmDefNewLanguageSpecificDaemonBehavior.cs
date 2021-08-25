using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon
{
    [Language(typeof(JsonNewLanguage))]
    public class AsmDefNewLanguageSpecificDaemonBehavior: ILanguageSpecificDaemonBehavior
    {
        public ErrorStripeRequestWithDescription InitialErrorStripe(IPsiSourceFile sourceFile)
        {
            if (sourceFile.PrimaryPsiLanguage.Is<JsonNewLanguage>())
            {
                var properties = sourceFile.Properties;
                if (!properties.ShouldBuildPsi) return ErrorStripeRequestWithDescription.CreateNoneNoPsi(properties);
                if (!properties.ProvidesCodeModel) return ErrorStripeRequestWithDescription.CreateNoneNoCodeModel(properties);
                return ErrorStripeRequestWithDescription.StripeAndErrors;
            }

            return ErrorStripeRequestWithDescription.None("File's primary language in not Json");
        }

        public bool CanShowErrorBox => true;
        public bool RunInSolutionAnalysis => false;
        public bool RunInFindCodeIssues => true;
    }
}