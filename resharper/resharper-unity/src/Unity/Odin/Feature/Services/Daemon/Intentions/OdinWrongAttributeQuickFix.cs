using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.Daemon.Intentions;

[QuickFix]
public class OdinWrongAttributeQuickFix : QuickFixBase
{
    private readonly OdinMemberWrongGroupingAttributeWarning myWarning;

    public OdinWrongAttributeQuickFix(OdinMemberWrongGroupingAttributeWarning warning)
    {
        myWarning = warning;
    }
    public override string Text => string.Format(Strings.OdinReplaceAttributeQuickFix, myWarning.ExpectShortName);
    
    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        var referenceName = CSharpElementFactory.GetInstance(myWarning.Attribute)
            .CreateReferenceName(myWarning.ExpectShortName);
        
        myWarning.Attribute.Name.ReplaceBy(referenceName);
        return null;
    }

    public override bool IsAvailable(IUserDataHolder cache) => myWarning.Attribute.IsValid();
}