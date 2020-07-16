using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using JetBrains.Annotations;
using JetBrains.Util;

namespace ApiParser
{
    public abstract class HasVersionRange
    {
        public Version MinimumVersion { get; private set; } = new Version(int.MaxValue, 0);
        public Version MaximumVersion { get; private set; } = new Version(0, 0);

        public void UpdateSupportedVersion(Version apiVersion)
        {
            if (apiVersion < MinimumVersion)
                MinimumVersion = apiVersion;
            if (apiVersion > MaximumVersion)
                MaximumVersion = apiVersion;
        }

        protected bool IsSupportedVersion(Version apiVersion)
        {
            return MinimumVersion <= apiVersion && apiVersion <= MaximumVersion;
        }

        protected void ExportVersionRange(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteAttributeString("minimumVersion", MinimumVersion.ToString(2));
            xmlWriter.WriteAttributeString("maximumVersion", MaximumVersion.ToString(2));
        }

        protected void ExportVersionRange(XmlTextWriter xmlWriter, HasVersionRange defaults)
        {
            if (MinimumVersion > defaults.MinimumVersion)
                xmlWriter.WriteAttributeString("minimumVersion", MinimumVersion.ToString(2));
            if (MaximumVersion < defaults.MaximumVersion)
                xmlWriter.WriteAttributeString("maximumVersion", MaximumVersion.ToString(2));
        }

        protected void ImportVersionRange(XElement element, HasVersionRange defaults)
        {
            var attr = element.Attribute("minimumVersion");
            MinimumVersion = attr != null ? Version.Parse(attr.Value) : defaults.MinimumVersion;
            attr = element.Attribute("maximumVersion");
            MaximumVersion = attr != null ? Version.Parse(attr.Value) : defaults.MaximumVersion;
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
            xmlWriter.WriteComment("This file is auto-generated");
            xmlWriter.WriteStartElement("api");
            ExportVersionRange(xmlWriter);
            foreach (var type in myTypes.OrderBy(t => t.Name))
                type.ExportTo(xmlWriter, this);
            xmlWriter.WriteEndElement();
        }

        public static UnityApi ImportFrom(FileSystemPath apiXml)
        {
            var doc = XDocument.Load(apiXml.FullPath);
            var apiNode = doc.Root;
            if (apiNode?.Name != "api")
                throw new InvalidDataException("Cannot find root api node");
            var api = new UnityApi();
            api.ImportVersionRange(apiNode, null);
            foreach (var typeElement in apiNode.Descendants("type"))
                api.myTypes.Add(UnityApiType.ImportFrom(typeElement, api));
            return api;
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

        private void TrimDuplicates()
        {
            var distinctFunctions = new List<UnityApiEventFunction>();
            var groupedFunctions = myEventFunctions.GroupBy(f => f.ToString());
            foreach (var group in groupedFunctions)
            {
                var minVersion = new Version(int.MaxValue, int.MaxValue);
                var maxVersion = new Version(0, 0);

                foreach (var function in group)
                {
                    if (function.MinimumVersion < minVersion)
                        minVersion = function.MinimumVersion;
                    if (function.MaximumVersion > maxVersion)
                        maxVersion = function.MaximumVersion;
                }

                var distinctFunction = group.First();
                distinctFunction.UpdateSupportedVersion(minVersion);
                distinctFunction.UpdateSupportedVersion(maxVersion);
                distinctFunctions.Add(distinctFunction);
            }
            myEventFunctions.Clear();
            myEventFunctions.AddRange(distinctFunctions);
        }

        public void ExportTo(XmlTextWriter xmlWriter, HasVersionRange defaultVersions)
        {
            // We can get duplicates when merging fixes. E.g. We read a fixed version from an existing api.xml
            // then read in a broken version from the actual documentation, and fix that broken one up, and there's
            // a duplicate
            TrimDuplicates();

            xmlWriter.WriteStartElement("type");
            xmlWriter.WriteAttributeString("kind", Kind);
            xmlWriter.WriteAttributeString("name", Name);
            xmlWriter.WriteAttributeString("ns", Namespace);
            ExportVersionRange(xmlWriter, defaultVersions);
            xmlWriter.WriteAttributeString("path", myDocPath.Replace(@"\", "/"));
            foreach (var eventFunction in myEventFunctions.OrderBy(f => f.OrderingString))
                eventFunction.ExportTo(xmlWriter, this);
            xmlWriter.WriteEndElement();
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Namespace))
                return Name;
            return Namespace + "." + Name;
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static UnityApiType ImportFrom(XElement element, HasVersionRange apiVersions)
        {
            var ns = element.Attribute("ns").Value;
            var name = element.Attribute("name").Value;
            var kind = element.Attribute("kind").Value;
            var path = element.Attribute("path").Value;
            var type = new UnityApiType(ns, name, kind, path, new Version(0, 0));
            type.ImportVersionRange(element, apiVersions);
            foreach (var message in element.Descendants("message"))
                type.myEventFunctions.Add(UnityApiEventFunction.ImportFrom(message, type));
            return type;
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
            if (myDescription?.StartsWith(":ref::") == true) // Yes, really. MonoBehaviour.OnCollisionStay in 2018.1
                myDescription = myDescription.Substring(6);
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
            var parameter = new UnityApiParameter(name, type, description);
            myParameters.Add(parameter);
            return parameter;
        }

        public void Update(UnityApiEventFunction function, Version apiVersion)
        {
            UpdateSupportedVersion(apiVersion);

            if (IsSupportedVersion(apiVersion))
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

        public void SetIsInstance()
        {
            myIsStatic = false;
        }

        public void SetReturnType(ApiType returnType)
        {
            myReturnType = returnType;
        }

        public void MakeParameterOptional(string name, string justification)
        {
            if (myParameters.Count != 1)
                throw new InvalidOperationException("Cannot handle multiple optional parameters");
            if (myParameters[0].Name == name)
                myParameters[0].SetOptional(justification);
        }

        public void UpdateParameter(string name, UnityApiParameter newParameter)
        {
            var parameter = myParameters.SingleOrDefault(p => p.Name == name);
            if (parameter == null)
                parameter = myParameters.SingleOrDefault(p => p.Name == newParameter.Name);
            if (parameter == null)
                throw new InvalidOperationException($"Cannot update parameter {name}");
            parameter.Update(newParameter, Name);
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
            if (!Enumerable.Any(myParameters))
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
            var parameters = string.Join(", ", myParameters.Select(p => p.Type.ToString()));
            return $"{myReturnType} {Name}({parameters})";
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static UnityApiEventFunction ImportFrom(XElement message, HasVersionRange versions)
        {
            var name = message.Attribute("name").Value;
            var isStatic = bool.Parse(message.Attribute("static").Value);
            var coroutineAttribute = message.Attribute("coroutine");
            var isCoroutine = coroutineAttribute != null && bool.Parse(coroutineAttribute.Value);
            var description = message.Attribute("description")?.Value;
            var path = message.Attribute("path")?.Value;
            var undocumentedAttribute = message.Attribute("undocumented");
            var isUndocumented = undocumentedAttribute != null && bool.Parse(undocumentedAttribute.Value);
            var returns = message.Descendants("returns").First();
            var type = returns.Attribute("type").Value;
            var isArray = bool.Parse(returns.Attribute("array").Value);
            var returnType = new ApiType(type + (isArray ? "[]" : string.Empty));
            var function = new UnityApiEventFunction(name, isStatic, isCoroutine, returnType, new Version(int.MaxValue, 0), description,
                path, isUndocumented);
            function.ImportVersionRange(message, versions);
            foreach (var parameter in message.Descendants("parameter"))
                function.myParameters.Add(UnityApiParameter.ImportFrom(parameter));
            return function;
        }
    }

    public class UnityApiParameter
    {
        private string myDescription;
        private string myJustification;

        public UnityApiParameter(string name, ApiType type, string description)
        {
            Name = name;
            Type = type;
            myDescription = description;
            myJustification = string.Empty;
        }

        public string Name { get; private set; }
        public ApiType Type { get; private set; }

        public void SetOptional(string justification)
        {
            myJustification = justification;
        }

        public void ExportTo(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("parameter");
            xmlWriter.WriteAttributeString("type", Type.FullName);
            xmlWriter.WriteAttributeString("array", Type.IsArray.ToString());
            if (Type.IsByRef)
                xmlWriter.WriteAttributeString("byRef", Type.IsByRef.ToString());
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

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static UnityApiParameter ImportFrom(XElement parameter)
        {
            var typeName = parameter.Attribute("type").Value;
            var isArray = bool.Parse(parameter.Attribute("array").Value);
            var byRefAttribute = parameter.Attribute("byRef");
            var isByRef = byRefAttribute != null && bool.Parse(byRefAttribute.Value);
            var name = parameter.Attribute("name").Value;
            var justification = parameter.Attribute("justification")?.Value;
            var isOptional = justification != null && bool.Parse(parameter.Attribute("optional").Value);
            var description = parameter.Attribute("description")?.Value;
            var apiType = new ApiType(typeName + (isArray ? "[]" : string.Empty) + (isByRef ? "&" : string.Empty));
            var p = new UnityApiParameter(name, apiType, description);
            if (isOptional)
                p.SetOptional(justification);
            return p;
        }

        public bool IsEquivalent(UnityApiParameter other)
        {
            if (myDescription != other.myDescription && !string.IsNullOrEmpty(other.myDescription))
                return false;
            return Equals(Type, other.Type);
        }

        public void Update(UnityApiParameter newParameter, string functionName)
        {
            // E.g. 2018.2 removed a UnityScript example for AssetProcessor.OnPostprocessSprites, so newer docs don't
            // have the proper parameter name. If the old one does, keep it.
            if (Name != newParameter.Name && !string.IsNullOrEmpty(newParameter.Name) && !newParameter.Name.StartsWith("arg"))
            {
                Name = newParameter.Name;
            }

            if (myDescription != newParameter.myDescription && !string.IsNullOrEmpty(newParameter.myDescription))
            {
                myDescription = newParameter.myDescription;
            }

            if (Type.FullName != newParameter.Type.FullName)
                throw new InvalidOperationException($"Parameter type differences for parameter {Name} of {functionName}! {Type.FullName} != {newParameter.Type.FullName}");

            if (Type.IsArray != newParameter.Type.IsArray || Type.IsByRef != newParameter.Type.IsByRef)
            {
                Console.WriteLine("WARNING: Parameter `{2}` of function `{3}` type changed: was {0} now {1}", Type, newParameter.Type, Name, functionName);
                Type = newParameter.Type;
            }
        }

        public override string ToString() => $"{Name}: {Type}";
    }
}