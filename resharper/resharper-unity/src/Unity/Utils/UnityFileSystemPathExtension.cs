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
                var headerChars = new char[6];
                var read = reader.Read(headerChars, 0, headerChars.Length);
                var isYamlFile = read == headerChars.Length && headerChars[0] == '%'
                                                            && (IsYamlHeader(headerChars) || IsUnityHeader(headerChars));
                logger.Trace($"Sniffed YAML header for file {sourceFile}: {isYamlFile}");
                return isYamlFile;
            });
            return isYaml;
        }

        private static bool IsYamlHeader(char[] headerChars)
        {
            return headerChars[1] == 'Y' &&
                   headerChars[2] == 'A' &&
                   headerChars[3] == 'M' &&
                   headerChars[4] == 'L';
        }

        private static bool IsUnityHeader(char[] headerChars)
        {
            return headerChars[1] == 'U' &&
                   headerChars[2] == 'N' &&
                   headerChars[3] == 'I' &&
                   headerChars[4] == 'T' &&
                   headerChars[5] == 'Y';
        }
    }
}