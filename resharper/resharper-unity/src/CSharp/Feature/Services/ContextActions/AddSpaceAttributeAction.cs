using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Add 'Space' attribute",
        Description =
            "Add space before this property in the Unity Editor inspector")]
    public class AddSpaceAttributeAction : AddInspectorAttributeAction
    {
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);
        protected override IClrTypeName AttributeTypeName => KnownTypes.Space;

        public AddSpaceAttributeAction(ICSharpContextActionDataProvider dataProvider) : base(dataProvider, ourSubmenuAnchor)
        {
        }

        public override BulbActionBase GetActionForOne(IMultipleFieldDeclaration multipleFieldDeclaration, IFieldDeclaration fieldDeclaration, IPsiModule module,
            CSharpElementFactory elementFactory, IAttribute existingAttribute)
        {
            return new AddSpaceActionOne(multipleFieldDeclaration.Declarators.Count, fieldDeclaration, module, elementFactory);
        }

        public override BulbActionBase GetActionForAll(IMultipleFieldDeclaration multipleFieldDeclaration, IPsiModule module,
            CSharpElementFactory elementFactory, IAttribute existingAttribute)
        {
            return new AddSpaceActionAll(multipleFieldDeclaration, module, elementFactory);
        }

        private class AddSpaceActionOne : BulbActionBase
        {
            private readonly int myDeclaratorsCount;
            private readonly IFieldDeclaration myFieldDeclaration;
            private readonly IPsiModule myModule;
            private readonly CSharpElementFactory myElementFactory;

            public AddSpaceActionOne(int declaratorsCount, IFieldDeclaration fieldDeclaration, IPsiModule module,
                CSharpElementFactory elementFactory)
            {
                myDeclaratorsCount = declaratorsCount;
                myFieldDeclaration = fieldDeclaration;
                myModule = module;
                myElementFactory = elementFactory;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                AttributeUtil.AddAttributeToSingleDeclaration(myFieldDeclaration, KnownTypes.Space, myModule, myElementFactory);

                return null;
            }

            public override string Text => myDeclaratorsCount == 1 ? "Add 'Space'" : $"Annotate '{myFieldDeclaration.DeclaredName}' with 'Space' attribute";
        }
        
        private class AddSpaceActionAll : BulbActionBase
        {
            private readonly IMultipleFieldDeclaration myMultipleFieldDeclaration;
            private readonly IPsiModule myModule;
            private readonly CSharpElementFactory myElementFactory;

            public AddSpaceActionAll(IMultipleFieldDeclaration multipleFieldDeclaration, IPsiModule module,
                CSharpElementFactory elementFactory)
            {
                myMultipleFieldDeclaration = multipleFieldDeclaration;
                myModule = module;
                myElementFactory = elementFactory;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var fieldDeclaration = (IFieldDeclaration) myMultipleFieldDeclaration.Declarators[0];
                AttributeUtil.AddAttributeToAllDeclarations(fieldDeclaration, KnownTypes.Space,
                    myModule, myElementFactory);

                return null;
            }

            public override string Text => "Annotate all fields with 'Space' attribute";
        }
    }
}