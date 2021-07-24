using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityFileSystemPathExtension
    {
        public static bool SniffYamlHeader(this VirtualFileSystemPath sourceFile)
        {
            var isYaml = sourceFile.ReadBinaryStream(reader =>
            {
                var headerChars = new char[5];
                reader.Read(headerChars, 0, headerChars.Length);
                if (headerChars[0] == '%' && headerChars[1] == 'Y' && headerChars[2] == 'A' &&
                    headerChars[3] == 'M' && headerChars[4] == 'L')
                    return true;
                return false;
            });
            return isYaml;
        }
    }
}