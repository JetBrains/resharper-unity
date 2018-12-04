using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public class UnityEditorOccurrence : ReferenceOccurrence
    {
        private readonly IUnityYamlReference myUnityEventTargetReference;
        public UnityEditorOccurrence([NotNull] IUnityYamlReference unityEventTargetReference, IDeclaredElement element,
            OccurrenceType occurrenceType)
            : base(unityEventTargetReference, element, occurrenceType)
        {
            myUnityEventTargetReference = unityEventTargetReference;
        }

        public override bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            if (solution.GetComponent<ConnectionTracker>().LastCheckResult != UnityEditorState.Disconnected)
                return base.Navigate(solution, windowContext, transferFocus, tabOptions);
            
            var findRequestCreator = solution.GetComponent<UnityEditorFindRequestCreator>();
            return findRequestCreator.CreateRequestToUnity(myUnityEventTargetReference, true);
        }
    }
}