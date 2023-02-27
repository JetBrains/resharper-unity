using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Navigation.NavigationExtensions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    public class OpenDotsSourceGeneratedFileBulbAction : IReadOnlyBulbAction
    {
        [NotNull] private readonly IClassLikeDeclaration myClassLikeDeclaration;

        public OpenDotsSourceGeneratedFileBulbAction(string text, [NotNull] IClassLikeDeclaration classLikeDeclaration)
        {
            Text = text;
            myClassLikeDeclaration = classLikeDeclaration;
        }

        public bool IsReadOnly => true;
        public string Text { get; }

        public void Execute(ISolution solution, ITextControl textControl)
        {
            var firstOrDefault = myClassLikeDeclaration.DeclaredElement!.GetDeclarations()
                .FirstOrDefault(d => d != myClassLikeDeclaration);
            if (firstOrDefault == null)
                return;

            var psiSourceFile = firstOrDefault.GetSourceFile();
            psiSourceFile.Navigate(new TextRange(0), true);
        }
    }
}