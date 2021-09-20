using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityYamlPsiSourceFileExtensions
    {
        public static bool IsAsset(this IPsiSourceFile sourceFile)
        {
            return sourceFile.GetLocation().IsInterestingAsset();
        }

        // ReSharper doesn't want us to use project files. See UnityExternalFilesModuleProcessor
        public static bool IsMeta(this IPsiSourceFile sourceFile)
        {
            return sourceFile.GetLocation().IsMeta();
        }
    }
}