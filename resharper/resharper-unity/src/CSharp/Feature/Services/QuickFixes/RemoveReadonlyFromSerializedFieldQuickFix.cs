using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class RemoveReadonlyFromSerializedFieldQuickFix : IQuickFix
    {
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.QuickFixesAnchor, SubmenuBehavior.Executable);

        private readonly IAttribute myAttribute;
        private readonly TreeNodeCollection<IFieldDeclaration> myFieldDeclarations;
        private readonly IMultipleFieldDeclaration myMultipleFieldDeclaration;

        public RemoveReadonlyFromSerializedFieldQuickFix(RedundantSerializeFieldAttributeWarning highlighting)
        {
            myAttribute = highlighting.Attribute;
            var attributeSectionList = AttributeSectionListNavigator.GetByAttribute(myAttribute);
            myMultipleFieldDeclaration = MultipleFieldDeclarationNavigator.GetByAttributes(attributeSectionList);
            myFieldDeclarations = FieldDeclarationNavigator.GetByAttribute(myAttribute);
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var readonlyCount = myFieldDeclarations.Count(fd => fd.IsReadonly);
            switch (readonlyCount)
            {
                case 0:
                    return EmptyList<IntentionAction>.Enumerable;

                case 1:
                    return new RemoveAllReadonly(myMultipleFieldDeclaration).ToQuickFixIntentions();

                default:
                    var list = new List<IntentionAction>();
                    list.Add(new RemoveAllReadonly(myMultipleFieldDeclaration).ToQuickFixIntention(ourSubmenuAnchor));
                    foreach (var fieldDeclaration in myFieldDeclarations)
                    {
                        if (fieldDeclaration.IsReadonly)
                            list.Add(new RemoveOneReadonly(fieldDeclaration).ToQuickFixIntention(ourSubmenuAnchor));
                    }

                    return list;
            }
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            return myAttribute.IsValid() && myFieldDeclarations.Any(fd => fd.IsReadonly);
        }

        private class RemoveAllReadonly : BulbActionBase
        {
            private readonly IMultipleFieldDeclaration myFieldDeclarations;

            public RemoveAllReadonly(IMultipleFieldDeclaration fieldDeclarations)
            {
                myFieldDeclarations = fieldDeclarations;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                ModifiersUtil.SetReadonly(myFieldDeclarations, false);
                return null;
            }

            public override string Text => myFieldDeclarations.Declarators.Count > 1
                ? "Make all fields non-readonly"
                : "Make field non-readonly";
        }

        private class RemoveOneReadonly : BulbActionBase
        {
            private readonly IFieldDeclaration myFieldDeclaration;

            public RemoveOneReadonly(IFieldDeclaration fieldDeclaration)
            {
                myFieldDeclaration = fieldDeclaration;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                myFieldDeclaration.SetReadonly(false);
                return null;
            }

            public override string Text => $"Make '{myFieldDeclaration.DeclaredName}' non-readonly";
        }
    }
}