#nullable enable
using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Feature.Services.QuickFixes;

public class SimpleReplaceCSharpExpressionBulbAction<T>(T expression, string text, Func<CSharpElementFactory, T, ICSharpExpression> replaceExpressionFactory) : BulbActionBase where T : ICSharpExpression
{
    public override string Text => text;
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        var factory = CSharpElementFactory.GetInstance(expression);
        expression.ReplaceBy(replaceExpressionFactory(factory, expression));
        return null;
    }
}
