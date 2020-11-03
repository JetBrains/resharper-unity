using JetBrains.Annotations;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    public interface IAnimatorScriptUsage : IScriptUsage
    {
        [NotNull]
        string Name { get; }
        
        void WriteTo([NotNull] UnsafeWriter writer);
    }
}