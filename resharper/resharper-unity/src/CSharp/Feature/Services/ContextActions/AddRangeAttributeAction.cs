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
        Name = "Add 'Range' attribute",
        Description =
            "Add range for this property in the Unity Editor inspector")]
    public class AddRangeAttributeAction : AddInspectorAttributeAction
    {
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);
        protected override IClrTypeName AttributeTypeName => KnownTypes.Range;

        public AddRangeAttributeAction(ICSharpContextActionDataProvider dataProvider) : base(dataProvider, ourSubmenuAnchor)
        {
        }

        public override BulbActionBase GetActionForOne(IMultipleFieldDeclaration multipleFieldDeclaration, IFieldDeclaration fieldDeclaration, IPsiModule module,
            CSharpElementFactory elementFactory, IAttribute existingAttribute)
        {
            return new AddRangeActionOne(multipleFieldDeclaration.Declarators.Count, fieldDeclaration, module);
        }

        public override BulbActionBase GetActionForAll(IMultipleFieldDeclaration multipleFieldDeclaration, IPsiModule module,
            CSharpElementFactory elementFactory, IAttribute existingAttribute)
        {
            return new AddRangeActionAll(multipleFieldDeclaration, module, elementFactory);
        }

        private class AddRangeActionOne : BulbActionBase
        {
            private readonly int myDeclaratorsCount;
            private readonly IFieldDeclaration myFieldDeclaration;
            private readonly IPsiModule myModule;

            public AddRangeActionOne(int declaratorsCount, IFieldDeclaration fieldDeclaration, IPsiModule module)
            {
                myDeclaratorsCount = declaratorsCount;
                myFieldDeclaration = fieldDeclaration;
                myModule = module;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                return ExecuteHotspotSession(solution, myFieldDeclaration, myModule);
            }

            public override string Text => myDeclaratorsCount == 1 ? "Add 'Range'" : $"Annotate '{myFieldDeclaration.DeclaredName}' with 'Range' attribute";
        }
        
        private class AddRangeActionAll : BulbActionBase
        {
            private readonly IMultipleFieldDeclaration myMultipleFieldDeclaration;
            private readonly IPsiModule myModule;

            public AddRangeActionAll(IMultipleFieldDeclaration multipleFieldDeclaration, IPsiModule module,
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

            public override string Text => "Annotate all fields with 'Range' attribute";
        }
        
        private static Action<ITextControl> ExecuteHotspotSession(ISolution solution, IFieldDeclaration fieldDeclaration, IPsiModule module)
        {
            AddAttribute(KnownTypes.Range, fieldDeclaration,
                new[] {new AttributeValue(new ConstantValue(0, module)), new AttributeValue(new ConstantValue(1, module)),}, module);


            var hotspotsRegistry = new HotspotsRegistry(solution.GetPsiServices());

            var attribute = fieldDeclaration.Attributes.First(t =>
                (t.TypeReference.Resolve().DeclaredElement as IClass).GetClrName().Equals(KnownTypes.Range));

            hotspotsRegistry.Register(new[] {attribute.Arguments[0]}, new NameSuggestionsExpression(new List<string>() {"0"}));
            hotspotsRegistry.Register(new[] {attribute.Arguments[1]}, new NameSuggestionsExpression(new List<string>() {"1"}));

            return BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, DocumentOffset.InvalidOffset);
        }
    }
}