#if !RIDER

using System.ComponentModel.Composition;
using System.Windows.Media;
using JetBrains.Platform.VisualStudio.SinceVs10.TextControl.Markup.FormatDefinitions;
using JetBrains.ReSharper.Plugins.Unity.Cg.Daemon;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

// ReSharper disable UnassignedField.Global

// Field is never assigned to, and will always have its default value null
#pragma warning disable 649

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.FormatDefinitions
{
    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CgKeywordClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = CgHighlightingAttributeIds.KEYWORD;

        public CgKeywordClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = Color.FromRgb(0, 0, 0xE0);
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CgNumberClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = CgHighlightingAttributeIds.NUMBER;

        public CgNumberClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = Colors.Black;
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CgFieldIdentifierClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = CgHighlightingAttributeIds.FIELD_IDENTIFIER;

        public CgFieldIdentifierClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = Colors.Purple;
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CgFunctionIdentifierClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = CgHighlightingAttributeIds.FUNCTION_IDENTIFIER;

        public CgFunctionIdentifierClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = Colors.DarkCyan;
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CgTypeIdentifierClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = CgHighlightingAttributeIds.TYPE_IDENTIFIER;

        public CgTypeIdentifierClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = Colors.DarkBlue;
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CgVariableIdentifierClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = CgHighlightingAttributeIds.VARIABLE_IDENTIFIER;

        public CgVariableIdentifierClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = Colors.DarkBlue;
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CgLineCommentClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = CgHighlightingAttributeIds.LINE_COMMENT;

        public CgLineCommentClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = Color.FromRgb(0x57, 0xA6, 0x4A);
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CgDelimitedCommentClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = CgHighlightingAttributeIds.DELIMITED_COMMENT;

        public CgDelimitedCommentClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = Color.FromRgb(0x57, 0xA6, 0x4A);
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CgPreprocessorLineContentClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = CgHighlightingAttributeIds.PREPPROCESSOR_LINE_CONTENT;

        public CgPreprocessorLineContentClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = Colors.Purple;
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }
}

#endif