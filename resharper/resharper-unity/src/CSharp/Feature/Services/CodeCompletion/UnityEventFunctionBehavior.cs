using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
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
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using DocumentRange = JetBrains.DocumentModel.DocumentRange;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    public class UnityEventFunctionBehavior : TextualBehavior<UnityEventFunctionTextualInfo>
    {
        private readonly UnityEventFunction myEventFunction;
        private readonly AccessRights myAccessRights;

        public UnityEventFunctionBehavior(UnityEventFunctionTextualInfo info,
                                          UnityEventFunction eventFunction, AccessRights accessRights)
            : base(info)
        {
            myEventFunction = eventFunction;
            myAccessRights = accessRights;
        }

        public override bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext) => false;

        public override void Accept(
            ITextControl textControl, DocumentRange nameRange,
            LookupItemInsertType insertType, Suffix suffix, ISolution solution, bool keepCaretStill)
        {
            var psiServices = solution.GetPsiServices();
            var updateMethodDeclaration = TextControlToPsi.GetElement<IMethodDeclaration>(solution, textControl);
            if (UpdateExistingMethod(updateMethodDeclaration, psiServices))
                return;

            var isCoroutine = updateMethodDeclaration?.TypeUsage is IUserTypeUsage userTypeUsage &&
                              userTypeUsage.ScalarTypeName?.ShortName == "IEnumerator";

            var fixedNameRange = nameRange.SetStartTo(Info.MemberReplaceRanges.InsertRange.StartOffset);
            var memberRange = Info.MemberReplaceRanges.GetAcceptRange(fixedNameRange, insertType);

            // Insert a dummy method declaration, as text, which means the PSI is reparsed. This will remove empty type
            // usages and merge leading attributes into a method declaration, such that we can copy them and replace
            // them once the declared element has expanded. This also fixes up the case where the type usage picks up
            // the attribute of the next code construct as an array specifier. E.g. `OnAni{caret} [SerializeField]`
            using (WriteLockCookie.Create())
            {
                textControl.Document.ReplaceText(memberRange, "void Foo(){}");
            }

            psiServices.Files.CommitAllDocuments();

            var methodDeclaration = TextControlToPsi.GetElement<IMethodDeclaration>(solution, textControl);
            if (methodDeclaration == null) return;

            var methodDeclarationCopy = methodDeclaration.Copy();
            var nodesBeforeCopyRange = NodesBeforeMethodHeader(methodDeclarationCopy);

            using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "RemoveInsertedDeclaration"))
            using (WriteLockCookie.Create())
            {
                LowLevelModificationUtil.DeleteChild(methodDeclaration);
            }

            var classDeclaration = TextControlToPsi.GetElement<IClassLikeDeclaration>(solution, textControl);
            Assertion.AssertNotNull(classDeclaration, "classDeclaration != null");

            var factory = CSharpElementFactory.GetInstance(classDeclaration);

            GenerateCodeWorkflowBase.ExecuteNonInteractive(
                GeneratorUnityKinds.UnityEventFunctions, solution, textControl, methodDeclaration.Language,
                configureContext: context =>
                {
                    // Note that the generated code will use the access rights, if specified. However, if they haven't
                    // been specified (NONE) or they are the default for methods (PRIVATE), the generated code will be
                    // whatever the current code style setting is - implicit or explicit
                    var knownTypesCache = solution.GetComponent<KnownTypesCache>();
                    var declaredElement = myEventFunction.CreateDeclaration(factory, knownTypesCache, classDeclaration,
                            myAccessRights, makeCoroutine: isCoroutine)
                        .DeclaredElement.NotNull("declaredElement != null");
                    context.InputElements.Clear();
                    context.InputElements.Add(new GeneratorDeclaredElement(declaredElement));
                },
                onCompleted: context =>
                {
                    if (nodesBeforeCopyRange.IsEmpty) return;

                    foreach (var outputElement in context.OutputElements)
                    {
                        if (outputElement is GeneratorDeclarationElement declarationElement)
                        {
                            using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "BringBackAttributes"))
                            using (WriteLockCookie.Create())
                            {
                                var newDeclaration = declarationElement.Declaration;
                                ModificationUtil.AddChildRangeAfter(newDeclaration, anchor: null, nodesBeforeCopyRange);
                            }

                            return;
                        }
                    }
                });

            ITreeRange NodesBeforeMethodHeader(IMethodDeclaration declaration)
            {
                var firstNode = declaration.ModifiersList ?? declaration.TypeUsage as ITreeNode;

                var smthBeforeTypeUsage = firstNode?.PrevSibling;
                if (smthBeforeTypeUsage == null) return TreeRange.Empty;

                return new TreeRange(declaration.FirstChild, smthBeforeTypeUsage);
            }
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