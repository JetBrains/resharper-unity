using System;
using System.Linq;
using JetBrains.Application.Components;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.DataContext;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TextControl;
using JetBrains.TextControl.DataContext;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.TestFramework
{
    [Category("QuickDoc")]
    [TestReferences("System.Core", DoesNotInherit = false)]
    public abstract class QuickDocTestBase : BaseTestWithTextControl
    {
        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            var textControl = OpenTextControl(TestLifetime);
            var document = textControl.Document;
            var projectFile = Solution.GetComponent<DocumentManager>().TryGetProjectFile(document);
            Assert.IsNotNull(projectFile, "projectFile == null");

            var context = CreateDataContext(textControl);
            var declaredElement = TextControlToPsi.GetDeclaredElements(Solution, textControl).SingleOrDefault();
            Exception? exception = null;
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

                var text = html.Text;
                var startIdx = text.IndexOf(XmlDocHtmlUtil.START_HEAD_MARKER, StringComparison.Ordinal);
                var endIdx = text.IndexOf(XmlDocHtmlUtil.END_HEAD_MARKER, StringComparison.Ordinal) + XmlDocHtmlUtil.END_HEAD_MARKER.Length;

                while (startIdx != -1)
                {
                    Assert.AreEqual(string.CompareOrdinal(text, endIdx, "\n<body>", 0, "\n<body>".Length), 0);
              
                    text = text.Remove(startIdx, endIdx - startIdx + 1);
                    startIdx = text.IndexOf(XmlDocHtmlUtil.START_HEAD_MARKER, StringComparison.Ordinal);
                    endIdx = text.IndexOf(XmlDocHtmlUtil.END_HEAD_MARKER, StringComparison.Ordinal) + XmlDocHtmlUtil.END_HEAD_MARKER.Length;
                }

                writer.Write(text);
            }));
            if (exception != null)
                throw exception;
        }

        private static IDataContext CreateDataContext(IComponentContainer componentContainer, ISolution solution, ITextControl control)
        {
            var actionManager = componentContainer.GetComponent<IActionManager>();

            var dataRules = DataRules
                .AddRule("Test", ProjectModelDataConstants.SOLUTION, _ => solution)
                .AddRule("Test", TextControlDataConstants.TEXT_CONTROL, _ => control)
                .AddRule("Test", DocumentModelDataConstants.DOCUMENT, _ => control.Document)
                .AddRule("Test", DocumentModelDataConstants.EDITOR_CONTEXT, _ => new DocumentEditorContext(new DocumentOffset(control.Document, control.Caret.Offset())));
            return actionManager.DataContexts.CreateWithDataRules(control.Lifetime, dataRules);
        }

        private IDataContext CreateDataContext(ITextControl control)
        {
            return CreateDataContext(ShellInstance, Solution, control);
        }

        protected abstract void TestAdditionalInfo(IDeclaredElement declaredElement, IProjectFile projectFile);
    }
}