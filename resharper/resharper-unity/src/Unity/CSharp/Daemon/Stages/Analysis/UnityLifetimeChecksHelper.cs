#nullable enable
using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[SolutionComponent]
public class UnityLifetimeChecksHelper(ISolution solution, HighlightingSettingsManager highlightingSettingsManager)
{
    private readonly IClrTypeName myNotDestroyedAttribute = new ClrTypeName("JetBrains.Annotations.NotDestroyedAttribute");

    public bool IsNullPatternMatchingWarningEnabled(IPsiSourceFile sourceFile) => highlightingSettingsManager.GetConfigurableSeverity(UnityObjectNullPatternMatchingWarning.HIGHLIGHTING_ID, sourceFile, sourceFile.GetLazySettingsStoreWithEditorConfig(solution)) >= Severity.HINT;

    public void AddNullPatternMatchingWarnings(IPattern? pattern, IHighlightingConsumer consumer)
    {
        Stack<IPattern>? pendingPatterns = null;
        do
        {
            ITreeNode? node = null;
            switch (pattern)
            {
                case IParenthesizedPattern { Pattern: { } operand }:
                    pattern = operand;
                    continue;
                case INegatedPattern { Pattern: { } negatedOperand }:
                    pattern = negatedOperand;
                    continue;
                case IBinaryPattern { LeftPattern: {} leftPattern, RightPattern: var rightPattern }:
                    pattern = leftPattern;
                    if (rightPattern != null)
                        (pendingPatterns ??= new Stack<IPattern>()).Push(rightPattern);
                    continue;
                case IBinaryPattern { RightPattern: { } rightPattern }:
                    pattern = rightPattern;
                    continue;
                case IConstantOrTypePattern constantOrTypePattern:
                    node = constantOrTypePattern.Expression;
                    break;
                case IRecursivePattern recursivePattern:
                    node = (ITreeNode?)recursivePattern.TypeUsage ?? recursivePattern.PropertyPatternClause?.LBrace;
                    break;
                case ITypePattern typePattern:
                    node = typePattern.TypeUsage;
                    break;
            }
            
            if (node != null)
                consumer.AddHighlighting(new UnityObjectNullPatternMatchingWarning(node));

            pattern = pendingPatterns?.TryPop();
        } while (pattern != null);
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
