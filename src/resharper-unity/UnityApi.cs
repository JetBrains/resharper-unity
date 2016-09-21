using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityApi
    {
        private readonly List<UnityType> myTypes = new List<UnityType>();

        public UnityApi()
        {
            var nodes = ApiXml.SelectNodes( @"/api/messages/type" );
            if (nodes == null) return;

            foreach (XmlNode type in nodes)
            {
                myTypes.Add(CreateMessageHost(type));
            }
        }

        private static UnityType CreateMessageHost(XmlNode type)
        {
            var key = type.Attributes?["key"].Value;

            var typeName = key != null ? UnityEnginePredefinedType.GetType(key) : PredefinedType.VOID_FQN;

            var messageNodes = type.SelectNodes("message");
            var messages = EmptyArray<UnityMessage>.Instance;
            if (messageNodes != null)
            {
                messages = messageNodes.OfType<XmlNode>().Select(CreateUnityMessage).ToArray();
            }

            return new UnityType(typeName, messages);
        }

        private static UnityMessage CreateUnityMessage(XmlNode node)
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
                var returnsKey = returns.Attributes?["key"].Value;
                if (returnsKey != null) returnType = UnityEnginePredefinedType.GetType(returnsKey);
            }

            return new UnityMessage(name, returnType, returnsArray, isStatic, description, parameters);
        }

        private static UnityMessageParameter LoadParameter([NotNull] XmlNode node, int i)
        {
            var key = node.Attributes?["key"].Value;
            var name = node.Attributes?["name"].Value;
            var description = node.Attributes?["description"]?.Value;
            var isArray = bool.Parse(node.Attributes?["array"].Value ?? "false");

            if (key == null || name == null)
            {
                return new UnityMessageParameter(name ?? $"arg{i + 1}", PredefinedType.INT_FQN, description, isArray);
            }

            var type = UnityEnginePredefinedType.GetType(key);
            return new UnityMessageParameter(name, type, description, isArray);
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