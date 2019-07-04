using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Add 'Header' attribute",
        Description =
            "Add header for this property in the Unity Editor inspector")]
    public class AddHeaderAttributeAction : AddInspectorAttributeAction
    {
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.LowPriorityContextActionsAnchor, SubmenuBehavior.Executable);
        protected override IClrTypeName AttributeTypeName => KnownTypes.Header;

        public AddHeaderAttributeAction(ICSharpContextActionDataProvider dataProvider) : base(dataProvider, ourSubmenuAnchor)
        {
        }

        public override BulbActionBase GetActionForOne(IMultipleFieldDeclaration multipleFieldDeclaration, IFieldDeclaration fieldDeclaration, IPsiModule module,
            CSharpElementFactory elementFactory, IAttribute existingAttribute)
        {
            return new AddHEaderActionOne(multipleFieldDeclaration.Declarators.Count, fieldDeclaration, module);
        }

        public override BulbActionBase GetActionForAll(IMultipleFieldDeclaration multipleFieldDeclaration, IPsiModule module,
            CSharpElementFactory elementFactory, IAttribute existingAttribute)
        {
            return new AddSpaceActionAll(multipleFieldDeclaration, module, elementFactory);
        }

        private class AddHEaderActionOne : BulbActionBase
        {
            private readonly int myDeclaratorsCount;
            private readonly IFieldDeclaration myFieldDeclaration;
            private readonly IPsiModule myModule;

            public AddHEaderActionOne(int declaratorsCount, IFieldDeclaration fieldDeclaration, IPsiModule module)
            {
                myDeclaratorsCount = declaratorsCount;
                myFieldDeclaration = fieldDeclaration;
                myModule = module;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                return ExecuteHotspotSession(solution, myFieldDeclaration, myModule);
            }

            public override string Text => myDeclaratorsCount == 1 ? "Add 'Header'" : $"Annotate '{myFieldDeclaration.DeclaredName}' with 'Header' attribute";
        }
        
        private class AddSpaceActionAll : BulbActionBase
        {
            private readonly IMultipleFieldDeclaration myMultipleFieldDeclaration;
            private readonly IPsiModule myModule;

            public AddSpaceActionAll(IMultipleFieldDeclaration multipleFieldDeclaration, IPsiModule module,
                CSharpElementFactory elementFactory)
            {
                myMultipleFieldDeclaration = multipleFieldDeclaration;
                myModule = module;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var fieldDeclaration = (IFieldDeclaration) myMultipleFieldDeclaration.Declarators[0];
                return ExecuteHotspotSession(solution, fieldDeclaration, myModule);
            }

            public override string Text => "Annotate all fields with 'Header' attribute";
        }
        
        private static Action<ITextControl> ExecuteHotspotSession(ISolution solution, IFieldDeclaration fieldDeclaration, IPsiModule module)
        {
            AddAttribute(KnownTypes.Header, fieldDeclaration,
                new[] {new AttributeValue(new ConstantValue(fieldDeclaration.NameIdentifier.Name, module))}, module);


            var hotspotsRegistry = new HotspotsRegistry(solution.GetPsiServices());

            var attribute = fieldDeclaration.Attributes.First(t =>
                (t.TypeReference.Resolve().DeclaredElement as IClass).GetClrName().Equals(KnownTypes.Header));

            hotspotsRegistry.Register(new[] {attribute.Arguments[0]}, new NameSuggestionsExpression(new List<string>() {fieldDeclaration.NameIdentifier.Name}));

            return BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, DocumentOffset.InvalidOffset);
        }
    }
}