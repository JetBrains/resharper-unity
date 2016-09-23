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

        public List<UnityType> LoadTypes()
        {
            var types = new List<UnityType>();

            var ns = GetType().Namespace;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ns + @".api.xml"))
            {
                if (stream != null)
                {
                    var document = new XmlDocument();
                    document.Load(stream);

                    var nodes = document.DocumentElement?.SelectNodes(@"/api/messages/type");
                    if (nodes != null)
                    {
                        foreach (XmlNode type in nodes)
                            types.Add(CreateUnityType(type));
                    }
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

        private UnityType CreateUnityType(XmlNode type)
        {
            var name = type.Attributes?["name"].Value;
            var ns = type.Attributes?["ns"].Value;

            var typeName = GetClrTypeName($"{ns}.{name}");
            var messageNodes = type.SelectNodes("message");
            var messages = EmptyArray<UnityMessage>.Instance;
            if (messageNodes != null)
            {
                messages = messageNodes.OfType<XmlNode>().Select(
                    node => CreateUnityMessage(node, typeName.GetFullNameFast())).ToArray();
            }

            return new UnityType(typeName, messages);
        }

        private UnityMessage CreateUnityMessage(XmlNode node, string typeName)
        {
            var name = node.Attributes?["name"].Value ?? "Invalid";
            var description = node.Attributes?["description"]?.Value;
            var isStatic = bool.Parse(node.Attributes?["static"].Value ?? "false");

            var parameters = EmptyArray<UnityMessageParameter>.Instance;

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

            return new UnityMessage(name, typeName, returnType, returnsArray, isStatic, description, parameters);
        }

        private UnityMessageParameter LoadParameter([NotNull] XmlNode node, int i)
        {
            var type = node.Attributes?["type"]?.Value;
            var name = node.Attributes?["name"].Value;
            var description = node.Attributes?["description"]?.Value;
            var isArray = bool.Parse(node.Attributes?["array"].Value ?? "false");

            if (type == null || name == null)
            {
                return new UnityMessageParameter(name ?? $"arg{i + 1}", PredefinedType.INT_FQN, description, isArray);
            }

            var parameterType = GetClrTypeName(type);
            return new UnityMessageParameter(name, parameterType, description, isArray);
        }
    }
}
