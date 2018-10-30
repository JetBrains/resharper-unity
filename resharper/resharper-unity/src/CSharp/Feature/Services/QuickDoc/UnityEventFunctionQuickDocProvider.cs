using System;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.Application.UI.Help;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel.DataContext;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Providers;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickDoc
{
    // Priority must be less than QuickDocLocalSymbolProvider and QuickDocTypeMemberProvider
    [QuickDocProvider(-1)]
    public class UnityEventFunctionQuickDocProvider : IQuickDocProvider
    {
        private readonly ISolution mySolution;
        private readonly UnityApi myUnityApi;
        private readonly DocumentManager myDocumentManager;
        private readonly QuickDocTypeMemberProvider myQuickDocTypeMemberProvider;
        private readonly HelpSystem myHelpSystem;
        private readonly ITheming myTheming;

        public UnityEventFunctionQuickDocProvider(ISolution solution, UnityApi unityApi,
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
            if (!project.IsUnityProject()) return false;

            var declaredElements = context.GetData(PsiDataConstants.DECLARED_ELEMENTS);
            return declaredElements != null && declaredElements.Any(e => IsEventFunction(e) || IsParameterForEventFunction(e as IParameter));
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
                var eventFunction = GetEventFunction(element);
                if (eventFunction != null)
                {
                    var presenter = new UnityEventFunctionQuickDocPresenter(eventFunction, element, myQuickDocTypeMemberProvider,
                        myTheming, myHelpSystem);
                    resolved(presenter, defaultLanguage);
                    return;
                }

                var eventFunctionForParameter = GetEventFunctionFromParameter(element as IParameter);
                if (eventFunctionForParameter != null)
                {
                    var presenter = new UnityEventFunctionQuickDocPresenter(eventFunctionForParameter, element.ShortName, element,
                        myQuickDocTypeMemberProvider, myTheming, myHelpSystem);
                    resolved(presenter, defaultLanguage);
                    return;
                }
            }
        }

        private bool IsEventFunction(IDeclaredElement declaredElement)
        {
            return GetEventFunction(declaredElement) != null;
        }

        private UnityEventFunction GetEventFunction(IDeclaredElement declaredElement)
        {
            var method = declaredElement as IMethod;
            return method != null ? myUnityApi.GetUnityEventFunction(method) : null;
        }

        private bool IsParameterForEventFunction(IParameter parameter)
        {
            return GetEventFunctionFromParameter(parameter) != null;
        }

        private UnityEventFunction GetEventFunctionFromParameter(IParameter parameter)
        {
            return GetEventFunction(parameter?.ContainingParametersOwner);
        }
    }
}