using System.IO;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates
{
    [ShellComponent]
    public class UnityTemplatesDefaultSettings : IHaveDefaultSettingsStream
    {
        // These get added to a static dictionary, so they can be referenced by name from templates
        public static TemplateImage UnityCSharpFile = new TemplateImage("UnityCSharpFile", PsiCSharpThemedIcons.Csharp.Id);
        public static TemplateImage UnityShaderFile = new TemplateImage("UnityShaderFile", LogoThemedIcons.UnityLogo.Id);

        public Stream GetDefaultSettingsStream(Lifetime lifetime)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JetBrains.ReSharper.Plugins.Unity.Templates.templates.dotSettings");
            lifetime.AddDispose(stream);
            return stream;
        }

        public string Name => "Unity default LiveTemplates";
    }
}