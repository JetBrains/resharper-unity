using System.Collections.Generic;
using System.Xml;
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
    }

    public class UnityApiType
    {
        private readonly string myNs;
        private readonly string myName;
        private readonly string myKind;
        private readonly string myDocPath;
        private readonly IList<UnityApiEventFunction> myEventFunctions;

        public UnityApiType(string ns, string name, string kind, string docPath)
        {
            myNs = ns;
            myName = name;
            myKind = kind;
            myDocPath = docPath;

            myEventFunctions = new List<UnityApiEventFunction>();
        }

        public UnityApiEventFunction AddEventFunction(string name, bool isStatic, string description, string docPath, ApiType returnType)
        {
            var eventFunction = new UnityApiEventFunction(name, isStatic, description, docPath, returnType);
            myEventFunctions.Add(eventFunction);
            return eventFunction;
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("type");
            xmlWriter.WriteAttributeString("kind", myKind);
            xmlWriter.WriteAttributeString("name", myName);
            xmlWriter.WriteAttributeString("ns", myNs);
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
        private readonly string myDescription;
        private readonly string myDocPath;
        private readonly ApiType myReturnType;
        private readonly IList<UnityApiParameter> myParameters;

        public UnityApiEventFunction(string name, bool isStatic, string description, string docPath, ApiType returnType)
        {
            myName = name;
            myIsStatic = isStatic;
            myDescription = description;
            myDocPath = docPath;
            myReturnType = returnType;

            myParameters = new List<UnityApiParameter>();
        }

        public UnityApiParameter AddParameter(string name, ApiType type, string description)
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