using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    public class ApiXml
    {
        private readonly Dictionary<string, IClrTypeName> myTypeNames = new();
        private readonly Dictionary<string, UnityTypeSpec> myTypeSpecs = new();
        private readonly Dictionary<string, Version> myVersions = new();
        private readonly JetHashSet<string> myIdentifiers = new();
        private readonly UnityTypeSpec myDefaultReturnTypeSpec = new(PredefinedType.INT_FQN);

        public UnityTypes LoadTypes()
        {
            var types = new List<UnityType>();

            var ns = GetType().Namespace;
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ns + @".api.xml");
            Assertion.AssertNotNull(stream, "stream != null");

            var document = new XmlDocument();
            document.Load(stream);

            var apiNode = document.DocumentElement?.SelectSingleNode("/api");
            Assertion.AssertNotNull(apiNode, "apiNode != null");

            var minimumVersion = apiNode.Attributes?["minimumVersion"]?.Value;
            var maximumVersion = apiNode.Attributes?["maximumVersion"]?.Value;

            Assertion.Assert(minimumVersion != null && maximumVersion != null);

            var nodes = document.DocumentElement?.SelectNodes(@"/api/type");
            Assertion.AssertNotNull(nodes, "nodes != null");
            foreach (XmlNode type in nodes)
                types.Add(CreateUnityType(type, minimumVersion, maximumVersion));

            return new UnityTypes(types, GetInternedVersion(minimumVersion), GetInternedVersion(maximumVersion));
        }

        private IClrTypeName GetInternedClrTypeName(string typeName)
        {
            if (!myTypeNames.TryGetValue(typeName, out var clrTypeName))
            {
                clrTypeName = new ClrTypeName(typeName);
                myTypeNames.Add(typeName, clrTypeName);
            }
            return clrTypeName;
        }

        private UnityTypeSpec GetInternedTypeSpec(string typeName, bool isArray)
        {
            // Note that the typeName might be a closed generic type, which is not a valid IClrTypeName, but is a good key
            var key = typeName + (isArray ? "[]" : string.Empty);

            if (!myTypeSpecs.TryGetValue(key, out var typeSpec))
            {
                // Note that this means the serialised name needs to be a CLR type name, not a C# name. So System.String
                // instead of `string` and `System.Collections.Generic.List`1[[System.String]]` instead of List<string>.
                // Also note that IClrTypeName does not handle closed generics, so we need to strip the type parameters.
                if (typeName.Contains('`'))
                {
                    // We don't handle nested generics
                    var match = Regex.Match(typeName, @"^(?<outer>.*`(?<count>\d+))\[\[(?<parameters>.*)\]\]$");
                    if (match.Success)
                    {
                        var outer = match.Groups["outer"];
                        var parameters = match.Groups["parameters"];

                        var @params = parameters.Value.Split(',');
                        var typeNames = new IClrTypeName[@params.Length];
                        for (var i = 0; i < @params.Length; i++)
                        {
                            typeNames[i] = GetInternedClrTypeName(@params[i]);
                        }

                        var outerTypeName = GetInternedClrTypeName(outer.Value);
                        typeSpec = new UnityTypeSpec(outerTypeName, isArray: isArray, typeParameters:typeNames);

                        myTypeSpecs.Add(key, typeSpec);
                    }
                    else
                    {
                        // We control the data coming in, so we'll only see this at dev time
                        throw new InvalidDataException("Unhandled formatting for CLR type name");
                    }
                }
                else
                {
                    var clrTypeName = GetInternedClrTypeName(typeName);
                    typeSpec = new UnityTypeSpec(clrTypeName, isArray: isArray);
                    myTypeSpecs.Add(key, typeSpec);
                }
            }

            return typeSpec;
        }

        private UnityType CreateUnityType(XmlNode type, string defaultMinimumVersion, string defaultMaximumVersion)
        {
            var name = type.Attributes?["name"].Value;
            var ns = type.Attributes?["ns"].Value;

            var minimumVersion = ParseVersionAttribute(type, "minimumVersion", defaultMinimumVersion);
            var maximumVersion = ParseVersionAttribute(type, "maximumVersion", defaultMaximumVersion);

            var typeName = GetInternedClrTypeName($"{ns}.{name}");
            var messageNodes = type.SelectNodes("message");
            var messages = EmptyArray<UnityEventFunction>.Instance;
            if (messageNodes != null)
            {
                messages = messageNodes.OfType<XmlNode>()
                    .Select(node => CreateUnityMessage(node, typeName, defaultMinimumVersion, defaultMaximumVersion))
                    .OrderBy(m => m.Name).ToArray();
            }

            return new UnityType(typeName, messages, minimumVersion, maximumVersion);
        }

        private Version ParseVersionAttribute(XmlNode node, string attributeName, string defaultValue)
        {
            var attributeValue = node.Attributes?[attributeName]?.Value ?? defaultValue;
            return GetInternedVersion(attributeValue);
        }

        private Version GetInternedVersion(string versionString)
        {
            if (!myVersions.TryGetValue(versionString, out var version))
            {
                version = Version.Parse(versionString);
                myVersions.Add(versionString, version);
            }

            return version;
        }

        private string GetInternedIdentifier(string identifier)
        {
            return myIdentifiers.Intern(identifier);
        }

        private UnityEventFunction CreateUnityMessage(XmlNode node, IClrTypeName containingTypeName,
                                                      string defaultMinimumVersion, string defaultMaximumVersion)
        {
            var name = node.Attributes?["name"].Value;

            Assertion.AssertNotNull(name, "name != null");

            var description = node.Attributes?["description"]?.Value;
            var isStatic = bool.Parse(node.Attributes?["static"]?.Value ?? "false");
            var canBeCoroutine = bool.Parse(node.Attributes?["coroutine"]?.Value ?? "false");
            var isUndocumented = bool.Parse(node.Attributes?["undocumented"]?.Value ?? "false");

            var minimumVersion = ParseVersionAttribute(node, "minimumVersion", defaultMinimumVersion);
            var maximumVersion = ParseVersionAttribute(node, "maximumVersion", defaultMaximumVersion);

            var parameters = EmptyArray<UnityEventFunctionParameter>.Instance;

            var parameterNodes = node.SelectNodes("parameters/parameter");
            if (parameterNodes != null)
            {
                parameters = parameterNodes.OfType<XmlNode>().Select(LoadParameter).ToArray();
            }

            var returnType = UnityTypeSpec.Void;
            var returns = node.SelectSingleNode("returns");
            if (returns != null)
            {
                var isArray = bool.Parse(returns.Attributes?["array"].Value ?? "false");
                var type = returns.Attributes?["type"]?.Value ?? "System.Void";
                returnType = GetInternedTypeSpec(type, isArray);
            }

            return new UnityEventFunction(GetInternedIdentifier(name), containingTypeName, returnType, isStatic,
                canBeCoroutine, description, isUndocumented, minimumVersion, maximumVersion, parameters);
        }

        private UnityEventFunctionParameter LoadParameter(XmlNode node, int i)
        {
            var type = node.Attributes?["type"]?.Value;
            var name = node.Attributes?["name"].Value;
            var description = node.Attributes?["description"]?.Value;
            var isArray = bool.Parse(node.Attributes?["array"]?.Value ?? "false");
            var isByRef = bool.Parse(node.Attributes?["byRef"]?.Value ?? "false");
            var isOptional = bool.Parse(node.Attributes?["optional"]?.Value ?? "false");
            var justification = node.Attributes?["justification"]?.Value;

            var parameterType = type != null ? GetInternedTypeSpec(type, isArray) : myDefaultReturnTypeSpec;
            var parameterName = GetInternedIdentifier(name ?? $"arg{i + 1}");

            return new UnityEventFunctionParameter(parameterName, parameterType, description, isByRef, isOptional,
                justification);
        }
    }

    public class UnityTypes
    {
        private readonly Version myMinimumVersion;
        private readonly Version myMaximumVersion;

        public UnityTypes(IList<UnityType> types, Version minimumVersion, Version maximumVersion)
        {
            Types = types;
            myMinimumVersion = minimumVersion;
            myMaximumVersion = maximumVersion;
        }

        public Version NormaliseSupportedVersion(Version actualVersion)
        {
            if (actualVersion < myMinimumVersion)
                return myMinimumVersion;
            if (actualVersion > myMaximumVersion)
                return myMaximumVersion;
            return actualVersion;
        }

        public IList<UnityType> Types { get; }
    }
}
