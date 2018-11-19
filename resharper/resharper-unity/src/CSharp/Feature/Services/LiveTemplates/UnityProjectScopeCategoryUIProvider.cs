using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp.Resources;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.ThemedIcons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates
{
    // Defines a category for the UI, and the scope points that it includes
    [ScopeCategoryUIProvider(Priority = Priority, ScopeFilter = ScopeFilter.Project)]
    public class UnityProjectScopeCategoryUIProvider : ScopeCategoryUIProvider
    {
        // Needs to be less than other priorities in R#'s built in ScopeCategoryUIProvider
        // to push it to the end of the list
        private const int Priority = -200;

        // These get added to a static dictionary, so they can be referenced by name from templates
        // We're using Unity_CSharp instead of just CSharp, because that's set up to use the C#
        // template scope icon instead of the C# file icon - see RIDER-9903
        public static TemplateImage Unity_CSharp = new TemplateImage("UnityCSharp", PsiCSharpThemedIcons.Csharp.Id);
        public static TemplateImage Unity_ShaderLab = new TemplateImage("UnityShaderLab", DivebomThemedIconsThemedIcons.Shader.Id);
        public static TemplateImage Unity_AsmDef = new TemplateImage("UnityAsmDef", PsiJavaScriptThemedIcons.Json.Id);

        public UnityProjectScopeCategoryUIProvider()
            : base(LogoThemedIcons.UnityLogo.Id)
        {
            // The main scope point is used to the UID of the QuickList for this category.
            // It does nothing unless there is also a QuickList stored in settings.
            MainPoint = new InUnityCSharpProject();
        }

        public override IEnumerable<ITemplateScopePoint> BuildAllPoints()
        {
            yield return new InUnityCSharpProject();
            yield return new InUnityCSharpAssetsFolder();

            yield return new InUnityCSharpEditorFolder();
            yield return new InUnityCSharpRuntimeFolder();

            yield return new InUnityCSharpFirstpassFolder();
            yield return new InUnityCSharpFirstpassEditorFolder();
            yield return new InUnityCSharpFirstpassRuntimeFolder();
        }

        public override string CategoryCaption => "Unity";
    }
}