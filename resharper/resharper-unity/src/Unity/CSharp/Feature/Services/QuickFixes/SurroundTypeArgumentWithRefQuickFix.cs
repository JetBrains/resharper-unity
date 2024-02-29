using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;

[QuickFix]
public sealed class SurroundTypeArgumentWithRefQuickFix(MustBeSurroundedWithRefRwRoWarning warning)
    : IQuickFix
{
    private readonly ITypeUsage myTypeUsage = warning.TypeUsage;
    private readonly int myTypeUsageIndex = warning.Index;

    public IEnumerable<IntentionAction> CreateBulbItems()
    {
        yield return new SurroundTypeArgumentWithRefBulbActionQuickFix(myTypeUsage, myTypeUsageIndex, KnownTypes.RefRO)
            .ToQuickFixIntention();
        yield return new SurroundTypeArgumentWithRefBulbActionQuickFix(myTypeUsage, myTypeUsageIndex, KnownTypes.RefRW)
            .ToQuickFixIntention();
    }

    public bool IsAvailable(IUserDataHolder cache)
    {
        return myTypeUsage.IsValid();
    }
}

public class SurroundTypeArgumentWithRefBulbActionQuickFix(
    ITypeUsage typeUsage,
    int typeUsageIndex,
    IClrTypeName clrTypeName)
    : UnityScopedQuickFixBase
{
    public override string Text => string.Format(Strings.UnityDots_WrongQueryTypeArgument_SurroundWith,
        typeUsage.GetText(), clrTypeName.ShortName);

    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        var factory = CSharpElementFactory.GetInstance(typeUsage);

        var cSharpExpression = factory.CreateTypeUsage($"{clrTypeName.ShortName}<{typeUsage.GetText()}>");
        var typeArgumentList = typeUsage.GetContainingNode<ITypeArgumentList>();
        var typeArgumentNodes = Enumerable.ToArray(typeArgumentList!.TypeArgumentNodes);
        typeArgumentNodes[typeUsageIndex] = cSharpExpression;
        typeArgumentList.SetTypeArguments(typeArgumentNodes);

        return null;
    }

    protected override ITreeNode TryGetContextTreeNode()
    {
        return typeUsage;
    }
}