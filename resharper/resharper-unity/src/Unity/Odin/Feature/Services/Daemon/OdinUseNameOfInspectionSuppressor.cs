using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.Daemon;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class OdinUseNameOfInspectionSuppressor : UseNameofExpressionAnalyzer.IUseNameOfInspectionSuppressor
{
    public bool IsSuppressed(ICSharpLiteralExpression literalExpression)
    {
        var argument = CSharpArgumentNavigator.GetByValue(literalExpression);
        var attribute = AttributeNavigator.GetByArgument(argument);
        var clrName = (attribute?.TypeReference?.Resolve().DeclaredElement as ITypeElement)?.GetClrName();
        if (clrName == null)
            return false;

        return clrName.FullName.StartsWith(OdinKnownAttributes.OdinNamespace);
    }
}