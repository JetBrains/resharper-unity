using System.Reflection;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ShellComponent]
    public class ApiXml
    {
        private readonly XmlDocument document = new XmlDocument();

        public ApiXml()
        {
            var ns = typeof(ApiXml).Namespace;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ns + @".api.xml"))
            {
                if (stream != null) document.Load(stream);
            }
        }

        [NotNull]
        private static ApiXml Instance => Shell.Instance.GetComponent<ApiXml>();

        [CanBeNull]
        public static XmlNodeList SelectNodes([NotNull] string xpath)
        {
            return Instance.document.DocumentElement?.SelectNodes(xpath);
        }
    }
}
