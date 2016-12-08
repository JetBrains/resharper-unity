using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Util;

namespace ApiParser
{
    public class UnityApi
    {
        private readonly IList<UnityApiType> types = new List<UnityApiType>();

        public UnityApiType AddType(string ns, string name, string kind, string docPath)
        {
            var unityApiType = new UnityApiType(ns, name, kind, docPath);
            types.Add(unityApiType);
            return unityApiType;
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("api");
            foreach (var type in types)
                type.ExportTo(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        public UnityApiType FindType(string name)
        {
            var type = types.SingleOrDefault(t => t.Name == name);
            if (type == null)
                throw new InvalidOperationException($"Cannot find type {name}");
            return type;
        }
    }

    public class UnityApiType
    {
        private readonly string myKind;
        private readonly string myDocPath;
        private readonly IList<UnityApiEventFunction> myEventFunctions;

        public UnityApiType(string ns, string name, string kind, string docPath)
        {
            Namespace = ns;
            Name = name;
            myKind = kind;
            myDocPath = docPath;

            myEventFunctions = new List<UnityApiEventFunction>();
        }

        public string Name { get; }
        public string Namespace { get; }

        public UnityApiEventFunction AddEventFunction(string name, bool isStatic, ApiType returnType, string docPath = null, string description = null, bool undocumented = false)
        {
            var eventFunction = new UnityApiEventFunction(name, isStatic, returnType, description, docPath, undocumented);
            myEventFunctions.Add(eventFunction);
            return eventFunction;
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("type");
            xmlWriter.WriteAttributeString("kind", myKind);
            xmlWriter.WriteAttributeString("name", Name);
            xmlWriter.WriteAttributeString("ns", Namespace);
            xmlWriter.WriteAttributeString("path", myDocPath);
            foreach (var eventFunction in myEventFunctions)
                eventFunction.ExportTo(xmlWriter);
            xmlWriter.WriteEndElement();
        }
    }

    public class UnityApiEventFunction
    {
        private readonly string myName;
        private readonly bool myIsStatic;
        [CanBeNull] private readonly string myDescription;
        [CanBeNull] private readonly string myDocPath;
        private readonly bool myUndocumented;
        private readonly ApiType myReturnType;
        private readonly IList<UnityApiParameter> myParameters;

        public UnityApiEventFunction(string name, bool isStatic, ApiType returnType, [CanBeNull] string description,
            [CanBeNull] string docPath, bool undocumented)
        {
            myName = name;
            myIsStatic = isStatic;
            myDescription = description;
            myDocPath = docPath;
            myUndocumented = undocumented;
            myReturnType = returnType;

            myParameters = new List<UnityApiParameter>();
        }

        public UnityApiParameter AddParameter(string name, ApiType type, string description = null)
        {
            var parmaeter = new UnityApiParameter(name, type, description);
            myParameters.Add(parmaeter);
            return parmaeter;
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("message");
            xmlWriter.WriteAttributeString("name", myName);
            xmlWriter.WriteAttributeString("static", myIsStatic.ToString());
            if (myUndocumented)
                xmlWriter.WriteAttributeString("undocumented", "True");
            if (!string.IsNullOrEmpty(myDescription))
                xmlWriter.WriteAttributeString("description", myDescription);
            if (!string.IsNullOrEmpty(myDocPath))
                xmlWriter.WriteAttributeString("path", myDocPath);
            WriteParameters(xmlWriter);
            WriteReturns(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        private void WriteParameters(XmlTextWriter xmlWriter)
        {
            if (myParameters.IsEmpty())
                return;

            xmlWriter.WriteStartElement("parameters");
            foreach (var parameter in myParameters)
                parameter.ExportTo(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        private void WriteReturns(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("returns");
            xmlWriter.WriteAttributeString("type", myReturnType.FullName);
            xmlWriter.WriteAttributeString("array", myReturnType.IsArray.ToString());
            xmlWriter.WriteEndElement();
        }
    }

    public class UnityApiParameter
    {
        private readonly string myName;
        private readonly ApiType myType;
        private readonly string myDescription;

        public UnityApiParameter(string name, ApiType type, string description)
        {
            myName = name;
            myType = type;
            myDescription = description;
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("parameter");
            xmlWriter.WriteAttributeString("type", myType.FullName);
            xmlWriter.WriteAttributeString("array", myType.IsArray.ToString());
            xmlWriter.WriteAttributeString("name", myName);
            if (!string.IsNullOrEmpty(myDescription))
                xmlWriter.WriteAttributeString("description", myDescription);
            xmlWriter.WriteEndElement();
        }
    }
}