using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Positions;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.QuickFixes
{
    [QuickFix]
    public class AspectFieldsWrongTypeQuickFix : IQuickFix
    {
        private readonly IFieldDeclaration myFieldDeclaration;

        public AspectFieldsWrongTypeQuickFix(AspectWrongFieldsTypeWarning warning)
        {
            myFieldDeclaration = warning.FieldDeclaration;
        }

        private static readonly SubmenuAnchor ourFirstLevel =
            new(new InvisibleAnchor(IntentionsAnchors.QuickFixesAnchor, AnchorPosition.BeforePosition),
                SubmenuBehavior.Executable);

        private static readonly SubmenuAnchor ourSecondLevel =
            new(new InvisibleAnchor(IntentionsAnchors.QuickFixesAnchor, AnchorPosition.BeforePosition),
                SubmenuBehavior.Executable);


        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            yield return new AspectFieldsWrongTypeBulbActionQuickFix(myFieldDeclaration, KnownTypes.RefRO)
                .ToQuickFixIntention(ourFirstLevel);

            var isIEnableableComponentInheritor = myFieldDeclaration.Type is IDeclaredType fieldDeclarationType &&
                                                  fieldDeclarationType.GetTypeElement()
                                                      .DerivesFrom(KnownTypes.IEnableableComponent);
            if (isIEnableableComponentInheritor)
            {
                yield return new AspectFieldsWrongTypeBulbActionQuickFix(myFieldDeclaration, KnownTypes.EnabledRefRO)
                    .ToQuickFixIntention(ourFirstLevel);
            }

            yield return new AspectFieldsWrongTypeBulbActionQuickFix(myFieldDeclaration, KnownTypes.RefRW)
                .ToQuickFixIntention(ourSecondLevel);

            if (isIEnableableComponentInheritor)
            {
                yield return new AspectFieldsWrongTypeBulbActionQuickFix(myFieldDeclaration, KnownTypes.EnabledRefRW)
                    .ToQuickFixIntention(ourSecondLevel);
            }
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            return myFieldDeclaration.IsValid();
        }
    }


    public class AspectFieldsWrongTypeBulbActionQuickFix : QuickFixBase
    {
        private readonly IFieldDeclaration myFieldDeclaration;
        private readonly IClrTypeName myWrapperTypeName;

        public AspectFieldsWrongTypeBulbActionQuickFix(IFieldDeclaration fieldDeclaration,
            IClrTypeName wrapperTypeName)
        {
            myFieldDeclaration = fieldDeclaration;
            myWrapperTypeName = wrapperTypeName;
        }
        
        

        public override string Text =>
            string.Format(Strings.UnityDots_AspectWrongFieldsType_WrapWith, myWrapperTypeName.ShortName);

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var module = myFieldDeclaration.GetPsiModule();
            var (wrapperTypeElement, _) = TypeFactory.CreateTypeByCLRName(myWrapperTypeName, module);

            var componentType = myFieldDeclaration.Type;
            var substitution = EmptySubstitution.INSTANCE.Extend(wrapperTypeElement!.TypeParameters[0], componentType);
            var wrapperWithSubstitution =
                TypeFactory.CreateType(wrapperTypeElement, substitution, NullableAnnotation.NotAnnotated);

            myFieldDeclaration.SetType(wrapperWithSubstitution);
            return null;
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return myFieldDeclaration.IsValid();
        }

        // public void Execute(ISolution solution, ITextControl textControl)
        // {
        //     
        //    
        // }
        //
        //
        //
        // protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        // {
        //  
        //
        //     return null;
        // }
        //
        // protected override ITreeNode TryGetContextTreeNode()
        // {
        //     return myFieldDeclaration;
        // }
    }
}