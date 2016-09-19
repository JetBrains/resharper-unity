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
        private readonly Dictionary<string, IClrTypeName> myUnityTypes = new Dictionary<string, IClrTypeName>();
        private readonly Dictionary<string, IClrTypeName> mySystemTypes;

        public UnityEnginePredefinedType()
        {
            var predefined = typeof(PredefinedType);
            var fields = predefined.GetFields(BindingFlags.Static | BindingFlags.Public);
            var matching = fields.Where(f => typeof(IClrTypeName).IsAssignableFrom(f.FieldType)).ToArray();

            mySystemTypes = matching.ToDictionary(
                f => predefined.FullName + "." + f.Name,
                f => (IClrTypeName)f.GetValue(null));

            var nodes = ApiXml.SelectNodes(@"/api/types/type");
            if (nodes == null) return;

            foreach (XmlNode node in nodes)
            {
                var key = node.Attributes?["key"].Value;
                var name = node.Attributes?["name"].Value;

                if (key == null || name == null) continue;

                myUnityTypes[key] = new ClrTypeName(name);
            }
        }

        [NotNull]
        private static UnityEnginePredefinedType Instance => Shell.Instance.GetComponent<UnityEnginePredefinedType>();

        [NotNull]
        private IClrTypeName this[[NotNull] string key]
        {
            get
            {
                if (myUnityTypes.ContainsKey(key)) return myUnityTypes[key];
                return mySystemTypes.ContainsKey(key) ? mySystemTypes[key] : PredefinedType.VOID_FQN;
            }
        }

        [NotNull]
        public static IClrTypeName GetType([NotNull] string key)
        {
            return Instance[key];
        }
    }
}