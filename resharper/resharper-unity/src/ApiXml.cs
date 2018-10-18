using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class ApiXml
    {
        private readonly IDictionary<string, IClrTypeName> myTypeNames = new Dictionary<string, IClrTypeName>();

        public UnityTypes LoadTypes()
        {
            var types = new List<UnityType>();

            var ns = GetType().Namespace;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ns + @".api.xml"))
            {
                Assertion.AssertNotNull(stream, "stream != null");

                var document = new XmlDocument();
                document.Load(stream);

                var apiNode = document.DocumentElement?.SelectSingleNode("/api");
                Assertion.AssertNotNull(apiNode, "apiNode != null");

                var minimumVersion = apiNode.Attributes?["minimumVersion"]?.Value;
                var maximumVersion = apiNode.Attributes?["maximumVersion"]?.Value;

                Assertion.Assert(minimumVersion != null && maximumVersion != null,
                    "minimumVersion != null && maximumVersion != null");

                var nodes = document.DocumentElement?.SelectNodes(@"/api/type");
                Assertion.AssertNotNull(nodes, "nodes != null");
                foreach (XmlNode type in nodes)
                    types.Add(CreateUnityType(type, minimumVersion, maximumVersion));

                return new UnityTypes(types, Version.Parse(minimumVersion), Version.Parse(maximumVersion));
            }
        }

        private IClrTypeName GetClrTypeName(string typeName)
        {
            if (!myTypeNames.TryGetValue(typeName, out var clrTypeName))
            {
                clrTypeName = new ClrTypeName(typeName);
                myTypeNames.Add(typeName, clrTypeName);
            }
            return clrTypeName;
        }

        private UnityType CreateUnityType(XmlNode type, string defaultMinimumVersion, string defaultMaximumVersion)
        {
            var name = type.Attributes?["name"].Value;
            var ns = type.Attributes?["ns"].Value;

            var minimumVersion = ParseVersionAttribute(type, "minimumVersion", defaultMinimumVersion);
            var maximumVersion = ParseVersionAttribute(type, "maximumVersion", defaultMaximumVersion);

            var typeName = GetClrTypeName($"{ns}.{name}");
            var messageNodes = type.SelectNodes("message");
            var messages = EmptyArray<UnityEventFunction>.Instance;
            if (messageNodes != null)
            {
                messages = messageNodes.OfType<XmlNode>().Select(
                    node => CreateUnityMessage(node, typeName.GetFullNameFast(), defaultMinimumVersion,
                        defaultMaximumVersion)).OrderBy(m => m.Name).ToArray();
            }

            return new UnityType(typeName, messages, minimumVersion, maximumVersion);
        }

        private static Version ParseVersionAttribute(XmlNode node, string attributeName, string defaultValue)
        {
            var attributeValue = node.Attributes?[attributeName]?.Value ?? defaultValue;
            return Version.Parse(attributeValue);
        }

        private UnityEventFunction CreateUnityMessage(XmlNode node, string typeName, string defaultMinimumVersion, string defaultMaximumVersion)
        {
            var name = node.Attributes?["name"].Value ?? "Invalid";
            var description = node.Attributes?["description"]?.Value;
            var isStatic = bool.Parse(node.Attributes?["static"]?.Value ?? "false");
            var isCoroutine = bool.Parse(node.Attributes?["coroutine"]?.Value ?? "false");
            var isUndocumented = bool.Parse(node.Attributes?["undocumented"]?.Value ?? "false");

            var minimumVersion = ParseVersionAttribute(node, "minimumVersion", defaultMinimumVersion);
            var maximumVersion = ParseVersionAttribute(node, "maximumVersion", defaultMaximumVersion);

            var parameters = EmptyArray<UnityEventFunctionParameter>.Instance;

            var parameterNodes = node.SelectNodes("parameters/parameter");
            if (parameterNodes != null)
            {
                parameters = parameterNodes.OfType<XmlNode>().Select(LoadParameter).ToArray();
            }

            var returnsArray = false;
            var returnType = PredefinedType.VOID_FQN;
            var returns = node.SelectSingleNode("returns");
            if (returns != null)
            {
                returnsArray = bool.Parse(returns.Attributes?["array"].Value ?? "false");
                var type = returns.Attributes?["type"]?.Value ?? "System.Void";
                returnType = GetClrTypeName(type);
            }

            return new UnityEventFunction(name, typeName, returnType, returnsArray, isStatic, isCoroutine, description, isUndocumented, minimumVersion, maximumVersion, parameters);
        }

        private UnityEventFunctionParameter LoadParameter([NotNull] XmlNode node, int i)
        {
            var type = node.Attributes?["type"]?.Value;
            var name = node.Attributes?["name"].Value;
            var description = node.Attributes?["description"]?.Value;
            var isArray = bool.Parse(node.Attributes?["array"]?.Value ?? "false");
            var isByRef = bool.Parse(node.Attributes?["byRef"]?.Value ?? "false");
            var isOptional = bool.Parse(node.Attributes?["optional"]?.Value ?? "false");
            var justification = node.Attributes?["justification"]?.Value;

            if (type == null || name == null)
            {
                return new UnityEventFunctionParameter(name ?? $"arg{i + 1}", PredefinedType.INT_FQN, description, isArray, isByRef, isOptional, justification);
            }

            var parameterType = GetClrTypeName(type);
            return new UnityEventFunctionParameter(name, parameterType, description, isArray, isByRef, isOptional, justification);
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
