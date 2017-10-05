using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    [Language(typeof(CgLanguage))]
    public class CgLanguageSpecificDaemonBehaviour : LanguageSpecificDaemonBehavior
    {
        public override ErrorStripeRequest InitialErrorStripe(IPsiSourceFile sourceFile)
        {
            return !sourceFile.Properties.ShouldBuildPsi || !sourceFile.Properties.ProvidesCodeModel ||
                   !sourceFile.PrimaryPsiLanguage.Is<CgLanguage>()
                ? ErrorStripeRequest.NONE
                : ErrorStripeRequest.STRIPE_AND_ERRORS;
        }
    }
}