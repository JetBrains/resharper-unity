using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.DataContext;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.DataContext;
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

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Framework
{
    public abstract class DrivenTestBase : RefactoringTestBase
    {
        private readonly IDictionary<string, string> mySettingsTables;

        protected DrivenTestBase()
        {
            mySettingsTables = InitSettingsTable();
        }

        [NotNull]
        protected virtual IDictionary<string, string> InitSettingsTable()
        {
            return EmptyDictionary<string, string>.Instance;
        }

        [CanBeNull]
        protected abstract IRefactoringWorkflow CreateRefactoringWorkflow([NotNull] ITextControl control,
            IDataContext context);

        protected string GetMySetting(IDocument document, string setting)
        {
            string value = GetSetting(document.Buffer, setting);
            if (value == null && !mySettingsTables.TryGetValue(setting, out value))
                value = "error";
            return value;
        }

        protected T GetTypedSetting<T>(IDocument document, string setting, Func<string, T> converter)
        {
            string value = GetSetting(document.Buffer, setting);
            if (value == null) return default(T);
            return converter.Invoke(value);
        }

        protected override void DoTest(IProject testProject)
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

            protected override IRefactoringWorkflow CreateRefactoringWorkflow(ITextControl textControl,
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
                dataRules.AddRule("Test", PsiDataConstants.REFERENCE, ctx =>
                {
                    var references = TextControlToPsi.GetReferencesAtCaret(solution, textControl);
                    return references != null ? references.FirstOrDefault() : null;
                });

                dataRules.AddRule("Test", PsiDataConstants.REFERENCES,
                    ctx => TextControlToPsi.GetReferencesAtCaret(solution, textControl));
                dataRules.AddRule("Test", ProjectModelDataConstants.PROJECT_MODEL_ELEMENT, ctx => null);
                dataRules.AddRule("Test", PsiDataConstants.SELECTED_EXPRESSION,
                    ctx => ExpressionSelectionUtil.GetSelectedExpression<ITreeNode>(Solution, textControl, false));
                dataRules.AddRule("Test", DocumentModelDataConstants.EDITOR_CONTEXT, ctx =>
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