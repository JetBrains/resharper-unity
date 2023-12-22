using System.Text.RegularExpressions;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public class UnityVersionUtils
    {
        public static (string, bool) GetUnityVersion(string versionInfo)
        {
            const string unknownVersion = "0.0.0f0";
            versionInfo = versionInfo ?? unknownVersion;
            var match = Regex.Match(versionInfo, UnityVersion.VersionRegex);
            if (match.Success)
            {
                var matchedSubstring = match.Value;
                return (matchedSubstring, !matchedSubstring.Equals(versionInfo));
            }
            else
            {
                return (unknownVersion, false);
            }
        }

    }
}