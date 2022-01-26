using JetBrains.Application.UI.Components;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Intentions.Options;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Options;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Naming;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Rider.Backend.Features.Settings;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.Settings
{
    // TODO: Get rid of this once RIDER-8339 is implemented
    [SolutionComponent]
    public class ChangeNamingRuleWindowProvider : RiderChangeNamingRuleWindowProvider, IChangeNamingRuleWindowProvider
    {
        private readonly UnityApi myUnityApi;

        public ChangeNamingRuleWindowProvider(ILogger logger, UnityApi unityApi)
            : base(logger)
        {
            myUnityApi = unityApi;
        }

        void IChangeNamingRuleWindowProvider.ShowSettingsDialog(string ruleName, NamingPolicy policyToChange,
                                                                IDeclaredElement declaredElement,
                                                                IPsiSourceFile psiSourceFile, ITextControl textControl,
                                                                NamingManager namingManager,
                                                                IUIApplicationSimple uiApplicationSimple,
                                                                ISolution solution)
        {
            if (declaredElement is IField field && myUnityApi.IsSerialisedField(field))
            {
                var optionsDialogOwner = solution.TryGetComponent<IOptionsDialogViewOwner>();
                if (optionsDialogOwner != null)
                {
                    Logger.Catch(() => optionsDialogOwner.Show(page: UnityOptionsPage.Name));
                    return;
                }
            }

            base.ShowSettingsDialog(ruleName, policyToChange, declaredElement, psiSourceFile, textControl,
                namingManager, uiApplicationSimple, solution);
        }
    }
}