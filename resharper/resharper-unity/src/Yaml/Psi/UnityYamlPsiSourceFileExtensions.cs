using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityYamlPsiSourceFileExtensions
    {
        public static bool IsAsset(this IPsiSourceFile sourceFile)
        {
            return sourceFile is UnityYamlAssetPsiSourceFile || sourceFile.GetLocation().IsInterestingAsset();
        }

        // ReSharper doesn't want us to use project files. See UnityExternalFilesModuleProcessor
        public static bool IsMeta(this IPsiSourceFile sourceFile)
        {
#if RESHARPER
            return sourceFile is UnityYamlAssetPsiSourceFile || sourceFile.GetLocation().IsMeta();
#else
            return sourceFile is UnityYamlExternalPsiSourceFile || sourceFile.GetLocation().IsMeta();
#endif
        }
    }
}