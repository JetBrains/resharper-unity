using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Intentions.CreateDeclaration;
using JetBrains.ReSharper.Feature.Services.Intentions.Impl.DeclarationBuilders;
using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Special;

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
                var shortName = Reference.GetName();

                var target = (ITypeTarget) GetTarget();
                var sourceTypeElement =
                    target.Anchor.GetContainingNode<ITypeDeclaration>(true).IfNotNull(_ => _.DeclaredElement);
                var targetTypeElement = target.TargetType;
                if (!targetTypeElement.Equals(sourceTypeElement))
                    shortName = target.TargetType.ShortName + "." + shortName;

                return $"Create Unity serialized field '{shortName}'";
            }
        }

        protected override bool IsAvailableInternal()
        {
            var available = base.IsAvailableInternal();
            if (available)
            {
                var typeTarget = GetTarget() as ITypeTarget;
                if (typeTarget != null)
                {
                    var containingType = typeTarget.TargetType;
                    var unityApi = containingType.GetSolution().GetComponent<UnityApi>();
                    return unityApi.IsUnityType(containingType);
                }
            }
            return available;
        }

        protected override IntentionResult ExecuteIntention(CreateFieldDeclarationContext context)
        {
            var intentionResult = FieldDeclarationBuilder.Create(context);
            var fieldDeclaration = intentionResult.ResultDeclaration as IFieldDeclaration;
            if (fieldDeclaration != null)
            {
                var attributeType = TypeFactory.CreateTypeByCLRName("UnityEngine.SerializeField", fieldDeclaration.GetPsiModule());
                var factory = CSharpElementFactory.GetInstance(fieldDeclaration);
                var attribute = factory.CreateAttribute(attributeType.GetTypeElement());
                fieldDeclaration.AddAttributeBefore(attribute, null);

                var languageService = fieldDeclaration.Language.LanguageService().NotNull();
                var codeFormatter = languageService.CodeFormatter.NotNull();

                codeFormatter.Format(fieldDeclaration, CodeFormatProfile.GENERATOR);
            }
            return intentionResult;
        }
    }
}