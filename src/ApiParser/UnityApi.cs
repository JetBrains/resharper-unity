using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Util;

namespace ApiParser
{
    public abstract class HasVersionRange
    {
        private Version myMinimumVersion = new Version(int.MaxValue, 0);
        private Version myMaximumVersion = new Version(0, 0);

        protected bool UpdateSupportedVersion(Version apiVersion)
        {
            if (apiVersion < myMinimumVersion)
                myMinimumVersion = apiVersion;
            if (apiVersion > myMaximumVersion)
            {
                myMaximumVersion = apiVersion;
                return true;
            }
            return false;
        }

        protected void ExportVersionRange(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteAttributeString("minimumVersion", myMinimumVersion.ToString(2));
            xmlWriter.WriteAttributeString("maximumVersion", myMaximumVersion.ToString(2));
        }
    }

    public class UnityApi : HasVersionRange
    {
        private readonly IList<UnityApiType> myTypes = new List<UnityApiType>();

        public UnityApiType AddType(string ns, string name, string kind, string docPath, Version apiVersion)
        {
            UpdateSupportedVersion(apiVersion);

            foreach (var type in myTypes)
            {
                if (type.Namespace == ns && type.Name == name)
                {
                    // We don't actually use kind, but let's be aware of any issues
                    if (type.Kind != kind)
                    {
                        Console.WriteLine($"WARNING: Kind has changed from `{type.Kind}` to `{kind}` for `{name}`");
                    }

                    return type;
                }
            }

            var unityApiType = new UnityApiType(ns, name, kind, docPath, apiVersion);
            myTypes.Add(unityApiType);
            return unityApiType;
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("api");
            ExportVersionRange(xmlWriter);
            foreach (var type in myTypes.OrderBy(t => t.Name))
                type.ExportTo(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        // TODO: Should this include a version?

        public UnityApiType FindType(string name)
        {
            var type = myTypes.SingleOrDefault(t => t.Name == name);
            if (type == null)
                throw new InvalidOperationException($"Cannot find type {name}");
            return type;
        }
    }

    public class UnityApiType : HasVersionRange
    {
        private readonly string myDocPath;
        private readonly IList<UnityApiEventFunction> myEventFunctions;

        public UnityApiType(string ns, string name, string kind, string docPath, Version apiVersion)
        {
            Namespace = ns;
            Name = name;
            Kind = kind;
            myDocPath = docPath;

            UpdateSupportedVersion(apiVersion);

            myEventFunctions = new List<UnityApiEventFunction>();
        }

        public string Name { get; }
        public string Namespace { get; }
        public string Kind { get; }

        public void MergeEventFunction(UnityApiEventFunction newFunction, Version apiVersion)
        {
            UpdateSupportedVersion(apiVersion);

            var newFunctionSig = newFunction.ToString();
            foreach (var eventFunction in myEventFunctions)
            {
                // If the signature matches, we've already got it, just
                // make sure it's up to date (newer docs take precedence
                // for e.g. param names, description, etc)
                if (eventFunction.ToString() == newFunctionSig)
                {
                    eventFunction.Update(newFunction, apiVersion);
                    return;
                }
            }

            // Not a match. We either haven't found this function before,
            // or a parameter or return type is different
            myEventFunctions.Add(newFunction);
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("type");
            xmlWriter.WriteAttributeString("kind", Kind);
            xmlWriter.WriteAttributeString("name", Name);
            xmlWriter.WriteAttributeString("ns", Namespace);
            ExportVersionRange(xmlWriter);
            xmlWriter.WriteAttributeString("path", myDocPath);
            foreach (var eventFunction in myEventFunctions.OrderBy(f => f.OrderingString))
                eventFunction.ExportTo(xmlWriter);
            xmlWriter.WriteEndElement();
        }
    }

    public class UnityApiEventFunction : HasVersionRange
    {
        private readonly bool myIsStatic;
        [CanBeNull] private string myDescription;
        [CanBeNull] private readonly string myDocPath;
        private readonly bool myUndocumented;
        private readonly ApiType myReturnType;
        private readonly IList<UnityApiParameter> myParameters;

        public UnityApiEventFunction(string name, bool isStatic, ApiType returnType, Version apiVersion,
            string description = null, string docPath = null, bool undocumented = false)
        {
            Name = name;
            myIsStatic = isStatic;
            myDescription = description;
            myDocPath = docPath;
            myUndocumented = undocumented;
            myReturnType = returnType;

            UpdateSupportedVersion(apiVersion);

            myParameters = new List<UnityApiParameter>();
        }

        public string Name { get; }
        public object OrderingString => (myUndocumented ? "zz" : string.Empty) + Name;

        public UnityApiParameter AddParameter(string name, ApiType type, string description = null)
        {
            var parmaeter = new UnityApiParameter(name, type, description);
            myParameters.Add(parmaeter);
            return parmaeter;
        }

        public void Update(UnityApiEventFunction function, Version apiVersion)
        {
            if (UpdateSupportedVersion(apiVersion))
            {
                if (function.myDescription != myDescription && !string.IsNullOrEmpty(function.myDescription))
                {
                    myDescription = function.myDescription;
                }

                for (var i = 0; i < myParameters.Count; i++)
                {
                    myParameters[i].Update(function.myParameters[i]);
                }
            }
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("message");
            xmlWriter.WriteAttributeString("name", Name);
            xmlWriter.WriteAttributeString("static", myIsStatic.ToString());
            ExportVersionRange(xmlWriter);
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

        public override string ToString()
        {
            var parameters = string.Join(", ", myParameters.Select(p => p.ToString()));
            return $"{myReturnType} {Name}({parameters})";
        }
    }

    public class UnityApiParameter
    {
        private string myName;
        private readonly ApiType myType;
        private string myDescription;

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

        public void Update(UnityApiParameter newParameter)
        {
            if (myName != newParameter.myName && !string.IsNullOrEmpty(newParameter.myName))
            {
                myName = newParameter.myName;
            }

            if (myDescription != newParameter.myDescription && !string.IsNullOrEmpty(newParameter.myDescription))
            {
                myDescription = newParameter.myDescription;
            }

            if (myType.FullName != newParameter.myType.FullName)
                throw new InvalidOperationException($"Parameter type differences for parameter {myName}! {myType.FullName} {newParameter.myType.FullName}");
        }

        public override string ToString()
        {
            // Don't include name, that's not important
            return $"{myType}";
        }
    }
}