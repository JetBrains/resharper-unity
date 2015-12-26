using JetBrains.Annotations;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class Extensions
    {
        public static bool HasAttribute([NotNull] this IAttributesSet set, [NotNull] string attribute)
        {
            return set.HasAttributeInstance(new ClrTypeName(attribute), true);
        }
    }
}