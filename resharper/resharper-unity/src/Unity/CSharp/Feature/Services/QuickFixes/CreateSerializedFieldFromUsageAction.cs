using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CreateFromUsage;
using JetBrains.ReSharper.Feature.Services.CreateFromUsage.CreateDeclaration;
using JetBrains.ReSharper.Feature.Services.CreateFromUsage.DeclarationBuilders;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    public class CreateSerializedFieldFromUsageAction : CreateFieldFromUsageAction
    {
        public CreateSerializedFieldFromUsageAction(IReference reference)
            : base(reference)
        {
        }

        public override string Text
        {
            get
            {
                // Marked as [CanBeNull] in base type, but we set it in ctor
                var shortName = Reference!.GetName();

                var target = (ITypeTarget) GetTarget().NotNull("GetTarget() != null");
                var sourceTypeElement = target.Anchor?.GetContainingNode<ITypeDeclaration>()?.DeclaredElement;
                var targetTypeElement = target.TargetType;
                if (!targetTypeElement.Equals(sourceTypeElement))
                    shortName = target.TargetType.ShortName + "." + shortName;

                return string.Format(Strings.CreateSerializedFieldFromUsageAction_Text_Create_Unity_serialized_field___0__, shortName);
            }
        }

        protected override bool IsAvailableInternal()
        {
            var available = base.IsAvailableInternal();
            if (available && GetTarget() is ITypeTarget typeTarget)
            {
                var containingType = typeTarget.TargetType;
                var unityApi = containingType.GetSolution().GetComponent<UnityApi>();
                return unityApi.IsUnityType(containingType);
            }
            return false;
        }

        protected override IntentionResult ExecuteIntention(CreateFieldDeclarationContext context)
        {
            var intentionResult = FieldDeclarationBuilder.Create(context);
            if (intentionResult.ResultDeclaration is IFieldDeclaration fieldDeclaration)
            {
                AttributeUtil.AddAttributeToSingleDeclaration(fieldDeclaration, KnownTypes.SerializeField,
                    fieldDeclaration.GetPsiModule(), CSharpElementFactory.GetInstance(fieldDeclaration));

                var languageService = fieldDeclaration.Language.LanguageService().NotNull();
                var codeFormatter = languageService.CodeFormatter.NotNull();

                codeFormatter.Format(fieldDeclaration, CodeFormatProfile.GENERATOR);
            }
            return intentionResult;
        }
    }
}
