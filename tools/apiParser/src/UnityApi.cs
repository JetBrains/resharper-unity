﻿using System;
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
                if (eventFunction.ToString() == newFunctionSig)
                {
                    // Prefer an existing documented function that covers this version, than an undocumented one. This
                    // means we will automatically replace our undocumented functions with documented versions without
                    // having to explicitly add an end version
                    if (newFunction.IsUndocumented && !eventFunction.IsUndocumented &&
                        eventFunction.MinimumVersion <= apiVersion && eventFunction.MaximumVersion >= apiVersion)
                    {
                        return;
                    }

                    // If the documented state of both functions is the same, update the function
                    if (newFunction.IsUndocumented == eventFunction.IsUndocumented)
                    {
                        eventFunction.Update(newFunction, apiVersion);
                        return;
                    }
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
            var groupedFunctions = myEventFunctions.GroupBy(f => f.ToString() + f.IsUndocumented);
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
        private readonly UnityApiDescriptions myDescriptions;
        [CanBeNull] private readonly string myDocPath;
        private ApiType myReturnType;
        private readonly IList<UnityApiParameter> myParameters;

        public UnityApiEventFunction(string name, bool isStatic, bool isCoroutine, ApiType returnType,
            Version apiVersion, string docPath = null, bool undocumented = false)
        {
            Name = name;
            myIsStatic = isStatic;
            myIsCoroutine = isCoroutine;
            myDescriptions = new UnityApiDescriptions();
            myDocPath = docPath;
            IsUndocumented = undocumented;
            myReturnType = returnType;

            UpdateSupportedVersion(apiVersion);

            myParameters = new List<UnityApiParameter>();
        }

        public string Name { get; }
        public bool IsUndocumented { get; }

        // Force undocumented functions to the bottom of the export list. More for consistency than anything else
        public object OrderingString => (IsUndocumented ? "zz" : string.Empty) + Name;
        
        public void AddDescription(string text, RiderSupportedLanguages langCode)
        {
            if (text.StartsWith(":ref::") == true) // Yes, really. MonoBehaviour.OnCollisionStay in 2018.1
                text = text.Substring(6);
            myDescriptions.Add(langCode, text);
        }
        
        public void AddParameter(string name, ApiType type, KeyValuePair<RiderSupportedLanguages, string>? description = null)
        {
            var parameter = new UnityApiParameter(name, type, description);
            myParameters.Add(parameter);
        }
        
        public void AddParameter(string name, ApiType type, UnityApiDescriptions descriptions)
        {
            var parameter = new UnityApiParameter(name, type, descriptions);
            myParameters.Add(parameter);
        }

        public void Update(UnityApiEventFunction function, Version apiVersion)
        {
            UpdateSupportedVersion(apiVersion);

            if (IsSupportedVersion(apiVersion))
            {
                myIsStatic = function.myIsStatic;
                
                myDescriptions.Update(function.myDescriptions);

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

        public void UpdateParameterIfExists(string name, UnityApiParameter newParameter)
        {
            var parameter = myParameters.SingleOrDefault(p => p.Name == name) ??
                            myParameters.SingleOrDefault(p => p.Name == newParameter.Name);
            parameter?.Update(newParameter, Name);
        }

        public void ExportTo(XmlTextWriter xmlWriter, HasVersionRange defaultVersions)
        {
            xmlWriter.WriteStartElement("message");
            xmlWriter.WriteAttributeString("name", Name);
            xmlWriter.WriteAttributeString("static", myIsStatic.ToString());
            if (myIsCoroutine)
                xmlWriter.WriteAttributeString("coroutine", "True");
            ExportVersionRange(xmlWriter, defaultVersions);
            if (IsUndocumented)
                xmlWriter.WriteAttributeString("undocumented", "True");
            if (!string.IsNullOrEmpty(myDocPath))
                xmlWriter.WriteAttributeString("path", myDocPath.Replace(@"\", "/"));
            myDescriptions.WriteDescriptions(xmlWriter);
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
            var path = message.Attribute("path")?.Value;
            var undocumentedAttribute = message.Attribute("undocumented");
            var isUndocumented = undocumentedAttribute != null && bool.Parse(undocumentedAttribute.Value);
            var returns = message.Descendants("returns").First();
            var type = returns.Attribute("type").Value;
            var isArray = bool.Parse(returns.Attribute("array").Value);
            var returnType = new ApiType(type + (isArray ? "[]" : string.Empty));
            var function = new UnityApiEventFunction(name, isStatic, isCoroutine, returnType, new Version(int.MaxValue, 0),
                path, isUndocumented);
            function.myDescriptions.ImportFrom(message.Descendants("description"));
            function.ImportVersionRange(message, versions);
            foreach (var parameter in message.Descendants("parameter"))
                function.myParameters.Add(UnityApiParameter.ImportFrom(parameter));
            return function;
        }
    }

    public class UnityApiDescriptions : Dictionary<RiderSupportedLanguages, string>
    {
        private void ExportTo(XmlTextWriter xmlWriter)
        {
            foreach (var description in this)
            {
                xmlWriter.WriteStartElement("description");
                xmlWriter.WriteAttributeString("text", description.Value);
                xmlWriter.WriteAttributeString("langCode", description.Key.ToString());
                xmlWriter.WriteEndElement();
            }
        }

        public void ImportFrom(IEnumerable<XElement> xElements)
        {
            foreach (var description in xElements)
            {
                var t = ImportFrom(description);
                this[t.Key] = t.Value;
            }
        }
    
        private static KeyValuePair<RiderSupportedLanguages, string> ImportFrom(XElement description)
        {
            var text = description.Attribute("text")?.Value;
            var langCode = description.Attribute("langCode")?.Value;
            
            if (Enum.TryParse<RiderSupportedLanguages>(langCode, out var code)) 
                return new KeyValuePair<RiderSupportedLanguages, string>(code, text);
            throw new Exception($"Unable to parse lang code {langCode}");
        }
    
        public void Update(UnityApiDescriptions newDescriptions)
        {
                foreach (var newDesc in newDescriptions)
                {
                    if (!string.IsNullOrEmpty(newDesc.Value)) 
                        this[newDesc.Key] = newDesc.Value;
                }
        }
        
        public void Add(RiderSupportedLanguages key, string value)
        {
            this[key] = value;
        }

        public void WriteDescriptions(XmlTextWriter xmlWriter)
        {
            if (!this.Any())
                return;

            xmlWriter.WriteStartElement("descriptions");
            ExportTo(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        public string GetByLangCode(RiderSupportedLanguages langCode)
        {
            return this[langCode];
        }
    }

    public class UnityApiParameter
    {
        private readonly UnityApiDescriptions myDescriptions;
        private string myJustification;

        public UnityApiParameter(string name, ApiType type, KeyValuePair<RiderSupportedLanguages, string>? description = null)
        {
            Name = name;
            Type = type;
            myDescriptions = new UnityApiDescriptions();
            if (description != null)
                myDescriptions.Add(description.Value.Key, description.Value.Value);
            myJustification = string.Empty;
        }
        
        public UnityApiParameter(string name, ApiType type, UnityApiDescriptions descriptions)
        {
            Name = name;
            Type = type;
            myDescriptions = new UnityApiDescriptions();
            foreach (var description in descriptions)
            {
                myDescriptions.Add(description.Key, description.Value);
            }
                
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
            myDescriptions.WriteDescriptions(xmlWriter);
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
            var apiType = new ApiType(typeName + (isArray ? "[]" : string.Empty) + (isByRef ? "&" : string.Empty));
            var p = new UnityApiParameter(name, apiType);
            p.myDescriptions.ImportFrom(parameter.Descendants("description"));
            if (isOptional)
                p.SetOptional(justification);
            return p;
        }

        public void Update(UnityApiParameter newParameter, string functionName)
        {
            // E.g. 2018.2 removed a UnityScript example for AssetProcessor.OnPostprocessSprites, so newer docs don't
            // have the proper parameter name. If the old one does, keep it.
            if (Name != newParameter.Name && !string.IsNullOrEmpty(newParameter.Name) && !newParameter.Name.StartsWith("arg"))
            {
                Name = newParameter.Name;
            }
            
            myDescriptions.Update(newParameter.myDescriptions);

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