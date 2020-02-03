using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public interface IUnityAssetDataElementBuilder
    {
        IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, IBuffer currentYamlDocumentBuffer);
    }
}