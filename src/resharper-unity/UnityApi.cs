using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityApi
    {
        private readonly IDictionary<string, IClrTypeName> myTypeNames = new Dictionary<string, IClrTypeName>();
        private readonly List<UnityType> myTypes = new List<UnityType>();

        public UnityApi()
        {
            var nodes = ApiXml.SelectNodes(@"/api/messages/type");
            if (nodes == null) return;

            foreach (XmlNode type in nodes)
                myTypes.Add(CreateUnityType(type));
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

        [NotNull]
        public IEnumerable<UnityType> GetBaseUnityTypes([NotNull] ITypeElement type)
        {
            return myTypes.Where(c => type.IsDescendantOf(c.GetType(type.Module)));
        }

        public bool IsUnityType([NotNull] ITypeElement type)
        {
            return GetBaseUnityTypes(type).Any();
        }

        public bool IsUnityMessage([NotNull] IMethod method)
        {
            var containingType = method.GetContainingType();
            if (containingType != null)
            {
                return GetBaseUnityTypes(containingType).Any(type => type.Contains(method));
            }
            return false;
        }

        public UnityMessage GetUnityMessage([NotNull] IMethod method)
        {
            var containingType = method.GetContainingType();
            if (containingType != null)
            {
                var messages = from t in GetBaseUnityTypes(containingType)
                    from m in t.Messages
                    where m.Match(method)
                    select m;
                return messages.FirstOrDefault();
            }
            return null;
        }
    }
}