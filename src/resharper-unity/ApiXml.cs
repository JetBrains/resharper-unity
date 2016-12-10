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

        private class VersionNormaliser
        {
            private readonly Version myMinimumVersion;
            private readonly Version myMaximumVersion;
            private readonly Version myActualVersion;

            public VersionNormaliser(Version minimumVersion, Version maximumVersion, Version actualVersion)
            {
                myMinimumVersion = minimumVersion;
                myMaximumVersion = maximumVersion;
                myActualVersion = actualVersion;
            }

            public Version NormaliseMinimum(Version version)
            {
                // Extend minimum to actual, if it's less
                return version == myMinimumVersion && myActualVersion < myMinimumVersion ? myActualVersion : version;
            }

            public Version NormaliseMaximum(Version version)
            {
                // Extend maximum to actual, if it's greater
                return version == myMaximumVersion && myActualVersion > myMaximumVersion ? myActualVersion : version;
            }
        }

        public List<UnityType> LoadTypes(Version currentVersion)
        {
            var types = new List<UnityType>();

            var ns = GetType().Namespace;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ns + @".api.xml"))
            {
                if (stream != null)
                {
                    var document = new XmlDocument();
                    document.Load(stream);

                    var apiNode = document.DocumentElement?.SelectSingleNode("/api");
                    Assertion.AssertNotNull(apiNode, "apiNode != null");

                    var minimumVersion = apiNode.Attributes?["minimumVersion"]?.Value;
                    var maximumVersion = apiNode.Attributes?["maximumVersion"]?.Value;

                    Assertion.Assert(minimumVersion != null && maximumVersion != null, "minimumVersion != null && maximumVersion != null");

                    var normaliser = new VersionNormaliser(Version.Parse(minimumVersion), Version.Parse(maximumVersion),
                        currentVersion);

                    var nodes = document.DocumentElement?.SelectNodes(@"/api/type");
                    Assertion.AssertNotNull(nodes, "nodes != null");
                    foreach (XmlNode type in nodes)
                        types.Add(CreateUnityType(type, normaliser));
                }
            }

            return types;
        }

        private IClrTypeName GetClrTypeName(string typeName)
        {
            IClrTypeName clrTypeName;
            if (!myTypeNames.TryGetValue(typeName, out clrTypeName))
            {
                clrTypeName = new ClrTypeName(typeName);
                myTypeNames.Add(typeName, clrTypeName);
            }
            return clrTypeName;
        }

        private UnityType CreateUnityType(XmlNode type, VersionNormaliser normaliser)
        {
            var name = type.Attributes?["name"].Value;
            var ns = type.Attributes?["ns"].Value;

            var minimumVersion = normaliser.NormaliseMinimum(ParseVersionAttribute(type, "minimumVersion", "1.0"));
            var maximumVersion = normaliser.NormaliseMaximum(ParseVersionAttribute(type, "maximumVersion", "655356.0"));

            var typeName = GetClrTypeName($"{ns}.{name}");
            var messageNodes = type.SelectNodes("message");
            var messages = EmptyArray<UnityEventFunction>.Instance;
            if (messageNodes != null)
            {
                messages = messageNodes.OfType<XmlNode>().Select(
                    node => CreateUnityMessage(node, typeName.GetFullNameFast(), normaliser)).OrderBy(m => m.Name).ToArray();
            }

            return new UnityType(typeName, messages, minimumVersion, maximumVersion);
        }

        private Version ParseVersionAttribute(XmlNode node, string attributeName, string defaultValue)
        {
            var attributeValue = node.Attributes?[attributeName]?.Value ?? defaultValue;
            return Version.Parse(attributeValue);
        }

        private UnityEventFunction CreateUnityMessage(XmlNode node, string typeName, VersionNormaliser normaliser)
        {
            var name = node.Attributes?["name"].Value ?? "Invalid";
            var description = node.Attributes?["description"]?.Value;
            var isStatic = bool.Parse(node.Attributes?["static"]?.Value ?? "false");
            var isUndocumented = bool.Parse(node.Attributes?["undocumented"]?.Value ?? "false");

            var minimumVersion = normaliser.NormaliseMinimum(ParseVersionAttribute(node, "minimumVersion", "1.0"));
            var maximumVersion = normaliser.NormaliseMaximum(ParseVersionAttribute(node, "maximumVersion", "655356.0"));

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

            return new UnityEventFunction(name, typeName, returnType, returnsArray, isStatic, description, isUndocumented, minimumVersion, maximumVersion, parameters);
        }

        private UnityEventFunctionParameter LoadParameter([NotNull] XmlNode node, int i)
        {
            var type = node.Attributes?["type"]?.Value;
            var name = node.Attributes?["name"].Value;
            var description = node.Attributes?["description"]?.Value;
            var isArray = bool.Parse(node.Attributes?["array"].Value ?? "false");

            if (type == null || name == null)
            {
                return new UnityEventFunctionParameter(name ?? $"arg{i + 1}", PredefinedType.INT_FQN, description, isArray);
            }

            var parameterType = GetClrTypeName(type);
            return new UnityEventFunctionParameter(name, parameterType, description, isArray);
        }
    }
}
