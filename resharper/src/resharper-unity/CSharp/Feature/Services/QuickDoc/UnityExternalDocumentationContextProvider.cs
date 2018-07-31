#if RIDER

using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickDoc
{
    [ContainsContextConsumer]
    public class UnityExternalDocumentationContextProvider
    {
        private static readonly DeclaredElementPresenterStyle MSDN_STYLE =
            new DeclaredElementPresenterStyle
            {
                ShowEntityKind = EntityKindForm.NONE,
                ShowName = NameStyle.QUALIFIED,
                ShowTypeParameters = TypeParameterStyle.CLR
            };

        [CanBeNull, AsyncContextConsumer]
        public static Action ProcessDataContext(
            [NotNull] Lifetime lifetime,
            [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))] IPsiDocumentRangeView psiDocumentRangeView,
            UnityHost host,
            UnityApi unityApi)
        {
            var unityName = GetUnityName(psiDocumentRangeView, unityApi);

            // This is called only if the process finished while the context is still valid
            return () => { host.PerformModelAction(rd => rd.ExternalDocContext.Value = unityName); };
        }

        [NotNull]
        private static string GetUnityName(IPsiDocumentRangeView psiDocumentRangeView, UnityApi unityApi)
        {
            var psiView = psiDocumentRangeView.View<CSharpLanguage>();
            if (psiView.ContainingNodes.All(n => !n.IsFromUnityProject()))
                return string.Empty;

            if (!(FindDeclaredElement(psiView) is IClrDeclaredElement element))
                return string.Empty;

            var unityName = GetUnityEventFunctionName(element, unityApi);
            if (unityName != null)
                return unityName;

            return GetFullyQualifiedUnityName(element);
        }

        [CanBeNull]
        private static string GetUnityEventFunctionName([NotNull] IDeclaredElement element, UnityApi unityApi)
        {
            var method = element as IMethod;
            if (method == null && element is IParameter parameter)
                method = parameter.ContainingParametersOwner as IMethod;

            if (method == null)
                return null;

            var unityEventFunction = unityApi.GetUnityEventFunction(method);
            if (unityEventFunction == null)
                return null;

            return unityEventFunction.TypeName + "." + element.ShortName;
        }

        private static string GetFullyQualifiedUnityName(IClrDeclaredElement element)
        {
            var moduleName = element.Module.Name;
            if (moduleName.StartsWith("UnityEngine") || moduleName.StartsWith("UnityEditor"))
                return DeclaredElementPresenter.Format(KnownLanguage.ANY, MSDN_STYLE, element);
            return string.Empty;
        }

        private static IDeclaredElement FindDeclaredElement([NotNull] IPsiView psiView)
        {
            var referenceExpression = psiView.GetSelectedTreeNode<IReferenceExpression>();
            if (referenceExpression != null)
            {
                return referenceExpression.Reference.Resolve().DeclaredElement;
            }

            var identifier = psiView.GetSelectedTreeNode<ICSharpIdentifier>();
            if (identifier != null)
            {
                var referenceName = ReferenceNameNavigator.GetByNameIdentifier(identifier);
                if (referenceName != null)
                    return referenceName.Reference.Resolve().DeclaredElement;

                var declarationUnderCaret =
                    FieldDeclarationNavigator.GetByNameIdentifier(identifier) ??
                    PropertyDeclarationNavigator.GetByNameIdentifier(identifier) ??
                    MethodDeclarationNavigator.GetByNameIdentifier(identifier) ??
                    ConstructorDeclarationNavigator.GetByTypeName(identifier) ??
                    CSharpTypeDeclarationNavigator.GetByNameIdentifier(identifier) ??
                    EventDeclarationNavigator.GetByNameIdentifier(identifier) ??
                    ConstantDeclarationNavigator.GetByNameIdentifier(identifier) ??
                    VariableDeclarationNavigator.GetByNameIdentifier(identifier);

                return declarationUnderCaret?.DeclaredElement;
            }

            var predefinedTypeUsage = psiView.GetSelectedTreeNode<IPredefinedTypeUsage>();
            return predefinedTypeUsage?.ScalarPredefinedTypeName.Reference.Resolve().DeclaredElement;
        }
    }
}

#endif