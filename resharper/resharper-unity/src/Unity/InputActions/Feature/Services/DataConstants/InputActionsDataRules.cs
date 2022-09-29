using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Feature.Services.DataConstants
{
    // This enables Find Usages on the string literal value for the "name" JSON property. The string literal value
    // doesn't have an IDeclaration, so the default rules can't find any IDeclaredElements. We have to provide one
    [ShellComponent]
    public class InputActionsDataRules
    {
        public InputActionsDataRules(Lifetime lifetime, IActionManager actionManager)
        {
            actionManager.DataContexts.RegisterDataRule(lifetime, "InputActionsDeclaredElements",
                PsiDataConstants.DECLARED_ELEMENTS, GetDeclaredElementsFromContext);
        }

        private static ICollection<IDeclaredElement> GetDeclaredElementsFromContext(IDataContext dataContext)
        {
            var psiEditorView = dataContext.GetData(PsiDataConstants.PSI_EDITOR_VIEW);
            if (psiEditorView == null) return null;

            var psiView = psiEditorView.DefaultSourceFile.View<JsonNewLanguage>();
            var cache = dataContext.GetComponent<InputActionsCache>();
            
            foreach (var containingNode in psiView.ContainingNodes)
            {
                var sourceFile = containingNode.GetSourceFile();
                if (sourceFile == null || !sourceFile.IsInputActions())
                    continue;

                if (cache.ContainsOffset(sourceFile, containingNode))
                {
                    return new List<IDeclaredElement>
                    {
                        new InputActionsDeclaredElement(containingNode.GetText(), sourceFile,
                            containingNode.GetTreeStartOffset().Offset)
                    };
                }
                // var parent = (IJsonNewMember)containingNode.AsStringLiteralValue();
                // if (parent?.Parent?.Key == "name" && parent)
            }

            return null;
        }
    }
}