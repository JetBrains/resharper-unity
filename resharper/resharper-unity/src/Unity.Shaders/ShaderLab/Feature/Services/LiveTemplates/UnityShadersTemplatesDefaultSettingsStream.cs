#nullable enable
using System;
using System.IO;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.LiveTemplates;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates
{
    [ShellComponent]
    public class UnityShadersTemplatesDefaultSettingsStream : IHaveDefaultSettingsStream, ITemplatesLocalizationSource
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