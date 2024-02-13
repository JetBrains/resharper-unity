using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeCompletion;

[Language(typeof(CSharpLanguage))]
public class OdinAssetListCodeCompletionProvider : AssetPathCompletionProviderBase
{
    public override bool IsAvailableInCurrentContext(CSharpCodeCompletionContext context, ICSharpLiteralExpression literalExpression)
    {
        var solution = context.NodeInFile.GetSolution();
        if (!OdinAttributeUtil.HasOdinSupport(solution))
            return false;
        
        var nodeInFile = context.NodeInFile;

        var propertyAssignment = nodeInFile.GetContainingNode<IPropertyAssignment>();
        if (propertyAssignment == null)
            return false;
        
        var attribute = AttributeNavigator.GetByPropertyAssignment(propertyAssignment);
        
        if (attribute == null)
            return false;

        var type = attribute.TypeReference?.Resolve().Result.DeclaredElement as ITypeElement;
        if (type == null)
            return false;

        if (!type.GetClrName().Equals(OdinKnownAttributes.AssetListAttribute))
            return false;


        var name = propertyAssignment.PropertyNameIdentifier.Name;
        return name.Equals("Path");
    }

    protected override RelativePath CalculateSearchPath(CSharpCodeCompletionContext context, ICSharpLiteralExpression stringLiteral,
        ITreeNode nodeInFile, out TextLookupRanges textLookupRanges)
    {
        return RelativePath.Parse(UnityYamlConstants.AssetsFolder)
            .Combine(base.CalculateSearchPath(context, stringLiteral, nodeInFile, out textLookupRanges));
    }
}