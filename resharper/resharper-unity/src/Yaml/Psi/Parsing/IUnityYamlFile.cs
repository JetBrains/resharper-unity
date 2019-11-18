using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing
{
    public interface IUnityYamlFile : IYamlFile
    {
        IEnumerable<ITreeNode> ComponentDocuments { get; }
    }
}