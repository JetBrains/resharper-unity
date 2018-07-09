using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TextControl;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Framework
{
    [Category("Rename")]
    [TestReferences("System.Core")]
    public abstract class RenameTestBase : DrivenTestBase
    {
        protected const string NewName =    "NEW_NAME";
        protected const string RenameFile = "RENAME_FILE";
        protected const string ChangeText = "CHANGE_TEXT";

        protected override IDictionary<string, string> InitSettingsTable()
        {
            return new Dictionary<string, string>
            {
                {NewName, "zzz"},
                {RenameFile, "false"},
                {ChangeText, "true"}
            };
        }

        protected override IList<IDataRule> ProvideContextData(IProject testProject, ITextControl control)
        {
            return DataRules.AddRule("Test", RenameRefactoringService.RenameDataProvider,
                ctx => CreateRenameDataProvider(ctx, control));
        }


        protected override IRefactoringWorkflow CreateRefactoringWorkflow(ITextControl control, IDataContext context)
        {
            var workflow = RefactoringsManager.Instance.GetWorkflowProviders<RenameWorkflowProvider>()
                .SelectMany(x => x.CreateWorkflow(context)).FirstOrDefault(x => x.IsAvailable(context));

            return workflow;
        }

        protected virtual RenameDataProvider CreateRenameDataProvider(IDataContext context, ITextControl control)
        {
            var setting = GetMySetting(control.Document, NewName);
            return new RenameDataProvider(setting)
            {
                Model =
                {
                    RenameFile = bool.Parse(GetMySetting(control.Document, RenameFile)),
                    ChangeTextOccurrences = bool.Parse(GetMySetting(control.Document, ChangeText)),
                    RenameDerived = true
                }
            };
        }
    }
}