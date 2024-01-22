using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.BulbActions;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;

[QuickFix]
public class UnityFixFloatingPointComparingQuickFix : FixFloatingPointComparingFix
{
    private readonly ISolution mySolution;

    private static readonly IAnchor ourTopFixesAnchor = new InvisibleAnchor(IntentionsAnchors.QuickFixesAnchor);

    public override IEnumerable<IntentionAction> CreateBulbItems() => this.ToQuickFixIntentions(ourTopFixesAnchor);

    public UnityFixFloatingPointComparingQuickFix([NotNull] FloatingPointEqualityComparisonWarning warning) : base(warning)
    {
        mySolution = warning.Expression.GetSolution();
    }

    public override string Text => Strings.UnityFixFloatingPointComparison_Text;


    public override bool IsAvailable(IUserDataHolder cache)
    {
        return base.IsAvailable(cache)
               && IsFloatComparison()
               && IsInUnityEnvironment();
    }

    private bool IsInUnityEnvironment()
    {
        return mySolution.HasUnityReference();
    }

    private bool IsFloatComparison()
    {
        var operatorReference = myEqualityExpression.Reference.NotNull();
        var signOperator = (ISignOperator)operatorReference.Resolve().DeclaredElement.NotNull();
        return signOperator.Parameters[0].Type.IsFloat();
    }


    protected override IBulbActionCommand GenerateComparisonCode(ISolution solution, CSharpElementFactory factory,
        bool isEqualityCheck)
    {
        var symbolCache = solution.GetPsiServices().Symbols;
        var symbolScope =
            symbolCache.GetSymbolScope(myEqualityExpression.PsiModule, withReferences: true, caseSensitive: true);

        var systemMathElement = symbolScope.GetTypeElementByCLRName(KnownTypes.Mathf);
        if (systemMathElement == null) return null;

        var relational = factory.CreateExpression(
            "$0.Approximately($1, $2)",
            systemMathElement, myEqualityExpression.LeftOperand, myEqualityExpression.RightOperand);

        myEqualityExpression.ReplaceBy(relational);

        return null;
    }
}