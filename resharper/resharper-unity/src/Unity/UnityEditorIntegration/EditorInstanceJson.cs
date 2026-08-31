using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Util;
using Newtonsoft.Json;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    public static class EditorInstanceJson
    {
        [CanBeNull]
        public static string TryGetValue(VirtualFileSystemPath editorInstanceJsonPath, string key)
        {
            var values = TryRead(editorInstanceJsonPath);
            if (values == null) return null;
            values.TryGetValue(key, out var value);
            return value;
        }

        [CanBeNull]
        public static Dictionary<string, string> TryRead(VirtualFileSystemPath editorInstanceJsonPath)
        {
            if (!editorInstanceJsonPath.ExistsFile) return null;
            var jsonString = editorInstanceJsonPath.ReadAllText2(Encoding.UTF8).Text;
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
        }
    }
}