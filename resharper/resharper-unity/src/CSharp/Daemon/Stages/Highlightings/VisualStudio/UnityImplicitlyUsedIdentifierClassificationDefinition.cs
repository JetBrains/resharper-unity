using System.ComponentModel.Composition;
using JetBrains.Platform.VisualStudio.SinceVs10.TextControl.Markup.FormatDefinitions;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

// ReSharper disable UnassignedField.Global
// Field is never assigned to, and will always have its default value null
#pragma warning disable 649

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.VisualStudio
{
    [ClassificationType(ClassificationTypeNames = Name)]
    [Order(After = VsAnalysisPriorityClassificationDefinition.Name,
        Before = VsHighlightPriorityClassificationDefinition.Name)]
    [Export(typeof(EditorFormatDefinition))]
    [Name(Name)]
    [DisplayName(Name)]
    [UserVisible(true)]
    public class UnityImplicitlyUsedIdentifierClassificationDefinition : ClassificationFormatDefinition
    {
        private const string Name = UnityHighlightingAttributeIds.UNITY_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE;

        public UnityImplicitlyUsedIdentifierClassificationDefinition()
        {
            DisplayName = Name;
            IsBold = true;
        }

        [Export, Name(Name), BaseDefinition("formal language")]
        internal ClassificationTypeDefinition ClassificationTypeDefinition;
    }
}