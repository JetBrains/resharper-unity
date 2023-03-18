using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.DataContext;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.ExpressionSelection;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.FeaturesTestFramework.Refactorings;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.TestFramework
{
    public abstract class DrivenTestBase : RefactoringTestBase
    {
        private readonly IDictionary<string, string> mySettingsTables;

        protected DrivenTestBase()
        {
            mySettingsTables = InitSettingsTable();
        }

        protected virtual IDictionary<string, string> InitSettingsTable()
        {
            return EmptyDictionary<string, string>.Instance;
        }

        protected abstract IRefactoringWorkflow? CreateRefactoringWorkflow(ITextControl control,
                                                                           IDataContext context);

        protected string GetMySetting(IDocument document, string setting)
        {
            var value = GetSetting(document.Buffer, setting);
            if (value == null && !mySettingsTables.TryGetValue(setting, out value))
                value = "error";
            return value;
        }

        protected T? GetTypedSetting<T>(IDocument document, string setting, Func<string, T> converter)
        {
            string? value = GetSetting(document.Buffer, setting);
            if (value == null) return default(T);
            return converter.Invoke(value);
        }

        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            CreateTestExecuter().ExecuteTest(testProject);
        }

        protected virtual TestExecutor CreateTestExecuter()
        {
            return new TestExecutor(this);
        }

        protected virtual void AdditionalTestChecks(ITextControl textControl, IProject project)
        {
        }

        protected virtual void AdditionalWorkflowTestChecks(IRefactoringWorkflow workflow, ITextControl textControl,
            IDataContext context)
        {
        }

        protected class TestExecutor : StandardTestExecutorBase
        {
            private readonly DrivenTestBase myDrivenTextBase;

            public TestExecutor(DrivenTestBase test)
                : base(test)
            {
                myDrivenTextBase = test;
            }

            protected override IRefactoringWorkflow? CreateRefactoringWorkflow(ITextControl textControl,
                                                                               IDataContext context)
            {
                return myDrivenTextBase.CreateRefactoringWorkflow(textControl, context);
            }

            protected override void AdditionalTestChecks(ITextControl textControl, IProject testProject)
            {
                myDrivenTextBase.AdditionalTestChecks(textControl, testProject);
            }

            protected override void AdditionalWorkflowTestChecks(IRefactoringWorkflow workflow, ITextControl control,
                IDataContext context)
            {
                myDrivenTextBase.AdditionalWorkflowTestChecks(workflow, control, context);
            }

            protected override IList<IDataRule> ProvideContextData(ITextControl textControl, IProject project,
                ISolution solution)
            {
                var dataRules = base.ProvideContextData(textControl, project, solution);
                var ret = myDrivenTextBase.ProvideContextData(project, textControl);
                dataRules.AddRange(ret);
                if (!ret.IsEmpty())
                    return dataRules;

                dataRules.AddRule("Test", DocumentModelDataConstants.DOCUMENT, textControl.Document);
                dataRules.AddRule("Test", ProjectModelDataConstants.PROJECT, project);
                dataRules.AddRule("Test", PsiDataConstants.REFERENCE, _ =>
                {
                    var references = TextControlToPsi.GetReferencesAtCaret(solution, textControl);
                    return references != null ? references.FirstOrDefault() : null;
                });

                dataRules.AddRule("Test", PsiDataConstants.REFERENCES,
                    _ => TextControlToPsi.GetReferencesAtCaret(solution, textControl));
                dataRules.AddRule("Test", ProjectModelDataConstants.PROJECT_MODEL_ELEMENT, _ => null);
                dataRules.AddRule("Test", PsiDataConstants.SELECTED_EXPRESSION,
                    _ => ExpressionSelectionUtil.GetSelectedExpression<ITreeNode>(Solution, textControl, false));
                dataRules.AddRule("Test", DocumentModelDataConstants.EDITOR_CONTEXT, _ =>
                    textControl.Selection.HasSelection()
                        ? new DocumentEditorContext(textControl.Selection.OneDocumentRangeWithCaret())
                        : null);

                return dataRules;
            }

            protected override void PreparePage(IRefactoringPage refactoringPage, ITextControl textControl)
            {
                //myDrivenTextBase.PreparePage(refactoringPage, textControl);
            }
        }

        protected virtual IList<IDataRule> ProvideContextData(IProject project, ITextControl control)
        {
            return EmptyArray<IDataRule>.Instance;
        }
    }
}