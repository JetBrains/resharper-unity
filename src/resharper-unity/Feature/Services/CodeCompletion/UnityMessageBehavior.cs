using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.CodeCompletion
{
    public class UnityMessageBehavior : TextualBehavior<DeclaredElementInfo>
    {
        private readonly UnityMessage myMessage;

        public UnityMessageBehavior(DeclaredElementInfo info, UnityMessage message)
            : base(info)
        {
            myMessage = message;
        }

        public override void Accept(ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType, Suffix suffix,
            ISolution solution, bool keepCaretStill)
        {
            var rangeMarker = nameRange.CreateRangeMarkerWithMappingToDocument(textControl.Document);

            var identifierNode = TextControlToPsi.GetElement<ITreeNode>(solution, textControl);
            var psiServices = solution.GetPsiServices();
            if (identifierNode != null)
            {
                IErrorElement errorElement = null;

                ITreeNode usage = identifierNode.GetContainingNode<IUserTypeUsage>();
                if (usage != null)
                    errorElement = usage.NextSibling as IErrorElement;
                else
                {
                    usage = identifierNode.PrevSibling;
                    while (usage != null && !(usage is ITypeUsage))
                        usage = usage.PrevSibling;
                    errorElement = identifierNode.NextSibling as IErrorElement;
                }

                using (var cookie = new PsiTransactionCookie(psiServices, DefaultAction.Rollback, "RemoveIdentifier"))
                using (new DisableCodeFormatter())
                {
                    using (WriteLockCookie.Create())
                    {
                        ModificationUtil.DeleteChild(identifierNode);
                        if (usage != null)
                            ModificationUtil.DeleteChild(usage);
                        if (errorElement != null)
                            ModificationUtil.DeleteChild(errorElement);
                    }

                    cookie.Commit();
                }
            }

            using (WriteLockCookie.Create())
            {
                textControl.Document.InsertText(rangeMarker.Range.StartOffset, "void Foo(){}");
            }

            psiServices.Files.CommitAllDocuments();

            var methodDeclaration = TextControlToPsi.GetElement<IMethodDeclaration>(solution, textControl);
            if (methodDeclaration == null) return;

            var insertionIndex = methodDeclaration.GetTreeStartOffset().Offset;

            string attributeText = null;

            var attributeList = methodDeclaration.FirstChild as IAttributeSectionList;
            if (attributeList != null)
            {
                attributeText = attributeList.GetText();
                var treeNode = attributeList.NextSibling;
                while (treeNode is IWhitespaceNode)
                {
                    attributeText += treeNode.GetText();
                    treeNode = treeNode.NextSibling;
                }
            }

            using (var cookie = new PsiTransactionCookie(psiServices, DefaultAction.Rollback, "RemoveInsertedDeclaration"))
            using (new DisableCodeFormatter())
            {
                using (WriteLockCookie.Create())
                    ModificationUtil.DeleteChild(methodDeclaration);

                cookie.Commit();
            }

            // Get the UniteMessages generator to actually insert the methods
            GenerateCodeWorkflowBase.ExecuteNonInteractive(
                GeneratorUnityKinds.UnityMessages, solution, textControl, methodDeclaration.Language,
                configureContext: context =>
                {
                    var inputElements = from e in context.ProvidedElements.Cast<GeneratorDeclaredElement<IMethod>>()
                        where myMessage.Match(e.DeclaredElement)
                        select e;

                    context.InputElements.Clear();
                    context.InputElements.AddRange(inputElements);
                });

            if (!string.IsNullOrEmpty(attributeText))
            {
                using (WriteLockCookie.Create())
                    textControl.Document.InsertText(insertionIndex, attributeText);
            }
        }
    }
}