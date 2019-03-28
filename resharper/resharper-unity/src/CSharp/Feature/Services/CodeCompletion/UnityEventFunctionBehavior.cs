using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    public class UnityEventFunctionBehavior : TextualBehavior<DeclaredElementInfo>
    {
        private readonly UnityEventFunction myEventFunction;
        private readonly AccessRights myAccessRights;

        public UnityEventFunctionBehavior(DeclaredElementInfo info, UnityEventFunction eventFunction, AccessRights accessRights)
            : base(info)
        {
            myEventFunction = eventFunction;
            myAccessRights = accessRights;
        }

        public override void Accept(ITextControl textControl, DocumentRange nameRange,
            LookupItemInsertType lookupItemInsertType, Suffix suffix,
            ISolution solution, bool keepCaretStill)
        {
            var rangeMarker = nameRange.CreateRangeMarkerWithMappingToDocument();
            Accept(textControl, rangeMarker, solution);
        }

        private void Accept(ITextControl textControl, IRangeMarker rangeMarker, ISolution solution)
        {
            var psiServices = solution.GetPsiServices();

            // Get the node at the caret. This will be the identifier
            var identifierNode = TextControlToPsi.GetElement<ITreeNode>(solution, textControl);
            if (identifierNode == null)
                return;

            // Delete the half completed identifier node. Also delete any explicitly entered return type, as our
            // declared element will create one anyway
            if (!(identifierNode.GetPreviousMeaningfulSibling() is ITypeUsage typeUsage))
            {
                // E.g. `void OnAnim{caret} [SerializeField]...` This is parsed as a field with an array specifier
                var fieldDeclaration = identifierNode.GetContainingNode<IFieldDeclaration>();
                typeUsage = fieldDeclaration?.GetPreviousMeaningfulSibling() as ITypeUsage;
            }

            using (var cookie = new PsiTransactionCookie(psiServices, DefaultAction.Rollback, "RemoveIdentifier"))
            using (new DisableCodeFormatter())
            {
                using (WriteLockCookie.Create())
                {
                    ModificationUtil.DeleteChild(identifierNode);
                    if (typeUsage != null)
                        ModificationUtil.DeleteChild(typeUsage);
                }

                cookie.Commit();
            }

            // Insert a dummy method declaration, as text, which means the PSI is reparsed. This will remove empty type
            // usages and merge leading attributes into a method declaration, such that we can copy them and replace
            // them once the declared element has expanded. This also fixes up the case where the type usage picks up
            // the attribute of the next code construct as an array specifier. E.g. `OnAni{caret} [SerializeField]`
            using (WriteLockCookie.Create())
                textControl.Document.InsertText(rangeMarker.DocumentRange.StartOffset, "void Foo(){}");

            psiServices.Files.CommitAllDocuments();

            var methodDeclaration = TextControlToPsi.GetElement<IMethodDeclaration>(solution, textControl);
            if (methodDeclaration == null) return;

            var attributeList = methodDeclaration.FirstChild as IAttributeSectionList;

            using (var cookie = new PsiTransactionCookie(psiServices, DefaultAction.Rollback, "RemoveInsertedDeclaration"))
            using (new DisableCodeFormatter())
            {
                using (WriteLockCookie.Create())
                    ModificationUtil.DeleteChild(methodDeclaration);
                cookie.Commit();
            }

            var classDeclaration = TextControlToPsi.GetElement<IClassLikeDeclaration>(solution, textControl);
            Assertion.AssertNotNull(classDeclaration, "classDeclaration != null");

            var factory = CSharpElementFactory.GetInstance(classDeclaration);

            // Get the UnityEventFunction generator to actually insert the methods
            GenerateCodeWorkflowBase.ExecuteNonInteractive(
                GeneratorUnityKinds.UnityEventFunctions, solution, textControl, identifierNode.Language,
                configureContext: context =>
                {
                    var declaredElement = myEventFunction.CreateDeclaration(factory, classDeclaration, myAccessRights)
                        .DeclaredElement.NotNull("declaredElement != null");
                    context.InputElements.Clear();
                    context.InputElements.Add(new GeneratorDeclaredElement(declaredElement));
                });

            methodDeclaration = TextControlToPsi.GetElement<IMethodDeclaration>(solution, textControl);
            if (methodDeclaration != null)
            {
                using (var transactionCookie =
                    new PsiTransactionCookie(psiServices, DefaultAction.Rollback, "FinishDeclaration"))
                {
                    // Overwrite access rights. The declared element we give the generator has the correct access rights
                    // unless we have AccessRights.NONE, in which case we explicitly get the default
                    methodDeclaration.SetAccessRights(myAccessRights);
                    if (attributeList != null)
                        methodDeclaration.SetAttributeSectionList(attributeList);
                    transactionCookie.Commit();
                }
            }
        }
    }
}
