﻿using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Components;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.DataConstants
{
    // This enables Find Usages on the string literal value for the "name" JSON property. The string literal value
    // doesn't have an IDeclaration, so the default rules can't find any IDeclaredElements. We have to provide one
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class AsmDefDataRules : IUnityProjectFolderLazyComponent
    {
        public AsmDefDataRules(Lifetime lifetime, IActionManager actionManager)
        {
            actionManager.DataContexts.RegisterDataRule(lifetime, "AsmDefDeclaredElements",
                PsiDataConstants.DECLARED_ELEMENTS, GetDeclaredElementsFromContext);
        }

        private static ICollection<IDeclaredElement> GetDeclaredElementsFromContext(IDataContext dataContext)
        {
            var psiEditorView = dataContext.GetData(PsiDataConstants.PSI_EDITOR_VIEW);
            if (psiEditorView == null) return null;

            var psiView = psiEditorView.DefaultSourceFile.View<JsonNewLanguage>();
            foreach (var containingNode in psiView.ContainingNodes)
            {
                var sourceFile = containingNode.GetSourceFile();
                if (sourceFile == null || !sourceFile.IsAsmDef())
                    continue;

                if (containingNode.IsNamePropertyValue())
                {
                    var nameDeclaredElement = dataContext.GetComponent<AsmDefCache>().GetNameDeclaredElement(sourceFile);
                    if (nameDeclaredElement != null)
                    {
                        return new List<IDeclaredElement>
                        {
                            nameDeclaredElement
                        };
                    }
                }
            }

            return null;
        }
    }
}