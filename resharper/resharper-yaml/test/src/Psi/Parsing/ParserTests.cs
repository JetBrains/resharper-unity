using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Yaml.Psi.Parsing
{
  [TestFileExtension(TestYamlProjectFileType.YAML_EXTENSION)]
  public class ParserTests : ParserTestBase<YamlLanguage>
  {
    protected override string RelativeTestDataPath => @"Psi\Parsing";

    // 5 Characters
    // 5.1 Character Set
    // 5.2 Character Encodings
    // 5.3 Indicator Characters
    [TestCase("BlockStructureIndicators")]
    [TestCase("FlowCollectionIndicators")]
    [TestCase("CommentIndicator")]
    [TestCase("NodePropertyIndicators")]
    [TestCase("BlockScalarIndicators")]
    [TestCase("QuotedScalarIndicators")]
    [TestCase("DirectiveIndicator")]
    [TestCase("ReservedIndicators")]

    // 5.4 Line Breaks - pfft
    // 5.5 White Space Characters
    [TestCase("WhitespaceCharacters")]

    // 5.6 Miscellaneous Characters - might be worth it, depending on how I want to parse strings/numbers
    //     (Do I want numbers to be lexed as numbers, or post-parse resolved to numbers?)
    // 5.7 Escaped Characters - doesn't need to be lexed, just a part of strings
    //     Can add an inspection for correct escaped values

    // 6 Basic Structures
    // 6.1 Indentation Spaces - handled by indenting lexer

    // 6.2 Separation Spaces
    [TestCase("SeparationSpaces")]

    // 6.3 Line Prefixes - depends on how indentation is implemented
    // 6.4 Empty Lines - nothing to test, mostly a private production in the YAML grammar

    // 6.5 Line Folding
    [TestCase("LineFolding")]
    [TestCase("BlockFolding")]
    [TestCase("FlowFolding")]

    // 6.6 Comments
    [TestCase("SeparatedComment")]
    [TestCase("CommentLines")]
    [TestCase("MultiLineComments")]
    [TestCase("Comments")]
    [TestCase("NotComments")]

    // 6.7 Separation Lines - describes whitespace (inc. comments) between items
    //     Might be required, depending on how we parse indentation and flow elements

    // 6.8 Directives
    [TestCase("ReservedDirective")]

    // 6.8.1 YAML directive
    [TestCase("YamlDirective")]

    // 6.8.2 TAG directive
    [TestCase("TagDirective")]

    // 6.8.2.1 Tag handles
    [TestCase("PrimaryTagHandle")]
    [TestCase("SecondaryTagHandle")]
    [TestCase("NamedTagHandle")]

    // 6.8.2.2 Tag prefixes
    [TestCase("LocalTagPrefix")]
    [TestCase("GlobalTagPrefix")]

    // 6.9 Node Properties
    [TestCase("NodeProperties")]

    // 6.9.1 Node tags
    [TestCase("VerbatimTag")]
    [TestCase("InvalidVerbatimTag")]
    [TestCase("TagShorthands")]
    [TestCase("InvalidTagShorthands")]
    [TestCase("NonSpecificTag")]

    // 6.9.2 Node anchors
    [TestCase("NodeAnchors")]

    // 7 Flow Styles
    // 7.1 Alias Nodes
    [TestCase("AliasNodes")]

    // 7.2 Empty Nodes
    [TestCase("EmptyNodes")]
    [TestCase("CompletelyEmptyFlowNodes")]
    [TestCase("EmptyImplicitBlockValueWithTrailingWhitespace")]

    // 7.3 Flow Scalar Styles
    // 7.3.1 Double-quoted style
    [TestCase("DoubleQuotedImplicitKeys")]
    [TestCase("DoubleQuotedLineBreaks")]
    [TestCase("DoubleQuotedLines")]

    // 7.3.2 Single-quoted style
    [TestCase("SingleQuotedCharacters")]
    [TestCase("SingleQuotedImplicitKeys")]
    [TestCase("SingleQuotedLines")]
    [TestCase("SingleQuotedLinesWithSingleQuotedCharacters")]

    // 7.3.3 Plain style
    [TestCase("PlainCharacters")]
    [TestCase("PlainImplicitKeys")]
    [TestCase("PlainLines")]

    // 7.4 Flow Collection Styles
    // 7.4.1 Flow sequences
    [TestCase("FlowSequence")]
    [TestCase("FlowSequenceEntries")]

    // 7.4.2 Flow mappings
    [TestCase("FlowMappings")]
    [TestCase("FlowMappingEntries")]
    [TestCase("FlowMappingSeparateValues")]
    [TestCase("FlowMappingAdjacentValues")]
    [TestCase("SinglePairFlowMappings")]
    [TestCase("SinglePairExplicitEntry")]
    [TestCase("SinglePairImplicitEntries")]
    // Invalid implicit keys - see example 7.22. Just too lazy to create it right now :)

    // 7.5 Flow Nodes
    [TestCase("FlowContent")]
    [TestCase("FlowContentMultiline")]
    [TestCase("FlowNodes")]

    // 8 Block Styles
    // 8.1 Block Scalar Styles
    // 8.1.1 Block scalar headers
    [TestCase("BlockScalarHeader")]

    // 8.1.1.1 Block indentation indicator - depends on indentation implementation
    // 8.1.1.2 Block chomping indicator - possibly depends on indentation implementation, or parser
    // 8.1.2 Literal style - depends on indentation implementation
    // 8.1.3 Literal style - depends on indentation implementation

    // 8.2 Block Collection Styles
    // 8.2.1 Block sequences
    [TestCase("BlockSequence")]
    [TestCase("BlockSequenceEntryTypes")]

    // 8.2.2 Block mappings
    [TestCase("BlockMappings")]
    [TestCase("ExplicitBlockMappingEntries")]
    [TestCase("ImplicitBlockMappingEntries")]
    [TestCase("CompactBlockMappings")]

    // 8.2.3 Block nodes
    [TestCase("BlockNodeTypes")]
    [TestCase("BlockScalarNodes")]
    [TestCase("BlockCollectionNodes")]

    // 9 YAML Character Stream
    // 9.1 Documents
    // 9.1.1 Document prefix
    [TestCase("DocumentPrefix")]
    [TestCase("DocumentPrefixWithBom")]  // Shouldn't matter to us

    // 9.1.2 Document markers
    [TestCase("DocumentMarkers")]

    // 9.1.3 Bare documents
    [TestCase("BareDocuments")]

    // 9.1.4 Explicit documents
    [TestCase("ExplicitDocuments")]

    // 9.1.5 Directives documents
    [TestCase("DirectivesDocuments")]

    // 9.2 Streams
    [TestCase("Stream")]

    public void TestParser(string name) => DoOneTest(name);
  }
}