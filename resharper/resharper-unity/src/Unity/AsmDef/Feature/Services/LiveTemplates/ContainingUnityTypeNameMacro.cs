using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.LiveTemplates
{
    public abstract class ContainingUnityTypeNameMacroBase : SimpleMacroImplementation
    {
        public override HotspotItems GetLookupItems(IHotspotContext context)
        {
            var document = context.ExpressionRange.Document;
            var solution = context.SessionContext.Solution;
            var unityApi = solution.TryGetComponent<UnityApi>();

            if (unityApi == null)
                return null;

            var sourceFile = document.GetPsiSourceFile(solution);
            if (sourceFile == null)
                return null;

            using (ReadLockCookie.Create())
            {
                solution.GetPsiServices().Files.CommitAllDocuments();
                var psiFile = sourceFile.GetPrimaryPsiFile();
                if (psiFile == null)
                    return null;

                var treeTextRange = psiFile.Translate(context.ExpressionRange);
                var tokenNode = psiFile.FindTokenAt(treeTextRange.StartOffset) as ITokenNode;

                if (tokenNode == null)
                    return null;

                var typeDeclaration = tokenNode.GetContainingNode<ITypeDeclaration>();
                while (typeDeclaration != null)
                {
                    if (unityApi.IsUnityType(typeDeclaration?.DeclaredElement))
                        break;

                    typeDeclaration = typeDeclaration.GetContainingNode<ITypeDeclaration>();
                }

                return typeDeclaration == null ? null : EvaluateName(typeDeclaration.DeclaredName);
            }
        }

        protected virtual HotspotItems EvaluateName(string declaredName)
        {
            return MacroUtil.SimpleEvaluateResult(declaredName);
        }
    }

    [MacroDefinition("unityParentTypeName",
        ResourceType = typeof(Strings),
        DescriptionResourceName = nameof(Strings.EvaluatesParentTypeName_Text),
        LongDescriptionResourceName = nameof(Strings.EvaluatesParentTypeName_Text))]
    public class ContainingUnityTypeNameMacroDef : SimpleMacroDefinition
    {
    }

    [MacroImplementation(Definition = typeof(ContainingUnityTypeNameMacroDef), ScopeProvider = typeof(PsiImpl))]
    public class ContainingUnityTypeNameMacroImpl : ContainingUnityTypeNameMacroBase
    {
    }

    [MacroDefinition("bakerNameBasedOnUnityParentTypeName",
        ResourceType = typeof(Strings),
        DescriptionResourceName = nameof(Strings.EvaluatesBakerParentTypeName_Text),
        LongDescriptionResourceName = nameof(Strings.EvaluatesBakerParentTypeName_Text))]
    public class BakerNameBasedOnUnityParentTypeNameDef : SimpleMacroDefinition
    {
    }

    [MacroImplementation(Definition = typeof(BakerNameBasedOnUnityParentTypeNameDef), ScopeProvider = typeof(PsiImpl))]
    public class BakerNameBasedOnUnityParentTypeNameImpl : ContainingUnityTypeNameMacroBase
    {
        private const string Authoring = "Authoring";

        protected override HotspotItems EvaluateName(string declaredName)
        {
            if (declaredName.EndsWith(Authoring, StringComparison.CurrentCultureIgnoreCase))
                declaredName = declaredName[..^Authoring.Length];

            return base.EvaluateName($"{declaredName}Baker");
        }
    }
}