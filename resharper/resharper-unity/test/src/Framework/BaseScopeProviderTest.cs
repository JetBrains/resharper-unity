using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Framework
{
    // Not provided by the default test framework
    public abstract class BaseScopeProviderTest : BaseTestWithTextControl
    {
        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            var textControl = OpenTextControl(lifetime);
            {
                var context = new TemplateAcceptanceContext(testProject.GetSolution(),
                    textControl.Caret.DocumentOffset(), textControl.Selection.OneDocumentRangeWithCaret());
                ExecuteWithGold(textControl.Document, sb =>
                {
                    foreach (var point in CreateScopeProvider().ProvideScopePoints(context))
                    {
                        sb.Write(point.ToString());
                        if (point.Prefix != null)
                            sb.Write(" - prefix: '{0}'", point.Prefix);
                        sb.WriteLine();
                    }
                });
            }
        }
    
        protected abstract IScopeProvider CreateScopeProvider();
    }
}