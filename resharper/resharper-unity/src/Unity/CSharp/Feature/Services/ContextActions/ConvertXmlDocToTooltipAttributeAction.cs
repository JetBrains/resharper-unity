using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        ResourceType = typeof(Strings), NameResourceName = nameof(Strings.ConvertXmlDocToTooltipAttributeAction_Name), 
        DescriptionResourceName = nameof(Strings.ConvertXmlDocToTooltipAttributeAction_Description)
        )]
    public class ConvertXmlDocToTooltipAttributeAction : IContextAction
    {
        private static readonly IAnchor ourAnchor = new SubmenuAnchor(IntentionsAnchors.LowPriorityContextActionsAnchor,
            SubmenuBehavior.Executable);

        private readonly ICSharpContextActionDataProvider myDataProvider;

        public ConvertXmlDocToTooltipAttributeAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (!myDataProvider.Project.IsUnityProject())
                return false;

            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();
            var multipleFieldDeclaration = myDataProvider.GetSelectedElement<IMultipleFieldDeclaration>();

            // We only need to check the first declared field for attributes/serialisable-ness
            if (multipleFieldDeclaration?.DeclaratorsEnumerable.FirstOrDefault() is not IFieldDeclaration
                firstFieldDeclaration || firstFieldDeclaration.DeclaredElement == null)
            {
                return false;
            }

            var hasTooltipAttribute = firstFieldDeclaration.GetAttribute(KnownTypes.TooltipAttribute) != null;
            if (hasTooltipAttribute)
                return false;

            var hasXml = multipleFieldDeclaration.DocCommentBlock != null;

            if (unityApi.IsSerialisedField(firstFieldDeclaration.DeclaredElement) == SerializedFieldStatus.NonSerializedField || !hasXml) return false;

            var psi = ((ICSharpDocCommentBlock) multipleFieldDeclaration.DocCommentBlock).GetXmlPsi();
            foreach (var innerTag in psi.XmlFile.InnerTags)
            {
                if (innerTag.GetTagName() == "summary")
                    return true;
            }

            return false;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var fieldDeclaration = myDataProvider.GetSelectedElement<IMultipleFieldDeclaration>();
            var isCaretOnXmlDoc = myDataProvider.GetSelectedElement<IDocCommentBlock>() != null;
            return new AddTooltipAttributeAction(fieldDeclaration, myDataProvider.ElementFactory, isCaretOnXmlDoc)
                .ToContextActionIntentions(ourAnchor);
        }

        private class AddTooltipAttributeAction : BulbActionBase
        {
            private readonly IMultipleFieldDeclaration myFieldDeclaration;
            private readonly CSharpElementFactory myElementFactory;
            private readonly string mySummary;
            private readonly bool myIsCaretOnXml;
            private readonly bool myDestructiveConvert;

            public AddTooltipAttributeAction(IMultipleFieldDeclaration fieldDeclaration,
                                             CSharpElementFactory elementFactory,
                                             bool isCaretOnXml)
            {
                myFieldDeclaration = fieldDeclaration;
                myElementFactory = elementFactory;
                myIsCaretOnXml = isCaretOnXml;

                var xmlPsi = ((ICSharpDocCommentBlock)fieldDeclaration.DocCommentBlock)
                    .NotNull("(ICSharpDocCommentBlock) fieldDeclaration.DocCommentBlock != null")
                    .GetXmlPsi();

                // If we've just got a single summary tag, we'll convert it. If there are more tags (e.g. <remarks>),
                // we'll add the tooltip and leave the XML docs in place
                myDestructiveConvert = xmlPsi.XmlFile.InnerTags.Count == 1;
                mySummary = string.Empty;
                foreach (var innerTag in xmlPsi.XmlFile.InnerTags)
                {
                    if (innerTag.GetTagName() == "summary")
                    {
                        mySummary = innerTag.InnerValue;
                        break;
                    }
                }
            }

            public override string Text
            {
                get
                {
                    return (myCaretOnXml: myIsCaretOnXml, myDestructiveConvert) switch
                    {
                        (true, true) => Strings.AddTooltipAttributeAction_Text_Convert_to__Tooltip__attribute,
                        (true, false) => Strings.AddTooltipAttributeAction_Text_Add__Tooltip__attribute,
                        (false, true) => Strings.AddTooltipAttributeAction_Text_Convert_XML_doc_to__Tooltip__attribute,
                        (false, false) => Strings.AddTooltipAttributeAction_Text_Add__Tooltip__attribute_from_XML_doc
                    };
                }
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var module = myFieldDeclaration.GetPsiModule();
                var values = new[] { new AttributeValue(ConstantValue.String(mySummary, module)) };
                AttributeUtil.AddAttributeToEntireDeclaration(myFieldDeclaration, KnownTypes.TooltipAttribute,
                    values, null, module, myElementFactory);

                if (myDestructiveConvert && myFieldDeclaration.DocCommentBlock != null)
                {
                    using(WriteLockCookie.Create(myFieldDeclaration.DocCommentBlock.IsPhysical()))
                        ModificationUtil.DeleteChild(myFieldDeclaration.DocCommentBlock);
                }

                return null;
            }
        }
    }
}