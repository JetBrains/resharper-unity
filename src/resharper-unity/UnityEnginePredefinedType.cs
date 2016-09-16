using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ShellComponent]
    public class UnityEnginePredefinedType
    {
        private readonly Dictionary<string, IClrTypeName> unityTypes = new Dictionary<string, IClrTypeName>();
        private readonly Dictionary<string, IClrTypeName> systemTypes;

        public UnityEnginePredefinedType()
        {
            var predefined = typeof(PredefinedType);
            var fields = predefined.GetFields(BindingFlags.Static | BindingFlags.Public);
            var matching = fields.Where(f => typeof(IClrTypeName).IsAssignableFrom(f.FieldType)).ToArray();

            systemTypes = matching.ToDictionary(
                f => predefined.FullName + "." + f.Name,
                f => (IClrTypeName)f.GetValue(null));

            var nodes = ApiXml.SelectNodes(@"/api/types/type");
            if (nodes == null) return;

            foreach (XmlNode node in nodes)
            {
                var key = node.Attributes?["key"].Value;
                var name = node.Attributes?["name"].Value;

                if (key == null || name == null) continue;

                unityTypes[key] = new ClrTypeName(name);
            }
        }

        [NotNull]
        private static UnityEnginePredefinedType Instance => Shell.Instance.GetComponent<UnityEnginePredefinedType>();

        [NotNull]
        private IClrTypeName this[[NotNull] string key]
        {
            get
            {
                if (unityTypes.ContainsKey(key)) return unityTypes[key];
                return systemTypes.ContainsKey(key) ? systemTypes[key] : PredefinedType.VOID_FQN;
            }
        }

        [NotNull]
        public static IClrTypeName GetType([NotNull] string key)
        {
            return Instance[key];
        }
    }
}