using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Features.Inspections.CallHierarchy;
using JetBrains.ReSharper.Psi;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public abstract class ShowCallsBulbActionBase : IBulbAction
    {
        public void Execute(ISolution solution, ITextControl textControl)
        {
            //CGTD assert read lock

            var text = Text;

            if (!solution.GetPsiServices().Caches.WaitForCaches(text))
                return;

            var manager = CallHierarchyExplorerViewManager.GetInstance(solution);
            var filter = GetFilter(solution);
            var start = GetStartElement();
            var callType = CallsType;
            
            ShowCalls(manager, filter, start, callType);
        }

        private void ShowCalls(CallHierarchyExplorerViewManager manager,
            Func<CallHierarchyFindResult, bool> filter,
            DeclaredElementInstance<IClrDeclaredElement> start, 
            ShowCallsType callType)
        {
            switch (callType)
            {
                case ShowCallsType.INCOMING:
                    manager.ShowIncoming(start, filter);
                    break;
                case ShowCallsType.OUTGOING:
                    manager.ShowOutgoing(start, filter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public abstract string Text { get; }
        protected abstract Func<CallHierarchyFindResult, bool> GetFilter(ISolution solution);
        protected abstract DeclaredElementInstance<IClrDeclaredElement> GetStartElement();
        protected abstract ShowCallsType CallsType { get; }

        public virtual IconId Icon
        {
            get
            {
                switch (CallsType)
                {
                    case ShowCallsType.INCOMING:
                        return CallHierarchyIcons.CalledByArrow;
                    case ShowCallsType.OUTGOING:
                        return CallHierarchyIcons.CalledArrow;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ShowCallsType
    {
        INCOMING,
        OUTGOING
    }

    public static class ShowCallsUtil
    {
        public static IEnumerable<ShowCallsType> GetShowCallsEnumerable()
        {
            var result = FixedList.Of(ShowCallsType.INCOMING, ShowCallsType.OUTGOING);

            return result;
        }
    }
}