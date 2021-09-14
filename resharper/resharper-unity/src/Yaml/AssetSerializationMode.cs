using System.Text.RegularExpressions;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    [SolutionComponent]
    public class AssetSerializationMode
    {
        public AssetSerializationMode(ISolution solution, ILogger logger)
        {
            // TODO: React to changes in serialisation mode
            // We could reload the value on any change, and then add or drop all asset files from the custom PSI module
            // Right now, it's much easier to just read it once at solution load

            Mode = SerializationMode.Unknown;

            var solutionDir = solution.SolutionDirectory;
            if (!solutionDir.IsAbsolute) return; // True in tests

            var assetsDir = solutionDir.Combine("Assets");
            var editorSettingsPath = solutionDir.Combine("ProjectSettings\\EditorSettings.asset");

            // We don't need a full check to see if this is a Unity solution - if these things exist, it's good enough
            // TODO: We need to refactor ProjectExtensions, UnityReferencesTracker and UnitySolutionTracker
            // Too many classes doing the same kind of thing, slightly differently. And we need UnitySolutionTracker
            // here, but it's a Rider only folder and too big to refactor right now
            if (assetsDir.ExistsDirectory && editorSettingsPath.ExistsFile)
            {
                // If binary serialisation is enabled, the EditorSettings.asset file might be in binary. Sheesh
                var isEditorSettingsInText = editorSettingsPath.SniffYamlHeader();

                // At best, we can say that it's mixed. If the settings asset is in text, read it to get the proper value
                Mode = SerializationMode.Mixed;

                if (isEditorSettingsInText)
                {
                    var text = editorSettingsPath.ReadAllText2().Text;
                    var match = Regex.Match(text, @"^\s+m_SerializationMode:\s+(?<mode>\d+)\s*$", RegexOptions.Multiline);
                    if (match.Success)
                    {
                        if (int.TryParse(match.Groups["mode"].Value, out var mode))
                        {
                            if (mode >= 0 && mode <= 2)
                                Mode = (SerializationMode) mode;
                        }
                    }
                }

                logger.Verbose("Unity asset serialisation mode: {0}", Mode);
            }
        }

        public SerializationMode Mode { get; protected set; }
        public bool IsForceText => Mode == SerializationMode.ForceText;

        public enum SerializationMode
        {
            Unknown = -1,    // Most likely not a Unity project. Or failure to read
            Mixed = 0,
            ForceBinary = 1,
            ForceText = 2
        }
    }
}