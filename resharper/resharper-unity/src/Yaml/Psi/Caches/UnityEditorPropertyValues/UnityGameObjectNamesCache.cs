using System.Collections.Generic;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Plugins.Yaml.Psi.UnityAsset;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PsiComponent]
    public class UnityGameObjectNamesCache : SimpleICache<Dictionary<string, string>>
    {
        private static readonly StringSearcher ourGameObjectReferenceStringSearcher =
            new StringSearcher("!u!1 &", true);

        public UnityGameObjectNamesCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, CreateMarshaller())

        {
        }

        private static IUnsafeMarshaller<Dictionary<string, string>> CreateMarshaller()
        {
            return UnsafeMarshallers.GetCollectionMarshaller(
                reader => new KeyValuePair<string, string>(reader.ReadString(), reader.ReadString()),
                (writer, value) => { writer.Write(value.Key); writer.Write(value.Value);},
                n => new Dictionary<string, string>(n));
        }

        protected override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return base.IsApplicable(sourceFile) &&
                   sourceFile.LanguageType.Is<UAProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   sourceFile.IsAsset();
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;


            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<UALanguage>() as IYamlFile;
            if (file == null)
                return null;

            var result = new Dictionary<string, string>();
            foreach (var document in file.DocumentsEnumerable)
            {
                var buffer = document.GetTextAsBuffer();
                if (ourGameObjectReferenceStringSearcher.Find(buffer) >= 0)
                {
                    var anchor = document.GetAnchor();
                    var name = document.GetUnityObjectPropertyValue("m_Name").GetPlainScalarText() ?? string.Empty;
                    result[anchor] = name;
                }
            }

            if (result.Count == 0)
                return null;
            return result;
        }
    }
}