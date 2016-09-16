using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class Extensions
    {
        [NotNull]
        public static IEnumerable<MessageHost> GetMessageHosts([NotNull] this ITypeElement type)
        {
            MessageHosts allHosts = MessageHosts.GetInstanceFor(type);
            return allHosts.GetHostsFor(type);
        }

        public static bool HasAttribute( [NotNull] this IAttributesSet set, [NotNull] string attribute )
        {
            return set.HasAttributeInstance( new ClrTypeName( attribute ), true );
        }

        public static bool IsMessage([NotNull] this IMethod method)
        {
            MessageHost[] hosts = method.GetContainingType()?.GetMessageHosts().ToArray();
            return hosts != null && hosts.Any(h => h.Contains(method));
        }

        public static bool IsMessageHost([NotNull] this ITypeElement element)
        {
            return element.GetMessageHosts().Any();
        }
    }
}