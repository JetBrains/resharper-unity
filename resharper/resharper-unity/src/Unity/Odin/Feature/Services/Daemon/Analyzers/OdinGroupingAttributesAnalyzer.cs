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
using JetBrains.ReSharper.Psi.Resolve.Managed;
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
        var memberToGroup = new Dictionary<ITypeMember, string>();
        var trie = new QualifiedNamesTrie<string>(false, '/');
        
        var grouping = OdinAttributeUtil.CollectGroupInfo(classType);

        var membersWithSeveralGroups = new LocalHashSet<ITypeMember>();
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

            if (memberToGroup.TryGetValue(info.Member, out var groupName))
            {
                if (!groupName.Equals(info.GroupPath))
                {
                    membersWithSeveralGroups.Add(info.Member);
                }
            }
            else
            {
                memberToGroup[info.Member] = info.GroupPath;
            }
        }

        foreach (var member in element.ClassMemberDeclarations)
        {
            var groupPath = memberToGroup.GetValueSafe(member.DeclaredElement);
            if (groupPath == null)
                continue;

            var sections = groupPath.Split('/');
            var currentSection = new StringBuilder(sections.Length);
            foreach (var section in sections)
            {
                currentSection.Append(section);
                var sectionToTest = currentSection.ToString();
                if (trie.Find(sectionToTest) == null)
                {
                    var attribute = GetAttributeByGroupingPath(member, groupPath).FirstOrDefault();
                    if (attribute != null)
                    {
                        var parameterName = OdinKnownAttributes.LayoutAttributes[attribute.GetAttributeInstance().GetClrName()];
                        var argument = attribute.Arguments.FirstOrDefault(t => parameterName.Equals(t.MatchingParameter?.Element.ShortName));

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
                    }
                    break;
                }
                currentSection.Append('/');
            }
         

            var declaredElement = member.DeclaredElement;
            if (membersWithSeveralGroups.Contains(declaredElement))
            {
                consumer.AddHighlighting(new OdinMemberPresentInMultipleGroupsWarning(member.GetNameDocumentRange()));
            }

            if (membersWithDefinedGroupWithDifferentAttribute.Contains(declaredElement))
            {
                var expectedAttributeName = existingGroup[groupPath];
                
                var attribute = GetAttributeByGroupingPath(member, groupPath).FirstOrDefault(t => !t.GetAttributeInstance().GetClrName().Equals(expectedAttributeName));
                if (attribute != null)
                {
                    consumer.AddHighlighting(new OdinMemberWrongGroupingAttributeWarning(attribute.Name.GetDocumentRange(), expectedAttributeName.ShortName.RemoveEnd("Attribute")));
                }
            }
        }
    }

    private IEnumerable<IAttribute> GetAttributeByGroupingPath(IClassMemberDeclaration classMemberDeclaration, string groupPath)
    {
        var result = new List<IAttribute>();
        foreach (var attribute in classMemberDeclaration.Attributes)
        {
            var path = OdinAttributeUtil.GetGroupPath(attribute.GetAttributeInstance());
            if (groupPath.Equals(path))
                result.Add(attribute);
        }

        return result;
    }

    private bool HasOdinAttribute(IClassMemberDeclaration member)
    {
        foreach (var attribute in member.Attributes)
        {
            var type = attribute.TypeReference?.Resolve().DeclaredElement as ITypeElement;
            if (type == null)
                continue;
            
            if (OdinKnownAttributes.LayoutAttributes.TryGetValue(type.GetClrName(), out _))
                return true;
        }
        return false;
    }
}