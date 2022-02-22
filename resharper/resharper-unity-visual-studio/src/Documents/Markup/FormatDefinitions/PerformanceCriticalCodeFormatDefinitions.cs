using System.ComponentModel.Composition;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

// ReSharper disable UnassignedField.Global
// Field is never assigned to, and will always have its default value null
#pragma warning disable 649

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Documents.Markup.FormatDefinitions
{
    [ClassificationType(ClassificationTypeNames = Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CostlyMethodHighlighterClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER;

        public CostlyMethodHighlighterClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = System.Windows.Media.Color.FromRgb(0xff, 0x75, 0x26);
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CostlyMethodInvocationClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION;

        public CostlyMethodInvocationClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = System.Windows.Media.Color.FromRgb(0xff, 0x75, 0x26);
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class NullComparisonClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = PerformanceHighlightingAttributeIds.NULL_COMPARISON;

        public NullComparisonClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = System.Windows.Media.Color.FromRgb(0xff, 0x75, 0x26);
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class CameraMainClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = PerformanceHighlightingAttributeIds.CAMERA_MAIN;

        public CameraMainClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = System.Windows.Media.Color.FromRgb(0xff, 0x75, 0x26);
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class InefficientMultiDimensionalArraysUsageClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = PerformanceHighlightingAttributeIds.INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE;

        public InefficientMultiDimensionalArraysUsageClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = System.Windows.Media.Color.FromRgb(0xff, 0x75, 0x26);
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }

    [ClassificationType(ClassificationTypeNames = Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    internal class InefficientMultiplicationOrderClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = PerformanceHighlightingAttributeIds.INEFFICIENT_MULTIPLICATION_ORDER;

        public InefficientMultiplicationOrderClassificationDefinition()
        {
            DisplayName = Name;
            ForegroundColor = System.Windows.Media.Color.FromRgb(0xff, 0x75, 0x26);
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }
}