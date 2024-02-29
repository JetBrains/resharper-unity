#nullable enable
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[SolutionComponent]
public class UnityLifetimeChecksHelper(ISolution solution, HighlightingSettingsManager highlightingSettingsManager)
{
    private readonly IClrTypeName myNotDestroyedAttribute = new ClrTypeName("JetBrains.Annotations.NotDestroyedAttribute");

    public bool IsNullPatternMatchingWarningEnabled(IPsiSourceFile sourceFile) => highlightingSettingsManager.GetConfigurableSeverity(UnityObjectNullPatternMatchingWarning.HIGHLIGHTING_ID, sourceFile, sourceFile.GetLazySettingsStoreWithEditorConfig(solution)) >= Severity.HINT;

    public bool IsLifetimeBypassPattern(IPattern pattern)
    {
        return pattern.GetPatternThroughNegations(out _).GetPatternThroughParentheses() switch
        {
            IVarPattern or IDiscardPattern => false,
            IBinaryPattern binaryPattern => IsLifetimeBypassPattern(binaryPattern.LeftPattern) || IsLifetimeBypassPattern(binaryPattern.RightPattern),
            _ => true
        };
    } 

    public bool CanBeDestroyed(ICSharpExpression expression)
    {
        if (!UnityTypeUtils.IsUnityObject(expression.Type()))
            return false;
        var hasNotDestroyedAttribute = GetAttributeSet(expression) is {} attributesSet && attributesSet.HasAttributeInstance(myNotDestroyedAttribute, AttributesSource.All);
        return !hasNotDestroyedAttribute;
    }

    private static IAttributesSet? GetAttributeSet(ICSharpExpression expression)
    {
        return expression switch
        {
            IInvocationExpression { InvokedExpression: IReferenceExpression { Reference: var reference } } when reference.Resolve().DeclaredElement is IMethod method => method.ReturnTypeAttributes,
            IReferenceExpression { Reference: var reference } => reference.Resolve().DeclaredElement switch
            {
                IAttributesOwner attributesOwner => attributesOwner,
                ILocalVariableDeclaration { Initializer: IExpressionInitializer { Value: { } value } } => GetAttributeSet(value),
                ISingleVariableDesignation { Parent: IVarPattern { Parent: IIsExpression { Operand: {} operand } } } => GetAttributeSet(operand),
                _ => null
            },
            _ => null
        };
    }
}
