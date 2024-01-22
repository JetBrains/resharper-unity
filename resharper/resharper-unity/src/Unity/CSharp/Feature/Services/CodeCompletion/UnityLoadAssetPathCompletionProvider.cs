#nullable enable

using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    ///     ex. AssetDatabase.LoadAssetAtPath(assetPath:"...")
    ///     assetPath could be "Assets/.../file.ext"
    ///     or "Packages/com.companyname.package-name/Folder/.../file.ext"
    [Language(typeof(CSharpLanguage))]
    public class UnityLoadAssetPathCompletionProvider : AssetPathCompletionProviderBase
    {
        public override bool IsAvailableInCurrentContext(CSharpCodeCompletionContext context, ICSharpLiteralExpression literalExpression)
        {
            var nodeInFile = context.NodeInFile;

            var argument = nodeInFile.GetContainingNode<IArgument>();
            if (argument == null)
                return false;

            var matchingParameter = argument.MatchingParameter;
            if (matchingParameter == null)
                return false;

            var (parameter, _) = matchingParameter;
            if (parameter == null)
                return false;

            var attributes = parameter.GetAttributeInstances(KnownTypes.AssetPathAttribute, true);
            if (attributes.Count == 0)
                return false;

            return true;
        }
    }
}