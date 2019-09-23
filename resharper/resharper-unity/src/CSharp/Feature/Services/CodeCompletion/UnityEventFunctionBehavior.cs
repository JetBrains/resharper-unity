using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
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
    public class UnityEventFunctionBehavior : TextualBehavior<TextualInfo>
    {
        private readonly IShellLocks myShellLocks;
        private readonly UnityEventFunction myEventFunction;
        private readonly AccessRights myAccessRights;

        public UnityEventFunctionBehavior(IShellLocks shellLocks, TextualInfo info,
                                          UnityEventFunction eventFunction, AccessRights accessRights)
            : base(info)
        {
            myShellLocks = shellLocks;
            myEventFunction = eventFunction;
            myAccessRights = accessRights;
        }

        public override void Accept(ITextControl textControl, DocumentRange nameRange,
            LookupItemInsertType lookupItemInsertType, Suffix suffix,
            ISolution solution, bool keepCaretStill)
        {
            Accept(textControl, nameRange, solution);
        }

        private void Accept(ITextControl textControl, DocumentRange nameRange, ISolution solution)
        {
            var psiServices = solution.GetPsiServices();

            // Get the node at the caret. This will be the identifier
            var identifierNode = TextControlToPsi.GetElement<ITreeNode>(solution, textControl) as IIdentifier;

            var methodDeclaration = TextControlToPsi.GetElement<IMethodDeclaration>(solution, textControl);
            if (UpdateExistingMethod(methodDeclaration, psiServices))
                return;

            if (identifierNode != null)
            {
                // Delete the half completed identifier node. Also delete any explicitly entered return type, as our
                // declared element will create one anyway
                if (!(identifierNode.GetPreviousMeaningfulSibling() is ITypeUsage typeUsage))
                {
                    // E.g. `void OnAnim{caret} [SerializeField]...` This is parsed as a field with an array specifier
                    var fieldDeclaration = identifierNode.GetContainingNode<IFieldDeclaration>();
                    typeUsage = fieldDeclaration?.GetPreviousMeaningfulSibling() as ITypeUsage;
                }

                var parameterListStart = methodDeclaration?.LPar;
                var parameterListEnd = methodDeclaration?.RPar;

                using (var cookie = new PsiTransactionCookie(psiServices, DefaultAction.Rollback, "RemoveIdentifier"))
                using (new DisableCodeFormatter())
                {
                    using (WriteLockCookie.Create())
                    {
                        ModificationUtil.DeleteChild(identifierNode);
                        if (typeUsage != null)
                        {
                            nameRange = nameRange.Shift(-typeUsage.GetTextLength());
                            ModificationUtil.DeleteChild(typeUsage);

                            // Also delete the parameter list, if there is one. If there was an existing method declaration,
                            // with parameter list and body, we would have fixed it by simply replacing the name. Deleting
                            // an existing parameter list allows rewriting the return type, method name, parameter list and
                            // body
                            if (parameterListStart != null && parameterListEnd != null)
                                ModificationUtil.DeleteChildRange(parameterListStart, parameterListEnd);
                            else if (parameterListStart != null)
                                ModificationUtil.DeleteChild(parameterListStart);
                            else if (parameterListEnd != null)
                                ModificationUtil.DeleteChild(parameterListEnd);
                        }
                    }

                    cookie.Commit();
                }
            }

            // Insert a dummy method declaration, as text, which means the PSI is reparsed. This will remove empty type
            // usages and merge leading attributes into a method declaration, such that we can copy them and replace
            // them once the declared element has expanded. This also fixes up the case where the type usage picks up
            // the attribute of the next code construct as an array specifier. E.g. `OnAni{caret} [SerializeField]`
            using (WriteLockCookie.Create())
                textControl.Document.InsertText(nameRange.StartOffset, "void Foo(){}");

            psiServices.Files.CommitAllDocuments();

            methodDeclaration = TextControlToPsi.GetElement<IMethodDeclaration>(solution, textControl);
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
                GeneratorUnityKinds.UnityEventFunctions, solution, textControl, classDeclaration.Language,
                configureContext: context =>
                {
                    // Note that the generated code will use the access rights, if specified. However, if they haven't
                    // been specified (NONE) or they are the default for methods (PRIVATE), the generated code will be
                    // whatever the current code style setting is - implicit or explicit
                    var declaredElement = myEventFunction.CreateDeclaration(factory, classDeclaration, myAccessRights)
                        .DeclaredElement.NotNull("declaredElement != null");
                    context.InputElements.Clear();
                    context.InputElements.Add(new GeneratorDeclaredElement(declaredElement));
                },
                onCompleted: context =>
                {
                    if (attributeList == null)
                        return;

                    methodDeclaration = TextControlToPsi.GetElement<IMethodDeclaration>(solution, textControl);
                    if (methodDeclaration == null)
                        return;

                    // The Generate workflow adds a helper function to the queue to select the contents of the method.
                    // Unfortunately, the offsets are calculated before adding the callback to the queue. If we modify
                    // the PSI directly here, the offsets are incorrect and the selection is wrong. Doing it this way
                    // loses the selection, but at least everything works.
                    // Technically, we should probably add the attributes during the generation method, but then we'd
                    // lose how multiple attributes are split into sections, etc.
                    myShellLocks.Queue(Lifetime.Eternal, "FinishDeclaration", () =>
                    {
                        using (ReadLockCookie.Create())
                        using (var transactionCookie =
                            new PsiTransactionCookie(psiServices, DefaultAction.Rollback, "FinishDeclaration"))
                        {
                            methodDeclaration.SetAttributeSectionList(attributeList);
                            transactionCookie.Commit();
                        }
                    });
                });
        }

        private bool UpdateExistingMethod([CanBeNull] IMethodDeclaration methodDeclaration, IPsiServices psiServices)
        {
            if (methodDeclaration?.Body == null)
                return false;

            var classLikeDeclaration = methodDeclaration.GetContainingTypeDeclaration() as IClassLikeDeclaration;
            if (classLikeDeclaration == null)
                return false;

            using (var cookie = new PsiTransactionCookie(psiServices, DefaultAction.Rollback, "UpdateExistingMethod"))
            using (WriteLockCookie.Create())
            {
                methodDeclaration.SetName(myEventFunction.Name);

                // TODO: We should also update return type and parameters
                // This doesn't work - it doesn't shorten the references and we end up "global::System.Void". Don't know
                // why and don't have time to look into right now.
                // At least the method signature inspections will help fix up if necessary
                // When this comes back, remember to try to match the existing parameters - they might be correct but
                // renamed. We don't want to set the names back and break code
//                methodDeclaration.SetTypeUsage(newDeclaration.TypeUsage);
//                methodDeclaration.SetParams(newDeclaration.Params);

                cookie.Commit();
            }

            return true;
        }
    }
}
