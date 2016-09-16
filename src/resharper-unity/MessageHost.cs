using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class MessageHost
    {
        private readonly MonoBehaviourEvent[] messages = EmptyArray<MonoBehaviourEvent>.Instance;
        private readonly IClrTypeName typeName;

        public MessageHost([NotNull] XmlNode type)
        {
            var key = type.Attributes?["key"].Value;

            typeName = key != null ? UnityEnginePredefinedType.GetType(key) : PredefinedType.VOID_FQN;

            var messageNodes = type.SelectNodes("message");
            if (messageNodes != null)
                messages = messageNodes.OfType<XmlNode>().Select(LoadMessage).ToArray();
        }

        [NotNull]
        public IEnumerable<MonoBehaviourEvent> Messages => messages;

        [CanBeNull]
        public ITypeElement GetType([NotNull] IPsiModule module)
        {
            var type = TypeFactory.CreateTypeByCLRName(typeName, module);
            return type.GetTypeElement();
        }

        private static MonoBehaviourEvent LoadMessage([NotNull] XmlNode node)
        {
            var name = node.Attributes?["name"].Value ?? "Invalid";
            var isStatic = bool.Parse(node.Attributes?["static"].Value ?? "false");

            var parameters = EmptyArray<MonoBehaviourEventParameter>.Instance;

            var parameterNodes = node.SelectNodes("parameters/parameter");
            if (parameterNodes != null)
            {
                parameters = parameterNodes.OfType<XmlNode>().Select(LoadParameter).ToArray();
            }

            var returnsArray = false;
            var returnsType = PredefinedType.VOID_FQN;
            var returns = node.SelectSingleNode("returns");
            if (returns != null)
            {
                returnsArray = bool.Parse(returns.Attributes?["array"].Value ?? "false");
                var returnsKey = returns.Attributes?["key"].Value;
                if (returnsKey != null) returnsType = UnityEnginePredefinedType.GetType(returnsKey);
            }

            return new MonoBehaviourEvent(name, returnsType, returnsArray, isStatic, parameters);
        }

        private static MonoBehaviourEventParameter LoadParameter([NotNull] XmlNode node, int i)
        {
            var key = node.Attributes?["key"].Value;
            var name = node.Attributes?["name"].Value;
            var isArray = bool.Parse( node.Attributes?[ "array" ].Value ?? "false" );

            if (key == null || name == null)
            {
                return new MonoBehaviourEventParameter(name ?? $"arg{i + 1}", PredefinedType.INT_FQN, isArray);
            }

            var type = UnityEnginePredefinedType.GetType(key);
            return new MonoBehaviourEventParameter(name, type, isArray);
        }

        public bool Contains([NotNull] IMethod method)
        {
            return messages.Any(m => m.Match(method));
        }
    }
}