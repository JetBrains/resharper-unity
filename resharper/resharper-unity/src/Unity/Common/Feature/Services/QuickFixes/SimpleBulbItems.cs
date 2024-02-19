#nullable enable
using System;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Feature.Services.QuickFixes;

public static class SimpleBulbItems
{
    public static IntentionAction ReplaceCSharpExpression<T>(T expression, string text, Func<CSharpElementFactory, T, ICSharpExpression> replaceExpressionFactory) where T : ICSharpExpression => 
        new SimpleReplaceCSharpExpressionBulbAction<T>(expression, text, replaceExpressionFactory).ToQuickFixIntention();
}