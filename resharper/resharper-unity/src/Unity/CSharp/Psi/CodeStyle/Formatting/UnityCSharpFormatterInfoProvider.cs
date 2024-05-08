#nullable enable

using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Calculated.Interface;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.Formatter;
using JetBrains.ReSharper.Psi.CSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.Formatting
{
    [Language(typeof(CSharpLanguage))]
    public class UnityCSharpFormatterInfoProvider : CSharpFormatterInfoProviderPart
    {
        public const int UnityPriority = 1_000_000;

        private readonly string[] myPossibleHeaderNames =
        {
            "UnityEngine.HeaderAttribute", "UnityEngine.Header", "HeaderAttribute", "Header"
        };

        public UnityCSharpFormatterInfoProvider(
            ISettingsSchema settingsSchema,
            ICalculatedSettingsSchema calculatedSettingsSchema,
            IThreading threading,
            Lifetime lifetime
        )
            : base(settingsSchema, calculatedSettingsSchema, threading, lifetime)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            HeaderAttributeRules();
        }

        private static string? GetSingleAttributeSectionName(ITreeNode node) =>
            node is IAttributeSection { Attributes: { SingleItem: { } attrSectionCandidate } }
                ? attrSectionCandidate.Name.GetText()
                : null;

        private string? GetSingleSectionAttributeListName(ITreeNode node) =>
            node is IAttributeSectionList { Sections: { SingleItem: { Attributes: { SingleItem: { } attrCandidate } } } }
                ? attrCandidate.Name.GetText()
                : null;

        private void HeaderAttributeRules()
        {
            var singleHeaderAttributeSection = Node().In(ElementType.ATTRIBUTE_SECTION).Satisfies((node, _) =>
                myPossibleHeaderNames.Contains(GetSingleAttributeSectionName(node.Node)) && node.Node.GetProject().IsUnityProject()).Obj;

            var singleHeaderAttributeSectionList = Node().In(ElementType.ATTRIBUTE_SECTION_LIST).Satisfies((node, _) =>
                myPossibleHeaderNames.Contains(GetSingleSectionAttributeListName(node.Node)) && node.Node.GetProject().IsUnityProject()).Obj;

            var multipleFieldDeclaration = Node().In(ElementBitsets.MULTIPLE_DECLARATION_BIT_SET
                .Except(ElementType.MULTIPLE_EVENT_DECLARATION)
                .Union(ElementType.ENUM_MEMBER_DECLARATION)).Obj;

            var attributeSectionFollowingHeader = Node().In(ElementType.ATTRIBUTE_SECTION).Satisfies((node, _) =>
            {
                // Default case: if node is the first section and it is not a Header section, we
                var attributeSectionList = AttributeSectionListNavigator.GetBySection(node.Node as IAttributeSection);
                if (attributeSectionList?.Sections.First() == node && !myPossibleHeaderNames.Contains(GetSingleAttributeSectionName(node.Node)))
                    return true;

                var previousSection = node.GetPreviousMeaningfulSibling();
                return previousSection != null && myPossibleHeaderNames.Contains(GetSingleAttributeSectionName(previousSection.Node)) && previousSection.Node.GetProject().IsUnityProject();
            }).Obj;

            // Blank lines after [Header]
            DescribeWithExternalKey<UnityCSharpFormattingSettingsKey, BlankLinesRule>()
                .Group(MinLineBreaks)
                .Name("EnforceBlankLinesAfterHeaderList")
                .Where(
                    Left().Is(singleHeaderAttributeSectionList)
                )
                .SwitchBlankLinesOnExternalKey(x => x.BLANK_LINES_AFTER_HEADER, true, BlankLineLimitKind.LimitMinimum)
                .Priority(UnityPriority)
                .Build();

            DescribeWithExternalKey<UnityCSharpFormattingSettingsKey, BlankLinesRule>()
                .Group(MinLineBreaks)
                .Name("EnforceBlankLinesAfterHeader")
                .Where(
                    Left().Is(singleHeaderAttributeSection)
                )
                .SwitchBlankLinesOnExternalKey(x => x.BLANK_LINES_AFTER_HEADER, true, BlankLineLimitKind.LimitMinimum)
                .Priority(UnityPriority)
                .Build();

            // Break the wrap region early, so the line break implemented by the next rule
            // won't be distributed to the rest of the wrap region.
            DescribeWithExternalKey<UnityCSharpFormattingSettingsKey, WrapRule>()
                .Name("BreakWrapAfterHeaderAttribute")
                .Where(
                    GrandParent().Is(multipleFieldDeclaration),
                    Left().Is(singleHeaderAttributeSection),
                    Right().HasType(ElementType.ATTRIBUTE_SECTION)
                )
                .SwitchOnExternalKey(x => x.BLANK_LINES_AFTER_HEADER,
                    When(true).Return(WrapType.BrokenRegion | WrapType.AllowEarlyBreak | WrapType.Chop)
                )
                .Priority(UnityPriority)
                .Build();

            DescribeWithExternalKey<UnityCSharpFormattingSettingsKey, FormattingRule>()
                .Group(LineBreaksRuleGroup)
                .Name("NewLineAfterHeaderAttributeSection")
                .Where(
                    Parent().In(ElementType.ATTRIBUTE_SECTION_LIST),
                    Left().Is(singleHeaderAttributeSection)
                )
                .SwitchOnExternalKey(x => x.ENFORCE_CUSTOM_HEADER_FORMATTING,
                    When(true).Return(IntervalFormatType.NewLine)
                )
                .Priority(UnityPriority)
                .Build();

            DescribeWithExternalKey<UnityCSharpFormattingSettingsKey, FormattingRule>()
                .Group(LineBreaksRuleGroup)
                .Name("NewLineAfterHeaderAttributeSection")
                .Where(
                    Parent().Is(multipleFieldDeclaration),
                    Left().Is(singleHeaderAttributeSectionList)
                )
                .SwitchOnExternalKey(x => x.ENFORCE_CUSTOM_HEADER_FORMATTING,
                    When(true).Return(IntervalFormatType.NewLine)
                )
                .Priority(UnityPriority)
                .Build();

            DescribeWithExternalKey<UnityCSharpFormattingSettingsKey, WrapRule>()
                .Name("CancelAttributeSectionListMultipleDeclaration")
                .Group(WrapRuleGroup)
                .Where(
                    Parent().Is(multipleFieldDeclaration),
                    Node().HasType(ElementType.ATTRIBUTE_SECTION_LIST))
                .SwitchOnExternalKey(x => x.ENFORCE_CUSTOM_HEADER_FORMATTING,
                    When(true).Switch(x => x.PLACE_FIELD_ATTRIBUTE_ON_SAME_LINE_EX,
                        When(PlaceOnSameLineAsOwner.ALWAYS).Switch(x => x.KEEP_EXISTING_ATTRIBUTE_ARRANGEMENT,
                            When(false).Return(WrapType.StartAtExternal)
                        )
                    )
                )
                .Priority(UnityPriority)
                .Build();

            DescribeWithExternalKey<UnityCSharpFormattingSettingsKey, WrapRule>()
                .Group(WrapRuleGroup)
                .Name("BeforeOrAfterHeaderAttribute")
                .Where(
                    GrandParent().Is(multipleFieldDeclaration),
                    Parent().In(ElementType.ATTRIBUTE_SECTION_LIST),
                    Left().Is(attributeSectionFollowingHeader)
                )
                .CloseNodeGetter(RegionRuleBase.GetLastSibling)
                .SwitchOnExternalKey(x => x.ENFORCE_CUSTOM_HEADER_FORMATTING,
                    When(true).Switch(x => x.PLACE_FIELD_ATTRIBUTE_ON_SAME_LINE_EX,
                        When(PlaceOnSameLineAsOwner.ALWAYS).Switch(x => x.KEEP_EXISTING_ATTRIBUTE_ARRANGEMENT,
                            When(false).Return(WrapType.StartAtExternal | WrapType.Chop |
                                               WrapType.LineBreakAfterIfMultiline)
                        )
                    )
                )
                .Priority(UnityPriority)
                .Build();

            DescribeWithExternalKey<UnityCSharpFormattingSettingsKey, WrapRule>()
                .Name("CancelAttributeOnTheSameLine")
                .Group(WrapRuleGroup)
                .Where(Node().Is(multipleFieldDeclaration))
                .SwitchOnExternalKey(x => x.ENFORCE_CUSTOM_HEADER_FORMATTING,
                    When(true).Switch(x => x.PLACE_FIELD_ATTRIBUTE_ON_SAME_LINE_EX,
                        When(PlaceOnSameLineAsOwner.IF_OWNER_IS_SINGLE_LINE)
                            .Switch(x => x.KEEP_EXISTING_ATTRIBUTE_ARRANGEMENT,
                                When(false).Return(WrapType.StartAtExternal))))
                .Priority(UnityPriority)
                .Build();

            DescribeWithExternalKey<UnityCSharpFormattingSettingsKey, WrapRule>()
                .Group(WrapRuleGroup)
                .Name("UnityAttributeOnTheSameLine")
                .Where(
                    GrandParent().Is(multipleFieldDeclaration),
                    Parent().In(ElementType.ATTRIBUTE_SECTION_LIST),
                    Left().Is(attributeSectionFollowingHeader)
                )
                .CloseNodeGetter((node, context) =>
                    new(context, AttributeSectionListNavigator.GetBySection(node.Node as IAttributeSection)?.Parent?.LastChild)
                )
                .SwitchOnExternalKey(x => x.ENFORCE_CUSTOM_HEADER_FORMATTING,
                    When(true).Switch(x => x.PLACE_FIELD_ATTRIBUTE_ON_SAME_LINE_EX,
                        When(PlaceOnSameLineAsOwner.IF_OWNER_IS_SINGLE_LINE)
                            .Switch(x => x.KEEP_EXISTING_ATTRIBUTE_ARRANGEMENT,
                                When(false).Return(WrapType.StartAtExternal | WrapType.Chop))))
                .Priority(UnityPriority)
                .Build();
        }
    }
}