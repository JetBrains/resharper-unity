using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.Stages
{
  [Language(typeof(YamlLanguage))]
  public class YamlLanguageSpecificDaemonBehaviour : LanguageSpecificDaemonBehavior
  {
    public override ErrorStripeRequest InitialErrorStripe(IPsiSourceFile sourceFile)
    {
      return !sourceFile.Properties.ShouldBuildPsi || !sourceFile.Properties.ProvidesCodeModel
             || !sourceFile.PrimaryPsiLanguage.Is<YamlLanguage>()
        ? ErrorStripeRequest.NONE
        : ErrorStripeRequest.STRIPE_AND_ERRORS;
    }
  }
}