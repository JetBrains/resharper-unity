using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class MessageHosts
    {
        private readonly List<MessageHost> _classes = new List<MessageHost>();

        public MessageHosts()
        {
            XmlNodeList types = ApiXml.SelectNodes( @"/api/messages/type" );
            if ( types == null ) return;

            foreach ( XmlNode type in types )
            {
                _classes.Add(new MessageHost(type));
            }
        }

        [NotNull]
        public IEnumerable<MessageHost> Classes => _classes;

        [NotNull]
        public static MessageHosts GetInstanceFor([NotNull] IDeclaredElement element)
        {
            return element.GetSolution().GetComponent<MessageHosts>();
        }

        [NotNull]
        public IEnumerable<MessageHost> GetHostsFor([NotNull]ITypeElement type)
        {
            return _classes.Where(c => type.IsDescendantOf(c.GetType(type.Module)));
        }
    }
}