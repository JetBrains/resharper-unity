using System;
using System.Linq;
using JetBrains.Application.Components;
using JetBrains.Application.DataContext;
using JetBrains.Application.platforms;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.DataContext;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TextControl;
using JetBrains.TextControl.DataContext;
using NUnit.Framework;

#if WAVE07 || WAVE08
using JetBrains.ActionManagement;
#else
using JetBrains.Application.UI.Actions.ActionManager;
#endif

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Feature.Services.QuickDoc
{
    [Category("QuickDoc")]
#if WAVE07
    [TestReferences(PlatformManager.SystemCore, Inherits = true)]
#else
    [TestReferences(PlatformManager.SystemCore, DoesNotInherit = false)]
#endif
    public abstract class QuickDocTestBase : BaseTestWithTextControl
    {
        protected override void DoTest(IProject testProject)
        {
            using (var textControl = OpenTextControl(testProject))
            {
                var document = textControl.Document;
                var projectFile = Solution.GetComponent<DocumentManager>().TryGetProjectFile(document);
                Assert.IsNotNull(projectFile, "projectFile == null");

                var context = CreateDataContext(textControl);
                var declaredElement = TextControlToPsi.GetDeclaredElements(Solution, textControl).SingleOrDefault();
                Exception exception = null;
                if (declaredElement != null)
                {
                    try
                    {
                        TestAdditionalInfo(declaredElement, projectFile);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }

                var quickDocService = Solution.GetComponent<IQuickDocService>();
                Assert.IsTrue(quickDocService.CanShowQuickDoc(context), "No QuickDoc available");
                quickDocService.ResolveGoto(context, (presenter, language) => ExecuteWithGold(projectFile, writer =>
                {
                    var html = presenter.GetHtml(language).Text;
                    Assert.NotNull(html);
                    var startIdx = html.IndexOf("  <head>", StringComparison.Ordinal);
                    var endIdx = html.IndexOf("</head>", StringComparison.Ordinal) + "</head>".Length;
                    Assert.AreEqual(string.CompareOrdinal(html, endIdx, "\n<body>", 0, "\n<body>".Length), 0);
                    writer.Write(html.Remove(startIdx, endIdx - startIdx + 1));
                }));
                if (exception != null)
                    throw exception;
            }
        }

        public static IDataContext CreateDataContext(IComponentContainer componentContainer, ISolution solution, ITextControl control)
        {
            var actionManager = componentContainer.GetComponent<IActionManager>();

            var dataRules = DataRules
                .AddRule("Test", ProjectModelDataConstants.SOLUTION, x => solution)
                .AddRule("Test", TextControlDataConstants.TEXT_CONTROL, x => control)
                .AddRule("Test", DocumentModelDataConstants.DOCUMENT, x => control.Document)
                .AddRule("Test", DocumentModelDataConstants.EDITOR_CONTEXT, x => new DocumentEditorContext(new DocumentOffset(control.Document, control.Caret.Position.Value.ToDocOffset())));
            return actionManager.DataContexts.CreateWithDataRules(control.Lifetime, dataRules);
        }

        private IDataContext CreateDataContext(ITextControl control)
        {
            return CreateDataContext(ShellInstance, Solution, control);
        }

        protected abstract void TestAdditionalInfo(IDeclaredElement declaredElement, IProjectFile projectFile);
    }
}