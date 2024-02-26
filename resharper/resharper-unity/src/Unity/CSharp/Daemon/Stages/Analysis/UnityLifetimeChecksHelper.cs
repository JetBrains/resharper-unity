#nullable enable
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[SolutionComponent]
public class UnityLifetimeChecksHelper
{
    private readonly IClrTypeName myNotDestroyedAttribute;
    
    public IProperty<bool> ForceLifetimeChecks { get; }
   

    public UnityLifetimeChecksHelper(Lifetime lifetime, IApplicationWideContextBoundSettingStore store)
    {
        ForceLifetimeChecks = store.BoundSettingsStore.GetValueProperty(lifetime, (UnitySettings s) => s.ForceLifetimeChecks);
        myNotDestroyedAttribute = new ClrTypeName("JetBrains.Annotations.NotDestroyedAttribute");
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
