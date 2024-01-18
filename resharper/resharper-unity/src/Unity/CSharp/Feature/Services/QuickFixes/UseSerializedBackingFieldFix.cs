using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.BulbActions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [MutuallyExclusiveAction(typeof(AutoPropertyToSerializedBackingFieldAction))]
    [QuickFix]
    public class UseSerializedBackingFieldFix : ModernQuickFixBase
    {
        [CanBeNull] private readonly IPropertyDeclaration myPropertyDeclaration;

        public UseSerializedBackingFieldFix([NotNull] NonAbstractAccessorWithoutBodyError error)
        {
            myPropertyDeclaration = error.TypeMemberDeclaration as IPropertyDeclaration;
        }

        public UseSerializedBackingFieldFix([NotNull] AutoPropertyMustOverrideAllAccessorsError error)
        {
            myPropertyDeclaration = error.Property as IPropertyDeclaration;
        }

        public override string Text => Strings.AutoPropertyToSerializedBackingFieldAction_Text_To_property_with_serialized_backing_field;

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return AutoPropertyToSerializedBackingFieldAction.IsAvailable(myPropertyDeclaration);
        }

        protected override IBulbActionCommand ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            if (myPropertyDeclaration == null) return null;
            return AutoPropertyToSerializedBackingFieldAction.Execute(myPropertyDeclaration, solution,
                CSharpElementFactory.GetInstance(myPropertyDeclaration));
        }
    }
}