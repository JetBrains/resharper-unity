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
    public class UnityEventFunctionBehavior : TextualBehavior<DeclaredElementInfo>
    {
        private readonly UnityEventFunction myEventFunction;

        public UnityEventFunctionBehavior(DeclaredElementInfo info, UnityEventFunction eventFunction)
            : base(info)
        {
            myEventFunction = eventFunction;
        }

        public override void Accept(ITextControl textControl, DocumentRange nameRange, LookupItemInsertType lookupItemInsertType, Suffix suffix,
            ISolution solution, bool keepCaretStill)
        {
            var rangeMarker = nameRange.CreateRangeMarkerWithMappingToDocument();
            Accept(textControl, rangeMarker, solution);
        }
        
        private void Accept(ITextControl textControl, IRangeMarker rangeMarker, ISolution solution)
        {

            var identifierNode = TextControlToPsi.GetElement<ITreeNode>(solution, textControl);
            var psiServices = solution.GetPsiServices();
            if (identifierNode != null)
            {
                IErrorElement errorElement;

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
                textControl.Document.InsertText(rangeMarker.DocumentRange.StartOffset, "void Foo(){}");
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

            // Get the UnityEventFunction generator to actually insert the methods
            GenerateCodeWorkflowBase.ExecuteNonInteractive(
                GeneratorUnityKinds.UnityEventFunctions, solution, textControl, methodDeclaration.Language,
                configureContext: context =>
                {
                    var inputElements = from e in context.ProvidedElements.Cast<GeneratorDeclaredElement<IMethod>>()
                        where myEventFunction.Match(e.DeclaredElement) != EventFunctionMatch.NoMatch
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