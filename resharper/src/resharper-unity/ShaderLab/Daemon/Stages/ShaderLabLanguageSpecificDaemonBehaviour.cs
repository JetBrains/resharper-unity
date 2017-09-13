using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabLanguageSpecificDaemonBehaviour : LanguageSpecificDaemonBehavior
    {
        public override ErrorStripeRequest InitialErrorStripe(IPsiSourceFile sourceFile)
        {
            return !sourceFile.Properties.ShouldBuildPsi || !sourceFile.Properties.ProvidesCodeModel ||
                   !sourceFile.PrimaryPsiLanguage.Is<ShaderLabLanguage>()
                ? ErrorStripeRequest.NONE
                : ErrorStripeRequest.STRIPE_AND_ERRORS;
        }
    }
}