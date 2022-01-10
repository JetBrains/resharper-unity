using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    [Language(typeof(CgLanguage))]
    public class CgLanguageSpecificDaemonBehaviour : LanguageSpecificDaemonBehavior
    {
        public override ErrorStripeRequestWithDescription InitialErrorStripe(IPsiSourceFile sourceFile)
        {
            if (sourceFile.PrimaryPsiLanguage.Is<CgLanguage>())
            {
                var properties = sourceFile.Properties;
                if (!properties.ShouldBuildPsi) return ErrorStripeRequestWithDescription.CreateNoneNoPsi(properties);
                if (!properties.ProvidesCodeModel) return ErrorStripeRequestWithDescription.CreateNoneNoCodeModel(properties);
                return ErrorStripeRequestWithDescription.StripeAndErrors;
            }

            return ErrorStripeRequestWithDescription.None("File's primary language in not Cg");
        }
    }
}