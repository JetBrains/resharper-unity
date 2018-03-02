using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;

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

        protected void ExportVersionRange(XmlTextWriter xmlWriter, HasVersionRange defaults)
        {
            if (myMinimumVersion > defaults.myMinimumVersion)
                xmlWriter.WriteAttributeString("minimumVersion", myMinimumVersion.ToString(2));
            if (myMaximumVersion < defaults.myMaximumVersion)
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
                type.ExportTo(xmlWriter, this);
            xmlWriter.WriteEndElement();
        }

        // TODO: Should this include a version?

        public UnityApiType FindType(string name)
        {
            var type = myTypes.SingleOrDefault(t => t.Name == name);
            if (type == null)
            {
                Console.WriteLine($"Cannot find type {name}");
                return null;
            }
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

        public IEnumerable<UnityApiEventFunction> FindEventFunctions(string name)
        {
            return myEventFunctions.Where(f => f.Name == name);
        }

        public void ExportTo(XmlTextWriter xmlWriter, HasVersionRange defaultVersions)
        {
            xmlWriter.WriteStartElement("type");
            xmlWriter.WriteAttributeString("kind", Kind);
            xmlWriter.WriteAttributeString("name", Name);
            xmlWriter.WriteAttributeString("ns", Namespace);
            ExportVersionRange(xmlWriter, defaultVersions);
            xmlWriter.WriteAttributeString("path", myDocPath.Replace(@"\", "/"));
            foreach (var eventFunction in myEventFunctions.OrderBy(f => f.OrderingString))
                eventFunction.ExportTo(xmlWriter, defaultVersions);
            xmlWriter.WriteEndElement();
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Namespace))
                return Name;
            return Namespace + "." + Name;
        }
    }

    public class UnityApiEventFunction : HasVersionRange
    {
        private bool myIsStatic;
        private bool myIsCoroutine;
        [CanBeNull] private string myDescription;
        [CanBeNull] private readonly string myDocPath;
        private readonly bool myUndocumented;
        private ApiType myReturnType;
        private readonly IList<UnityApiParameter> myParameters;

        public UnityApiEventFunction(string name, bool isStatic, bool isCoroutine, ApiType returnType,
            Version apiVersion, string description = null, string docPath = null, bool undocumented = false)
        {
            Name = name;
            myIsStatic = isStatic;
            myIsCoroutine = isCoroutine;
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
                myIsStatic = function.myIsStatic;

                if (function.myDescription != myDescription && !string.IsNullOrEmpty(function.myDescription))
                {
                    myDescription = function.myDescription;
                }

                for (var i = 0; i < myParameters.Count; i++)
                {
                    myParameters[i].Update(function.myParameters[i], function.Name);
                }
            }
        }

        public void SetIsCoroutine()
        {
            myIsCoroutine = true;
        }

        public void SetIsStatic()
        {
            myIsStatic = true;
        }

        public void SetReturnType(ApiType returnType)
        {
            myReturnType = returnType;
        }

        public void MakeParameterOptional(string name, string justification)
        {
            if (myParameters.Count != 1)
                throw new InvalidOperationException("Cannot handle multiple optional parameters");
            if (myParameters[0].Name != name)
                throw new InvalidOperationException($"Cannot find parameter {name}");
            myParameters[0].SetOptional(justification);
        }

        public void UpdateParameter(string name, UnityApiParameter newParameter)
        {
            var parameter = GetParameter(name);
            parameter.Update(newParameter, Name);
        }

        private UnityApiParameter GetParameter(string name)
        {
            return myParameters.Single(p => p.Name == name);
        }

        public void ExportTo(XmlTextWriter xmlWriter, HasVersionRange defaultVersions)
        {
            xmlWriter.WriteStartElement("message");
            xmlWriter.WriteAttributeString("name", Name);
            xmlWriter.WriteAttributeString("static", myIsStatic.ToString());
            if (myIsCoroutine)
                xmlWriter.WriteAttributeString("coroutine", "True");
            ExportVersionRange(xmlWriter, defaultVersions);
            if (myUndocumented)
                xmlWriter.WriteAttributeString("undocumented", "True");
            if (!string.IsNullOrEmpty(myDescription))
                xmlWriter.WriteAttributeString("description", myDescription);
            if (!string.IsNullOrEmpty(myDocPath))
                xmlWriter.WriteAttributeString("path", myDocPath.Replace(@"\", "/"));
            WriteParameters(xmlWriter);
            WriteReturns(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        private void WriteParameters(XmlTextWriter xmlWriter)
        {
            if (!myParameters.Any())
                return;

            xmlWriter.WriteStartElement("parameters");
            foreach (var parameter in myParameters)
                parameter.ExportTo(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        private void WriteReturns(XmlWriter xmlWriter)
        {
            if (myReturnType.IsByRef)
                throw new InvalidOperationException("Cannot have ref return type");

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
        private ApiType myType;
        private string myDescription;
        private string myJustification;

        public UnityApiParameter(string name, ApiType type, string description)
        {
            Name = name;
            myType = type;
            myDescription = description;
            myJustification = string.Empty;
        }

        public string Name { get; private set; }

        public void SetOptional(string justification)
        {
            myJustification = justification;
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("parameter");
            xmlWriter.WriteAttributeString("type", myType.FullName);
            xmlWriter.WriteAttributeString("array", myType.IsArray.ToString());
            if (myType.IsByRef)
                xmlWriter.WriteAttributeString("byRef", myType.IsByRef.ToString());
            xmlWriter.WriteAttributeString("name", Name);
            if (!string.IsNullOrEmpty(myJustification))
            {
                xmlWriter.WriteAttributeString("optional", "True");
                xmlWriter.WriteAttributeString("justification", myJustification);
            }
            if (!string.IsNullOrEmpty(myDescription))
                xmlWriter.WriteAttributeString("description", myDescription);
            xmlWriter.WriteEndElement();
        }

        public void Update(UnityApiParameter newParameter, string functionName)
        {
            if (Name != newParameter.Name && !string.IsNullOrEmpty(newParameter.Name))
            {
                Name = newParameter.Name;
            }

            if (myDescription != newParameter.myDescription && !string.IsNullOrEmpty(newParameter.myDescription))
            {
                myDescription = newParameter.myDescription;
            }

            if (myType.FullName != newParameter.myType.FullName)
                throw new InvalidOperationException($"Parameter type differences for parameter {Name}! {myType.FullName} {newParameter.myType.FullName}");

            if (myType.IsArray != newParameter.myType.IsArray || myType.IsByRef != newParameter.myType.IsByRef)
            {
                Console.WriteLine("WARNING: Parameter `{2}` of function `{3}` type changed: was {0} now {1}", myType, newParameter.myType, Name, functionName);
                myType = newParameter.myType;
            }
        }

        public override string ToString()
        {
            // Don't include name, that's not important
            return $"{myType}";
        }
    }
}