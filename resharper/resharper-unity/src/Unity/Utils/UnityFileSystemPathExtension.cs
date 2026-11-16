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
                var headerBytes = reader.ReadBytes(6);
                var isYamlFile = headerBytes.Length == 6 && headerBytes[0] == (byte)'%'
                                                         && (IsYamlHeader(headerBytes) || IsUnityHeader(headerBytes));
                logger.Trace($"Sniffed YAML header for file {sourceFile}: {isYamlFile}");
                return isYamlFile;
            });
            return isYaml;
        }

        private static bool IsYamlHeader(byte[] headerBytes)
        {
            return headerBytes[1] == (byte)'Y' &&
                   headerBytes[2] == (byte)'A' &&
                   headerBytes[3] == (byte)'M' &&
                   headerBytes[4] == (byte)'L';
        }

        private static bool IsUnityHeader(byte[] headerBytes)
        {
            return headerBytes[1] == (byte)'U' &&
                   headerBytes[2] == (byte)'N' &&
                   headerBytes[3] == (byte)'I' &&
                   headerBytes[4] == (byte)'T' &&
                   headerBytes[5] == (byte)'Y';
        }
    }
}