#nullable enable
using System;
using System.IO;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityShadersTemplatesDefaultSettingsStream : IHaveDefaultSettingsStream, ITemplatesLocalizationSource, IDefaultSettingsRootKey<LiveTemplatesSettings>
    {
        public string Name => "Unity Shaders default LiveTemplates";
        public Stream GetDefaultSettingsStream(Lifetime lifetime)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JetBrains.ReSharper.Plugins.Unity.Shaders.Templates.templates.dotSettings");
            Assertion.AssertNotNull(stream, "stream != null");
            lifetime.AddDispose(stream);
            return stream;
        }
        
        public Type GetResourceType() => typeof(Resources.Strings);
    }
}