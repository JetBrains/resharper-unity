using System;
using System.IO;
using System.Text;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityFileSystemPathExtension
    {
        private static readonly ILogger ourLogger = Logger.GetLogger("UnityFileSystemPathExtension");
        public static bool SniffYamlHeader(this FileSystemPath sourceFile)
        {
            try
            {
                var isYaml = sourceFile.ReadStream(s =>
                {
                    using (var sr = new StreamReader(s, Encoding.UTF8, true, 30))
                    {
                        var headerChars = new char[20];
                        sr.Read(headerChars, 0, headerChars.Length);
                        for (int i = 0; i < 20; i++)
                        {
                            if (headerChars[i] == '%')
                            {
                                if (headerChars[i + 1] == 'Y' && headerChars[i + 2] == 'A' &&
                                    headerChars[i + 3] == 'M' && headerChars[i + 4] == 'L')
                                {
                                    return true;
                                }

                                return false;
                            } 
                        }

                        return false;
                    }
                });
                return isYaml;
            }
            catch (Exception e)
            {
                ourLogger.Error(e, "An error occurred while detecting asset's encoding");
                return false;
            }
        }
    }
}