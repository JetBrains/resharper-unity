#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Common.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;

[QuickFix]
public class UnityObjectLifetimeCheckViaNullEqualityQuickFix(IEqualityExpression equalityExpression) : IQuickFix
{
    public UnityObjectLifetimeCheckViaNullEqualityQuickFix(UnityObjectLifetimeCheckViaNullEqualityWarning warning) : this(warning.Expression) { }

    public UnityObjectLifetimeCheckViaNullEqualityQuickFix(UnityObjectLifetimeCheckViaNullEqualityHintHighlighting highlighting) : this(highlighting.Expression) { }

    public IEnumerable<IntentionAction> CreateBulbItems()
    {
        yield return SimpleBulbItems.ReplaceCSharpExpression(equalityExpression, "Convert to check via implicit bool conversion", (factory, exp) =>
        {
            var template = exp.EqualityType == EqualityExpressionType.NE ? "$0" : "!$0";
            return factory.CreateExpression(template, GetUnityObjectOperand(exp));
        });
        yield return SimpleBulbItems.ReplaceCSharpExpression(equalityExpression, "Convert to regular null check", (factory, exp) =>
        {
            var template = exp.EqualityType == EqualityExpressionType.NE ? "$0 is not null" : "$0 is null";
            return factory.CreateExpression(template, GetUnityObjectOperand(exp)); 
        });
        yield break;

        static ICSharpExpression GetUnityObjectOperand(IEqualityExpression exp) => exp.LeftOperand.GetOperandThroughParenthesis().IsNullLiteral() ? exp.RightOperand : exp.LeftOperand;
    }

    public bool IsAvailable(IUserDataHolder cache) => equalityExpression.IsValid();
}