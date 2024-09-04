using System.IO;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityTemplatesDefaultSettings : IHaveDefaultSettingsStream, IDefaultSettingsRootKey<LiveTemplatesSettings>
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