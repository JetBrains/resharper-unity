using System;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Help;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel.DataContext;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Providers;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Descriptions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickDoc
{
    // Uses the Unity IElementDocumentProvider to get the description for a Unity element, and formats it as QuickDoc.
    // We cannot rely on QuickDocDescriptionProvider because it doesn't include all details, such as web link. More
    // importantly, it's registered with a greater priority than QuickDocTypeMemberProvider and
    // QuickDocLocalSymbolProvider which will always try to handle a type member or variable, without fall back to other
    // quick doc providers. We register with a lower priority so we get chance to handle Unity elements first.
    [QuickDocProvider(-1)]
    public class UnityElementQuickDocProvider : IQuickDocProvider
    {
        private readonly ISolution mySolution;
        private readonly UnityApi myUnityApi;
        private readonly DocumentManager myDocumentManager;
        private readonly UnityElementDescriptionProvider myDescriptionProvider;
        private readonly QuickDocTypeMemberProvider myQuickDocTypeMemberProvider;
        private readonly HelpSystem myHelpSystem;
        private readonly XmlDocHtmlPresenter myPresenter;

        public UnityElementQuickDocProvider(ISolution solution,
                                            UnityApi unityApi,
                                            DocumentManager documentManager,
                                            UnityElementDescriptionProvider descriptionProvider,
                                            QuickDocTypeMemberProvider quickDocTypeMemberProvider,
                                            HelpSystem helpSystem,
                                            XmlDocHtmlPresenter presenter)
        {
            mySolution = solution;
            myUnityApi = unityApi;
            myDocumentManager = documentManager;
            myDescriptionProvider = descriptionProvider;
            myQuickDocTypeMemberProvider = quickDocTypeMemberProvider;
            myHelpSystem = helpSystem;
            myPresenter = presenter;
        }

        public bool CanNavigate(IDataContext context)
        {
            var project = context.GetData(ProjectModelDataConstants.PROJECT);
            if (!project.IsUnityProject()) return false;

            var declaredElements = context.GetData(PsiDataConstants.DECLARED_ELEMENTS);
            if (declaredElements == null)
                return false;

            foreach (var declaredElement in declaredElements)
            {
                if (!RichTextBlock.IsNullOrEmpty(myDescriptionProvider.GetElementDescription(declaredElement,
                    DeclaredElementDescriptionStyle.FULL_STYLE, declaredElement.PresentationLanguage)))
                {
                    return true;
                }
            }

            return false;
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
                var description = myDescriptionProvider.GetElementDescription(element,
                    DeclaredElementDescriptionStyle.FULL_STYLE, defaultLanguage);
                if (description != null && !RichTextBlock.IsNullOrEmpty(description))   // No annotations, sigh
                {
                    var presenter = new UnityElementQuickDocPresenter(element, description.Text, myUnityApi,
                        myQuickDocTypeMemberProvider, myPresenter, myHelpSystem);
                    resolved(presenter, defaultLanguage);
                    return;
                }
            }
        }
    }
}