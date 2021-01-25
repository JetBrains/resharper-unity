using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Daemon
{
    [Language(typeof(JsonNewLanguage))]
    public class AsmDefNewLanguageSpecificDaemonBehavior: ILanguageSpecificDaemonBehavior
    {
        public ErrorStripeRequest InitialErrorStripe(IPsiSourceFile sourceFile)
        {
            return !sourceFile.Properties.ShouldBuildPsi || !sourceFile.Properties.ProvidesCodeModel ||
                   !sourceFile.PrimaryPsiLanguage.Is<JsonNewLanguage>()
                ? ErrorStripeRequest.NONE
                : ErrorStripeRequest.STRIPE_AND_ERRORS;
        }

        public bool CanShowErrorBox => true;
        public bool RunInSolutionAnalysis => false;
        public bool RunInFindCodeIssues => true;
    }
}