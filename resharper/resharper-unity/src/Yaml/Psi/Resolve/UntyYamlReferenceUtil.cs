using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public static class UnityYamlReferenceUtil
    {
        // If we add any more references, add them here, or SWEA's usage count won't pick them up!!
        public static bool CanContainReference([NotNull] IBuffer buffer)
        {
            return UnityEventTargetReferenceFactory.CanContainReference(buffer)
                || MonoScriptReferenceFactory.CanContainReference(buffer);
        }
    }
}
