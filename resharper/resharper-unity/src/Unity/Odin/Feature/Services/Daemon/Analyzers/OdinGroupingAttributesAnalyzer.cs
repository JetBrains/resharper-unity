using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.Daemon.Analyzers;

[ElementProblemAnalyzer(typeof(IClassLikeDeclaration))]
public class OdinGroupingAttributesAnalyzer : UnityElementProblemAnalyzer<IClassLikeDeclaration>
{
    private readonly UnityTechnologyDescriptionCollector myTechnologyCollector;

    public OdinGroupingAttributesAnalyzer(UnityApi unityApi, UnityTechnologyDescriptionCollector technologyCollector) : base(unityApi)
    {
        myTechnologyCollector = technologyCollector;
    }

    protected override void Analyze(IClassLikeDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (!myTechnologyCollector.DiscoveredTechnologies.ContainsKey("Odin"))
            return;

        var classType = element.DeclaredElement;
        if (classType == null)
            return;
        
        bool hasOdinAttribute = false;
        foreach (var member in element.ClassMemberDeclarations)
        {
            hasOdinAttribute |= HasOdinAttribute(member);
            if (hasOdinAttribute)
                break;
        }
        
        if (!hasOdinAttribute)
            return;

        var existingGroup = new Dictionary<string, IClrTypeName>();
        var trie = new QualifiedNamesTrie<string>(false, '/');
        
        var grouping = OdinAttributeUtil.CollectGroupInfo(classType);

        var membersWithDefinedGroupWithDifferentAttribute = new LocalHashSet<ITypeMember>();
        foreach (var info in grouping)
        {
            trie.Add(info.GroupPath, info.GroupPath);

            var clrName = info.AttributeInstance.GetClrName();
            if (existingGroup.TryGetValue(info.GroupPath, out var attribute))
            {
                if (!attribute.Equals(clrName))
                {
                    membersWithDefinedGroupWithDifferentAttribute.Add(info.Member);
                }
            }
            else
            {
                existingGroup[info.GroupPath] = clrName;
            }
        }

        foreach (var member in element.ClassMemberDeclarations)
        {
            var declaredElement = member.DeclaredElement;

            var localList = new LocalList<string>();

            foreach (var memberAttribute in member.Attributes)
            {
                var memberAttributeInstance = memberAttribute.GetAttributeInstance();
                if (!OdinKnownAttributes.LayoutAttributes.TryGetValue(memberAttributeInstance.GetClrName(), out var parameterName))
                    continue;
                
                var groupPath = OdinAttributeUtil.GetMajorGroupPath(memberAttributeInstance);
                if (groupPath == null)
                    continue;

                localList.Add(groupPath);
                
                var sections = groupPath.Split('/');
                var currentSection = new StringBuilder(sections.Length);
                foreach (var section in sections)
                {
                    currentSection.Append(section);
                    var sectionToTest = currentSection.ToString();
                    if (trie.Find(sectionToTest) == null)
                    {
                        var argument = memberAttribute.Arguments.FirstOrDefault(t => parameterName.Equals(t.MatchingParameter?.Element.ShortName));

                        if (argument != null)
                        {
                            if (argument.Expression is ICSharpLiteralExpression expression
                                && expression.IsConstantValue() && expression.ConstantValue.IsString())
                            {
                                var literalStartOffset = expression.GetDocumentRange();
                                var highlightingOffset = new DocumentRange(literalStartOffset.Document,
                                    new TextRange(literalStartOffset.StartOffset.Offset + 1,
                                        literalStartOffset.StartOffset.Offset + 1 + currentSection.Length));
                                consumer.AddHighlighting(new OdinUnknownGroupingPathWarning(highlightingOffset, sectionToTest));
                            }
                        }
                        break;
                    }
                    currentSection.Append('/');
                }
                
                if (membersWithDefinedGroupWithDifferentAttribute.Contains(declaredElement))
                {
                    var expectedAttributeName = existingGroup[groupPath];
                    consumer.AddHighlighting(new OdinMemberWrongGroupingAttributeWarning(memberAttribute, memberAttribute.Name.GetDocumentRange(), expectedAttributeName.ShortName.RemoveEnd("Attribute")));
                }
            }

            if (localList.Count > 0)
            {
                var groupsToVerify = localList.ReadOnlyList().OrderBy().ToList();
                for (int i = 0; i < groupsToVerify.Count - 1; i++)
                {
                    if (!groupsToVerify[i + 1].StartsWith(groupsToVerify[i]))
                    {
                        // Show error in case:
                        // [BoxGroup("A/B")]
                        // [BoxGroup("A/C")]
                        // int x;
                        // Do not show error in case:
                        // [BoxGroup("A")]
                        // [BoxGroup("A/B")]
                        // [BoxGroup("A/B/C")]
                        // int x;
                        consumer.AddHighlighting(new OdinMemberPresentInMultipleGroupsWarning(member.GetNameDocumentRange()));
                        break;
                    }
                }
            }
        }
    }

    private bool HasOdinAttribute(IClassMemberDeclaration member)
    {
        foreach (var attribute in member.Attributes)
        {
            var type = attribute.GetAttributeInstance().GetClrName();
            if (OdinKnownAttributes.LayoutAttributes.TryGetValue(type, out _))
                return true;
        }
        return false;
    }
}