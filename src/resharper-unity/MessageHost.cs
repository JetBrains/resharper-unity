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
        private readonly MonoBehaviourEvent[] _messages = EmptyArray<MonoBehaviourEvent>.Instance;
        private readonly IClrTypeName _name;

        public MessageHost([NotNull] XmlNode type)
        {
            string name = type.Attributes?["name"].Value ?? "Invalid";
            string ns = type.Attributes?[@"ns"].Value ?? "Invalid";
            string fullName = string.Concat(ns, ".", name);
            string key = type.Attributes?["key"].Value;

            _name = key != null ? UnityEnginePredefinedType.GetType(key) : PredefinedType.VOID_FQN;

            XmlNodeList messages = type.SelectNodes("message");
            if (messages != null) _messages = messages.OfType<XmlNode>().Select(LoadMessage).ToArray();
        }

        [NotNull]
        public IEnumerable<MonoBehaviourEvent> Messages => _messages;

        [CanBeNull]
        public ITypeElement GetType([NotNull] IPsiModule module)
        {
            IDeclaredType type = TypeFactory.CreateTypeByCLRName(_name, module);
            return type.GetTypeElement();
        }

        private static MonoBehaviourEvent LoadMessage([NotNull] XmlNode node)
        {
            string name = node.Attributes?["name"].Value ?? "Invalid";
            bool isStatic = bool.Parse(node.Attributes?["static"].Value ?? "false");

            MonoBehaviourEventParameter[] parameters = EmptyArray<MonoBehaviourEventParameter>.Instance;

            XmlNodeList parameterNodes = node.SelectNodes("parameters/parameter");
            if (parameterNodes != null)
            {
                parameters = parameterNodes.OfType<XmlNode>().Select(LoadParameter).ToArray();
            }

            var returnsArray = false;
            IClrTypeName returnsType = PredefinedType.VOID_FQN;
            XmlNode returns = node.SelectSingleNode("returns");
            if (returns != null)
            {
                returnsArray = bool.Parse(returns.Attributes?["array"].Value ?? "false");
                string returnsKey = returns.Attributes?["key"].Value;
                if (returnsKey != null) returnsType = UnityEnginePredefinedType.GetType(returnsKey);
            }

            return new MonoBehaviourEvent(name, returnsType, returnsArray, isStatic, parameters);
        }

        private static MonoBehaviourEventParameter LoadParameter([NotNull] XmlNode node, int i)
        {
            string key = node.Attributes?["key"].Value;
            string name = node.Attributes?["name"].Value;
            bool isArray = bool.Parse( node.Attributes?[ "array" ].Value ?? "false" );

            if (key == null || name == null)
            {
                return new MonoBehaviourEventParameter(name ?? $"arg{i + 1}", PredefinedType.INT_FQN, isArray);
            }

            IClrTypeName type = UnityEnginePredefinedType.GetType(key);
            return new MonoBehaviourEventParameter(name, type, isArray);
        }

        public bool Contains([NotNull] IMethod method)
        {
            return _messages.Any(m => m.Match(method));
        }
    }
}