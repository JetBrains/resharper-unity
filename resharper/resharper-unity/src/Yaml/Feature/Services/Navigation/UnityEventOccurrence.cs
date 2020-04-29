using System.Drawing;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Presentation;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityEventOccurrence : UnityAssetOccurrence, IDeclaredElementOccurrence
    {
        public bool IsPrefabModification { get; }

        public UnityEventOccurrence(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, LocalReference attachedElementLocation, bool isPrefabModification)
            : base(sourceFile, declaredElement.CreateElementPointer(), attachedElementLocation)
        {
            IsPrefabModification = isPrefabModification;
            OccurrenceElement = new DeclaredElementEnvoy<IDeclaredElement>(declaredElement);
        }

        public override bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            using (ReadLockCookie.Create())
            {
                solution.GetPsiServices().Files.AssertAllDocumentAreCommitted();
                var declaredElement = DeclaredElementPointer.FindDeclaredElement();

                if (declaredElement != null)
                {
                    declaredElement.Navigate(transferFocus, windowContext, null, tabOptions);
                    return true;
                }

                return false;
            }
        }

        public override RichText GetDisplayText()
        {
            var declaredElement = DeclaredElementPointer.FindDeclaredElement();
            if (declaredElement == null)
                return "";
            
            var elementRichText = DeclaredElementMenuItemFormatter.FormatText(declaredElement, declaredElement.PresentationLanguage, out _);
            var grayTextStyle = TextStyle.FromForeColor(SystemColors.GrayText);
            var containerText = DeclaredElementPresenter.Format(declaredElement.PresentationLanguage,
                DeclaredElementMenuItemFormatter.ContainerPresentationStyle, declaredElement, EmptySubstitution.INSTANCE).Text;
            
            var richText = new RichText(containerText, grayTextStyle);

            return elementRichText + " " + richText + ", " + base.GetDisplayText();
        }

        public override IconId GetIcon()
        {
            if (IsPrefabModification)
                return UnityFileTypeThemedIcons.FileUnityPrefab.Id;
            return base.GetIcon();
        }

        public IDeclaredElementEnvoy OccurrenceElement { get; }
        public IDeclaredElementEnvoy DisplayElement => OccurrenceElement;
        public SearchTargetRole SearchTargetRole { get; set; }
    }
}