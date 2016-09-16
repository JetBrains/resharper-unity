using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;

namespace ApiParser
{
    public class ApiWriter
    {
        private readonly XmlDocument doc = new XmlDocument();
        private readonly Stack<XmlElement> elements = new Stack<XmlElement>();

        public ApiWriter()
        {
            var element = doc.CreateElement("api");
            doc.AppendChild(element);
            elements.Push(element);
        }

        public void Enter([NotNull] string name)
        {
            Leave(name);

            var element = doc.CreateElement(name);
            elements.Peek().AppendChild(element);
            elements.Push(element);
        }

        public void Leave([NotNull] string name)
        {
            while (elements.Any(e => e.Name == name)) elements.Pop();
        }

        public void LeaveTo([NotNull] string name)
        {
            while (elements.Any(e => e.Name == name) && elements.Peek().Name != name) elements.Pop();
        }

        public void SetAttribute<T>([NotNull] string name, [NotNull] T value)
        {
            elements.Peek().SetAttribute(name, value.ToString());
        }

        public void SetDescription([NotNull] string text)
        {
            var description = doc.CreateElement("description");
            description.AppendChild(doc.CreateTextNode(text));
            elements.Peek().AppendChild(description);
        }

        public void WriteTo([NotNull] XmlWriter writer)
        {
            doc.WriteTo(writer);
        }
    }
}
