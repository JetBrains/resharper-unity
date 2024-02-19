#nullable enable
using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Feature.Services.QuickFixes;

class SimplePsiTransactionBulbAction(/*Localized*/ string text, Func<ISolution, IProgressIndicator, Action<ITextControl>?> action) : BulbActionBase
{
    public override /*Localized*/ string Text => text;

    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress) => action(solution, progress);
}