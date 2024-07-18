using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.References.Members;

public class OdinMemberReferenceFactory : IReferenceFactory
{
    private readonly UnityTechnologyDescriptionCollector myCollector;

    public OdinMemberReferenceFactory(UnityTechnologyDescriptionCollector collector)
    {
        myCollector = collector;
    }

    public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
    {
        if (!OdinAttributeUtil.HasOdinSupport(myCollector))
            return ReferenceCollection.Empty;
        
        if (element is not ICSharpLiteralExpression expression)
            return ReferenceCollection.Empty;

        var argument = CSharpArgumentNavigator.GetByValue(expression);
        if (argument == null)
            return ReferenceCollection.Empty;
        
        if (!expression.IsConstantValue())
            return ReferenceCollection.Empty;

        if (!expression.ConstantValue.IsString())
            return ReferenceCollection.Empty;

        var attribute = AttributeNavigator.GetByArgument(argument);
        if (attribute == null)
            return ReferenceCollection.Empty;

        var typeElement = attribute.GetContainingTypeElement();
        if (typeElement == null)
            return ReferenceCollection.Empty;

        var clrName = (attribute.TypeReference?.Resolve().DeclaredElement as ITypeElement)?.GetClrName();
        if (clrName == null)
            return ReferenceCollection.Empty;
        
        if (!clrName.FullName.StartsWith(OdinKnownAttributes.OdinNamespace))
            return ReferenceCollection.Empty;

        var stringValue = expression.ConstantValue.AsString() ?? "";
        if (stringValue.IsNullOrEmpty())
            return ReferenceCollection.Empty;

        var subLiterals = ExtractReferences(stringValue);

        var references = new LocalList<IReference>();
        foreach (var (name, startOffset, endOffset) in subLiterals)
        {
            references.Add(new OdinMemberReference(attribute.GetContainingTypeElement(), expression, name, startOffset, endOffset));
        }

        if (stringValue.Length == 0 || stringValue[0] != '@' && stringValue[0] != '$')
        {
            if (OdinKnownAttributes.AttributesWithMemberCompletion.TryGetValue(clrName, out var possibleNames))
            {
                if (possibleNames.Contains(argument.MatchingParameter?.Element.ShortName))
                {
                    references.Add(new OdinRegularMemberReference(attribute.GetContainingTypeElement(), expression,
                    stringValue, 1, 1 + stringValue.Length));
                }
            }
        }

        var collection = new ReferenceCollection(references.ReadOnlyList());
        return ResolveUtil.ReferenceSetsAreEqual(collection, oldReferences) ? oldReferences : collection;
    }

    private IEnumerable<(string memberName, int startOffset, int endOffset)> ExtractReferences(string stringValue)
    {
        var parts = stringValue.Split('/');

        // for '"' synbol
        var index = 1;
        foreach (var part in parts)
        {
            if (part.Length > 0 && part[0] == '$')
            {
                yield return (part.Substring(1), index + 1, index + part.Length);
            }

            index += part.Length + 1;
        }
    }

    public bool HasReference(ITreeNode element, IReferenceNameContainer names)
    {
        if (!OdinAttributeUtil.HasOdinSupport(myCollector))
            return false;
        
        if (element is not ICSharpLiteralExpression expression)
            return false;

        var argument = CSharpArgumentNavigator.GetByValue(expression);
        if (argument == null)
            return false;
        
        if (!expression.IsConstantValue())
            return false;

        if (!expression.ConstantValue.IsString())
            return false;

        var attribute = AttributeNavigator.GetByArgument(argument);
        if (attribute == null)
            return false;

        var typeElement = attribute.GetContainingTypeElement();
        if (typeElement == null)
            return false;

        return true;
    }
}