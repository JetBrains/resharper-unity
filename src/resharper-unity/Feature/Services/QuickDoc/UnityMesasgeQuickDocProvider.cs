using System;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel.DataContext;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Providers;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.UI.Application;
using JetBrains.UI.Theming;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickDoc
{
    // Priority must be less than QuickDocLocalSymbolProvider and QuickDocTypeMemberProvider
    [QuickDocProvider(-1)]
    public class UnityMesasgeQuickDocProvider : IQuickDocProvider
    {
        private readonly ISolution mySolution;
        private readonly UnityApi myUnityApi;
        private readonly DocumentManager myDocumentManager;
        private readonly QuickDocTypeMemberProvider myQuickDocTypeMemberProvider;
        private readonly HelpSystem myHelpSystem;
        private readonly ITheming myTheming;

        public UnityMesasgeQuickDocProvider(ISolution solution, UnityApi unityApi,
                                            DocumentManager documentManager, QuickDocTypeMemberProvider quickDocTypeMemberProvider,
                                            HelpSystem helpSystem, ITheming theming)
        {
            mySolution = solution;
            myUnityApi = unityApi;
            myDocumentManager = documentManager;
            myQuickDocTypeMemberProvider = quickDocTypeMemberProvider;
            myHelpSystem = helpSystem;
            myTheming = theming;
        }

        public bool CanNavigate(IDataContext context)
        {
            var project = context.GetData(ProjectModelDataConstants.PROJECT);
            if (project == null || !project.IsUnityProject()) return false;

            var declaredElements = context.GetData(PsiDataConstants.DECLARED_ELEMENTS);
            return declaredElements != null && declaredElements.Any(e => IsMessage(e) || IsMessageParameter(e));
        }

        public void Resolve(IDataContext context, Action<IQuickDocPresenter, PsiLanguageType> resolved)
        {
            var elements = context.GetData(PsiDataConstants.DECLARED_ELEMENTS);
            Assertion.AssertNotNull(elements, "elements != null");

            var document = context.GetData(DocumentModelDataConstants.DOCUMENT);
            IProjectFile projectFile = null;
            if (document != null)
                projectFile = myDocumentManager.TryGetProjectFile(document);

            var defaultLanguage = PresentationUtil.GetPresentationLanguageByContainer(projectFile, mySolution);

            foreach (var element in elements.OfType<IClrDeclaredElement>())
            {
                var message = GetMessage(element);
                if (message != null)
                {
                    var presenter = new UnityMessageQuickDocPresenter(message, element, myQuickDocTypeMemberProvider,
                        myTheming, myHelpSystem);
                    resolved(presenter, defaultLanguage);
                    return;
                }

                var owningMessage = GetMessageParameterOwner(element);
                if (owningMessage != null)
                {
                    var presenter = new UnityMessageQuickDocPresenter(owningMessage, element.ShortName, element,
                        myQuickDocTypeMemberProvider, myTheming, myHelpSystem);
                    resolved(presenter, defaultLanguage);
                    return;
                }
            }
        }

        private bool IsMessage(IDeclaredElement declaredElement)
        {
            return GetMessage(declaredElement) != null;
        }

        private UnityMessage GetMessage(IDeclaredElement declaredElement)
        {
            var method = declaredElement as IMethod;
            return method != null ? myUnityApi.GetUnityMessage(method) : null;
        }

        private bool IsMessageParameter(IDeclaredElement declaredElement)
        {
            return GetMessageParameterOwner(declaredElement) != null;
        }

        private UnityMessage GetMessageParameterOwner(IDeclaredElement declaredElement)
        {
            var parameter = declaredElement as IParameter;
            return GetMessage(parameter?.ContainingParametersOwner);
        }
    }
}