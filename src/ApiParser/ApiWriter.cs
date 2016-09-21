using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;

namespace ApiParser
{
    public class ApiWriter
    {
        private readonly XmlDocument myDoc = new XmlDocument();
        private readonly Stack<XmlElement> myElements = new Stack<XmlElement>();

        public ApiWriter()
        {
            var element = myDoc.CreateElement("api");
            myDoc.AppendChild(element);
            myElements.Push(element);
        }

        public void Enter([NotNull] string name)
        {
            Leave(name);

            var element = myDoc.CreateElement(name);
            myElements.Peek().AppendChild(element);
            myElements.Push(element);
        }

        public void Leave([NotNull] string name)
        {
            while (myElements.Any(e => e.Name == name)) myElements.Pop();
        }

        public void LeaveTo([NotNull] string name)
        {
            while (myElements.Any(e => e.Name == name) && myElements.Peek().Name != name) myElements.Pop();
        }

        public void SetAttribute<T>([NotNull] string name, [NotNull] T value)
        {
            myElements.Peek().SetAttribute(name, value.ToString());
        }

        public void WriteTo([NotNull] XmlWriter writer)
        {
            myDoc.WriteTo(writer);
        }
    }
}
