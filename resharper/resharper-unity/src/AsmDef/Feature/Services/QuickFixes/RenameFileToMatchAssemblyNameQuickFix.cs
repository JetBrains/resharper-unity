using System;
using JetBrains.Application.Progress;
using JetBrains.DocumentManagers.Transactions;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.QuickFixes
{
    [QuickFix]
    public class RenameFileToMatchAssemblyNameQuickFix : QuickFixBase
    {
        private readonly IJsonNewLiteralExpression myLiteral;

        public RenameFileToMatchAssemblyNameQuickFix(MismatchedAsmDefFilenameWarning warning)
        {
            myLiteral = warning.LiteralExpression;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var projectFile = myLiteral.GetSourceFile().ToProjectFile();
            if (projectFile == null)
                return null;

            var newName = myLiteral.GetUnquotedText() + ".asmdef";
            return _ =>
            {
                if (projectFile.Location.Directory.Combine(newName).ExistsFile)
                {
                    MessageBox.ShowError($"File '{newName}' already exists",
                        $"Cannot rename '{projectFile.Location.Name}'");
                }
                else
                {
                    using (var transactionCookie = solution.CreateTransactionCookie(DefaultAction.Commit, Text,
                        NullProgressIndicator.Instance))
                    {
                        transactionCookie.Rename(projectFile, newName);
                    }
                }
            };
        }

        public override string Text => "Rename file to match assembly name";
        public override bool IsAvailable(IUserDataHolder cache) => ValidUtils.Valid(myLiteral);
    }
}