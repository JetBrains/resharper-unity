using System.Collections.Generic;
using System.Text;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.JavaScript.Util.Literals;
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
                   sourceFile.LanguageType.Is<UnityYamlProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   sourceFile.IsAsset();
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;


            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<UnityYamlLanguage>() as IYamlFile;
            if (file == null)
                return null;

            var result = new Dictionary<string, string>();
            foreach (var document in file.DocumentsEnumerable)
            {
                var buffer = document.GetTextAsBuffer();
                if (ourGameObjectReferenceStringSearcher.Find(buffer) >= 0)
                {
                    var anchor = GetAnchorFromBuffer(document);
                    var name = GetNameFromBuffer(document);
                    result[anchor] = name;
                }
            }

            if (result.Count == 0)
                return null;
            return result;
        }
        
        public static string GetAnchorFromBuffer(IYamlDocument document)
        {
            var buffer = document.GetTextAsBuffer();
            var index = 0;
            while (true)
            {
                if (index == buffer.Length)
                {
                    return null;
                }
                
                if (buffer[index] == '&')
                    break;

                index++;
            }
            index++;

            var sb = new StringBuilder();
            while (index != buffer.Length && buffer[index].IsDigit())
            {
                sb.Append(buffer[index++]);
            }

            return sb.ToString();
        }
        
        public static string GetNameFromBuffer(IYamlDocument document)
        {
            var buffer = document.GetTextAsBuffer();
            var index = 0;
            while (true)
            {
                if (index + 7 == buffer.Length)
                {
                    return null;
                }
                
                if (buffer[index] == 'm' &&
                    buffer[index + 1] == '_' &&
                    buffer[index + 2] == 'N' &&
                    buffer[index + 3] == 'a' &&
                    buffer[index + 4] == 'm' &&
                    buffer[index + 5] == 'e' &&
                    buffer[index + 6] == ':' &&
                    buffer[index + 7] == ' ')
                    break;

                index++;
            }
            index += 8;

            var sb = new StringBuilder();
            while (index != buffer.Length && buffer[index] != '\r' && buffer[index] != '\n')
            {
                sb.Append(buffer[index++]);
            }

            return sb.ToString();
        }
    }
}