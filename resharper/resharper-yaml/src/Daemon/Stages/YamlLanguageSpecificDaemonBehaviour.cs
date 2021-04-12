using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.Stages
{
  [Language(typeof(YamlLanguage))]
  public class YamlLanguageSpecificDaemonBehaviour : LanguageSpecificDaemonBehavior
  {
    public override ErrorStripeRequestWithDescription InitialErrorStripe(IPsiSourceFile sourceFile)
    {
      
      if (sourceFile.PrimaryPsiLanguage.Is<YamlLanguage>())
      {
        var properties = sourceFile.Properties;
        if (!properties.ShouldBuildPsi) return ErrorStripeRequestWithDescription.CreateNoneNoPsi(properties);
        if (!properties.ProvidesCodeModel) return ErrorStripeRequestWithDescription.CreateNoneNoCodeModel(properties);
        return ErrorStripeRequestWithDescription.StripeAndErrors;
      }

      return ErrorStripeRequestWithDescription.None("File's primary language in not Yaml");
    }
  }
}