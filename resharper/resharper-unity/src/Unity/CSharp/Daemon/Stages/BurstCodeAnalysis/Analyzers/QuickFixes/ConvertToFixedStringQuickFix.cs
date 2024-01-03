using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Positions;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.QuickFixes
{
    [QuickFix]
    public class ConvertToFixedStringQuickFix : IQuickFix
    {
        private static readonly SubmenuAnchor ourFirstLevel =
            new(new InvisibleAnchor(IntentionsAnchors.QuickFixesAnchor, AnchorPosition.BeforePosition),
                SubmenuBehavior.Executable);

        private readonly IMultipleDeclarationMember myMultipleDeclarationMember;

        public ConvertToFixedStringQuickFix(BurstLocalStringVariableDeclarationWarning warning)
        {
            myMultipleDeclarationMember = warning.MultipleDeclarationMember;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            yield return new ConvertToFixedStringActionQuickFix(myMultipleDeclarationMember, null)
                .ToQuickFixIntention(ourFirstLevel);

            yield return new ConvertToFixedStringActionQuickFix(myMultipleDeclarationMember,
                KnownTypes.FixedString32Bytes).ToQuickFixIntention(ourFirstLevel);
            yield return new ConvertToFixedStringActionQuickFix(myMultipleDeclarationMember,
                KnownTypes.FixedString64Bytes).ToQuickFixIntention(ourFirstLevel);
            yield return new ConvertToFixedStringActionQuickFix(myMultipleDeclarationMember,
                KnownTypes.FixedString128Bytes).ToQuickFixIntention(ourFirstLevel);
            yield return new ConvertToFixedStringActionQuickFix(myMultipleDeclarationMember,
                KnownTypes.FixedString512Bytes).ToQuickFixIntention(ourFirstLevel);
            yield return new ConvertToFixedStringActionQuickFix(myMultipleDeclarationMember,
                KnownTypes.FixedString4096Bytes).ToQuickFixIntention(ourFirstLevel);
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            return myMultipleDeclarationMember.IsValid();
        }
    }

    public class ConvertToFixedStringActionQuickFix : QuickFixBase
    {
        public ConvertToFixedStringActionQuickFix(IMultipleDeclarationMember multipleDeclarationMember,
            [CanBeNull] ClrTypeName fixedStringClrTypeName)
        {
            MultipleDeclarationMember = multipleDeclarationMember;
            Text = string.Format(Strings.BurstLocalStringVariableDeclarationQuickFix,
                fixedStringClrTypeName?.ShortName ?? "FixedString");
            myFixedStringClrTypeName = fixedStringClrTypeName;
        }

        [CanBeNull] private readonly ClrTypeName myFixedStringClrTypeName;
        private IMultipleDeclarationMember MultipleDeclarationMember { get; }

        public override string Text { get; }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                var psiModule = MultipleDeclarationMember.GetPsiModule();

                var fixedStringCLRType =
                    myFixedStringClrTypeName ?? CalculateFixedStringCLRType(MultipleDeclarationMember);

                var fixedStringTypeElement = TypeFactory.CreateTypeByCLRName(fixedStringCLRType, psiModule);
                MultipleDeclarationMember.SetType(fixedStringTypeElement);

                return null;
            }
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return MultipleDeclarationMember.IsValid();
        }

        private static ClrTypeName CalculateFixedStringCLRType(IMultipleDeclarationMember multipleDeclarationMember)
        {
            var fixedStringCLRType = KnownTypes.FixedString4096Bytes;

            if (multipleDeclarationMember is not IInitializerOwnerDeclaration
                {
                    Initializer: IExpressionInitializer { Value: ICSharpLiteralExpression sharpLiteralExpression }
                })
                return fixedStringCLRType;

            var textLength = Encoding.UTF8.GetByteCount(sharpLiteralExpression.GetText()) - 4; // -4 for extra symbols 2x'"' 2x'\'

            fixedStringCLRType = textLength switch
            {
                <= 32 => KnownTypes.FixedString32Bytes,
                <= 64 => KnownTypes.FixedString64Bytes,
                <= 128 => KnownTypes.FixedString128Bytes,
                <= 512 => KnownTypes.FixedString512Bytes,
                _ => KnownTypes.FixedString4096Bytes
            };

            return fixedStringCLRType;
        }
    }
}