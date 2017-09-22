using System;
using JetBrains.ProjectModel.Properties;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates.Scope
{
    // Defines a scope point, but has no inherent behavour, other than to compare against
    // other scope points. A template can declare that it requires this scope point, and
    // the template will only be made available if a ScopeProvider "publishes" this scope
    // point based on the current context (e.g. the project is a Unity project)
    public class InUnityCSharpProject : InLanguageSpecificProject, IMainScopePoint
    {
        private static readonly Guid DefaultUID = new Guid("B37325A3-4F0A-405B-8A5C-00ECA4ED3B30");
        private static readonly Guid QuickUID = new Guid("D32F297F-E422-4612-839A-FE76D9914B34");

        public InUnityCSharpProject()
            : base(ProjectLanguage.CSHARP)
        {
            AdditionalSuperTypes.Add(typeof(InLanguageSpecificProject));
        }

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => "Unity projects";

        public override PsiLanguageType RelatedLanguage => CSharpLanguage.Instance;

        // Define the name and UID of the QuickList we'll use for Unity projects. Any
        // scope points that are subsets will appear in this QuickList (I think)
        public new string QuickListTitle => "Unity projects";
        public new Guid QuickListUID => QuickUID;
    }
}