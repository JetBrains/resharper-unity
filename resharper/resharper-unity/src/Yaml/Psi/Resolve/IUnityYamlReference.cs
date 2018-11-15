using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public interface IUnityYamlReference : IReference
    {
        IYamlDocument ComponentDocument { get; }
    }
}