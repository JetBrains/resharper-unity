using System;
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
        private readonly Dictionary<string, IClrTypeName> _unityTypes = new Dictionary<string, IClrTypeName>();
        private readonly Dictionary<string, IClrTypeName> _systemTypes;

        public UnityEnginePredefinedType()
        {
            Type predefined = typeof(PredefinedType);
            FieldInfo[] fields = predefined.GetFields(BindingFlags.Static | BindingFlags.Public);
            FieldInfo[] matching = fields.Where(f => typeof(IClrTypeName).IsAssignableFrom(f.FieldType)).ToArray();

            _systemTypes = matching.ToDictionary(
                f => predefined.FullName + "." + f.Name,
                f => (IClrTypeName)f.GetValue(null));

            XmlNodeList nodes = ApiXml.SelectNodes(@"/api/types/type");
            if (nodes == null) return;

            foreach (XmlNode node in nodes)
            {
                string key = node.Attributes?["key"].Value;
                string name = node.Attributes?["name"].Value;

                if (key == null || name == null) continue;

                _unityTypes[key] = new ClrTypeName(name);
            }
        }

        [NotNull]
        private static UnityEnginePredefinedType Instance => Shell.Instance.GetComponent<UnityEnginePredefinedType>();

        [NotNull]
        private IClrTypeName this[[NotNull] string key]
        {
            get
            {
                if (_unityTypes.ContainsKey(key)) return _unityTypes[key];
                return _systemTypes.ContainsKey(key) ? _systemTypes[key] : PredefinedType.VOID_FQN;
            }
        }

        [NotNull]
        public static IClrTypeName GetType([NotNull] string key)
        {
            return Instance[key];
        }
    }
}