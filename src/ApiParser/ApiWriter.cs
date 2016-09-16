using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;

namespace ApiParser
{
    public class ApiWriter
    {
        private readonly XmlDocument _dom = new XmlDocument();
        private readonly Stack<XmlElement> _elements = new Stack<XmlElement>();

        public ApiWriter()
        {
            XmlElement element = _dom.CreateElement("api");
            _dom.AppendChild(element);
            _elements.Push(element);
        }

        public void Enter([NotNull] string name)
        {
            Leave(name);

            XmlElement element = _dom.CreateElement(name);
            _elements.Peek().AppendChild(element);
            _elements.Push(element);
        }

        public void Leave([NotNull] string name)
        {
            while (_elements.Any(e => e.Name == name)) _elements.Pop();
        }

        public void LeaveTo([NotNull] string name)
        {
            while (_elements.Any(e => e.Name == name) && _elements.Peek().Name != name) _elements.Pop();
        }

        public void SetAttribute<T>([NotNull] string name, [NotNull] T value)
        {
            _elements.Peek().SetAttribute(name, value.ToString());
        }

        public void SetDescription([NotNull] string text)
        {
            XmlElement description = _dom.CreateElement("description");
            description.AppendChild(_dom.CreateTextNode(text));
            _elements.Peek().AppendChild(description);
        }

        public void WriteTo([NotNull] XmlWriter writer)
        {
            _dom.WriteTo(writer);
        }
    }
}
