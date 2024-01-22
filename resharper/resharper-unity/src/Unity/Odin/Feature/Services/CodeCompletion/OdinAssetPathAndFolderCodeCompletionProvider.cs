using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeCompletion;

[Language(typeof(CSharpLanguage))]
public class OdinAssetPathAndFolderCodeCompletionProvider : AssetPathCompletionProviderBase
{
    public override bool IsAvailableInCurrentContext(CSharpCodeCompletionContext context, ICSharpLiteralExpression literalExpression)
    {
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

        var name = propertyAssignment.PropertyNameIdentifier.Name;

        if (type.GetClrName().Equals(OdinKnownAttributes.AssetSelectorAttribute))
        {
            return name.Equals("Paths");
        }
        
        
        if (type.GetClrName().Equals(OdinKnownAttributes.FilePathAttribute) || 
            type.GetClrName().Equals(OdinKnownAttributes.FolderPathAttribute))
        {
            return name.Equals("ParentFolder");
        }
        
        return false;
    }
}