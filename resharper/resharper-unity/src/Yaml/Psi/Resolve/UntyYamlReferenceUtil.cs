using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public static class UnityYamlReferenceUtil
    {
        // If we add any more references, add them here, or SWEA's usage count won't pick them up!!
        public static bool CanContainReference([NotNull] IYamlDocument document)
        {
            var buffer = document.GetTextAsBuffer();
            return UnityEventTargetReferenceFactory.CanContainReference(buffer)
                || MonoScriptReferenceFactory.CanContainReference(buffer);
        }
    }
}
