using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Daemon.Stages
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

            return ErrorStripeRequestWithDescription.None(Strings.CgLanguageSpecificDaemonBehaviour_InitialErrorStripe_File_s_primary_language_in_not_Cg);
        }
    }
}