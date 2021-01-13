﻿using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Psi.CSharp.Resources;
using JetBrains.ReSharper.Psi.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    // Defines a category for the UI, and the scope points that it includes
    [ScopeCategoryUIProvider(Priority = Priority, ScopeFilter = ScopeFilter.Project)]
    public class UnityProjectScopeCategoryUIProvider : ScopeCategoryUIProvider
    {
        static UnityProjectScopeCategoryUIProvider()
        {
            // UnityCSharp requires its own icon rather than the generic C# icon because it's used as the group icon
            // for the UITag "Unity Class" menu item
            TemplateImage.Register("UnityCSharp", UnityFileTypeThemedIcons.FileUnity.Id);
            TemplateImage.Register("UnityShaderLab", ShaderFileTypeThemedIcons.FileShader.Id);
            TemplateImage.Register("UnityAsmDef", PsiJavaScriptThemedIcons.Json.Id);
        }

        // Needs to be less than other priorities in R#'s built in ScopeCategoryUIProvider
        // to push it to the end of the list
        private const int Priority = -200;

        public UnityProjectScopeCategoryUIProvider()
            : base(LogoIcons.Unity.Id)
        {
            // The main scope point is used to the UID of the QuickList for this category.
            // It does nothing unless there is also a QuickList stored in settings.
            MainPoint = new InUnityCSharpProject();
        }

        public override IEnumerable<ITemplateScopePoint> BuildAllPoints()
        {
            // TODO: Remove this once RIDER-10132 is fixed
            // Exposing this simply allows custom templates to be included in the same group (and "Unity Class" UITag)
            // as the default templates.
            yield return new UnityFileTemplateSectionMarker();

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