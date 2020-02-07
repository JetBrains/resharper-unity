using System;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public class AssetUtils
    {
        private static readonly StringSearcher ourMonoBehaviourCheck = new StringSearcher("!u!114", true);
        private static readonly StringSearcher ourFileIdCheck = new StringSearcher("fileID:", false);

        public static bool IsMonoBehaviourDocument(IBuffer buffer) =>
            ourMonoBehaviourCheck.Find(buffer, 0, Math.Min(buffer.Length, 20)) >= 0;

        public static bool IsReferenceValue(IBuffer buffer) =>
            ourFileIdCheck.Find(buffer, 0, Math.Min(buffer.Length, 30)) >= 0;
    }
}