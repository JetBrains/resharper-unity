using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public static class UnityFileSystemPathExtension
    {
        public static bool SniffYamlHeader(this VirtualFileSystemPath sourceFile)
        {
            var logger = Logger.GetLogger(typeof(UnityFileSystemPathExtension));

            var isYaml = sourceFile.ReadBinaryStream(reader =>
            {
                var headerChars = new char[5];
                var read = reader.Read(headerChars, 0, headerChars.Length);
                var isYamlFile = read == 5 && headerChars[0] == '%'
                                           && headerChars[1] == 'Y' && headerChars[2] == 'A'
                                           && headerChars[3] == 'M' && headerChars[4] == 'L';
                logger.Trace("Sniffed YAML header for file {0}: {1}", sourceFile, isYamlFile);
                return isYamlFile;
            });
            return isYaml;
        }
    }
}