using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.UnityAsset
{
  internal class UAFile : YamlFile
  {
    public override PsiLanguageType Language => UALanguage.Instance;
  }
}