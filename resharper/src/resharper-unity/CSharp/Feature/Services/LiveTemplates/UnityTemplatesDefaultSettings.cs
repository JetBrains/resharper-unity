using System.IO;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates
{
    [ShellComponent]
    public class UnityTemplatesDefaultSettings : IHaveDefaultSettingsStream
    {
        public Stream GetDefaultSettingsStream(Lifetime lifetime)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JetBrains.ReSharper.Plugins.Unity.Templates.templates.dotSettings");
            Assertion.AssertNotNull(stream, "stream != null");
            lifetime.AddDispose(stream);
            return stream;
        }

        public string Name => "Unity default LiveTemplates";
    }
}