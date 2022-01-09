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
            if (!editorInstanceJsonPath.ExistsFile)
                return null;
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(editorInstanceJsonPath.ReadAllText2(Encoding.UTF8).Text);
            if (values.ContainsKey(key))
                return values[key];
            return null;
        }
    }
}