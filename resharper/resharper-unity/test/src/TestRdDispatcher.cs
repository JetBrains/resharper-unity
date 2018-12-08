#if RIDER
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.Application.ContextNotifications;
using JetBrains.Application.Interop.NativeHook;
using JetBrains.Application.platforms;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.Components;
using JetBrains.Application.UI.Tooltips;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ReSharper.Feature.Services.ClipboardRead;
using JetBrains.ReSharper.FeaturesTestFramework.Clipboard;
using JetBrains.ReSharper.FeaturesTestFramework.Navigation;
using JetBrains.ReSharper.Host.Features.BraceMatching;
using JetBrains.ReSharper.Host.Features.Components;
using JetBrains.ReSharper.Host.Features.Intentions;
using JetBrains.ReSharper.Host.Features.NavigationHandling.Features;
using JetBrains.ReSharper.Host.Features.Platforms;
using JetBrains.ReSharper.Host.Features.Services;
using JetBrains.ReSharper.Host.Features.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.ReSharper.TestFramework.Components.Feature.Services.ContextHighlighters;
using JetBrains.Rider.Model;
using JetBrains.TestFramework.Projects;
using JetBrains.TestFramework.TextControl;
using JetBrains.TestFramework.UI.Components;
using JetBrains.TestFramework.UI.Tooltips;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [ShellComponent]
    public class TestRdDispatcher : ShellRdDispatcher
    {
        public TestRdDispatcher(IShellLocks shellLocks)
            : base(shellLocks)
        {
        }
    }

    [ShellComponent]
    public class MyProtocol : ProtocolComponent, IHideImplementation<ShellProtocol>
    {
        public MyProtocol(string name, ISerializers serializers, IIdentities identities, IScheduler scheduler, IWire wire)
            : base(name, serializers, identities, scheduler, wire)
        {
        }

        public MyProtocol(IShellLocks locks, IPolymorphicTypesCatalog typesCatalog)
            : base(locks, typesCatalog)
        {
        }
    }
//
//    [ShellComponent(Lifecycle.DemandReclaimable, Creation.AnyThread, Access.AnyThread)]
//    public class MyTooltipManagerComponent : TestImplTooltipManagerComponent, IHideImplementation<HostTooltipManagerComponent>
//    {
//        public MyTooltipManagerComponent(IUIApplication uiapp)
//            : base(uiapp)
//        {
//        }
//    }
//
//    [ShellComponent]
//    public class MyIsApplicationActiveStateTracker : TestImplIsApplicationActiveStateTracker,
//        IHideImplementation<RiderIsApplicationActiveStateTracker>
//    {
//        public MyIsApplicationActiveStateTracker([NotNull] Lifetime lifetimeComponent, IWindowsHookManager windowsHookManager)
//            : base(lifetimeComponent, windowsHookManager)
//        {
//        }
//    }
//
//    [ShellComponent]
//    public class MyGutterComponent: GutterMarginTestComponent, IHideImplementation<RiderGutterMarginComponent>
//    {
//        public MyGutterComponent([NotNull] SettingsStore settingsStore, [NotNull] Lifetime lifetime)
//            : base(settingsStore, lifetime)
//        {
//        }
//    }
//
//    [ShellComponent]
//    public class MyClipboardEntriesImpl: TestClipboardEntriesImpl, IHideImplementation<RiderClipboardEntriesImpl>
//    {
//        public MyClipboardEntriesImpl([NotNull] Lifetime lifetime, [NotNull] IActionManager actionManager, [NotNull] IThreading threading, [NotNull] Clipboard clipboard)
//            : base(lifetime, actionManager, threading, clipboard)
//        {
//        }
//    }
//
//    [ShellComponent]
//    public class MyClipboard : TestClipboard, IHideImplementation<RiderClipboard>
//    {
//        public MyClipboard(Lifetime lifetime, IThreading threading)
//            : base(lifetime, threading)
//        {
//        }
//    }
//
//    [PlatformsProvider]
//    public class MyDotNetCorePlatformsProvider : DotNetCorePlatformProviderTestImpl, IHideImplementation<RiderDotNetCorePlatformsProvider>
//    {
//        public MyDotNetCorePlatformsProvider(Lifetime lifetime)
//            : base(lifetime)
//        {
//        }
//    }
//
//    [ShellComponent]
//    public class MyGotoDeclarationUsagesSettings: TestGotoDeclarationUsagesSettings, IHideImplementation<RiderGotoDeclarationUsagesSettings>
//    {
//        public MyGotoDeclarationUsagesSettings([NotNull] Lifetime lifetime, [NotNull] ISettingsStore settingsStore, [NotNull] ContextNotificationHostProvider contextNotificationHostProvider)
//            : base(lifetime, settingsStore, contextNotificationHostProvider)
//        {
//        }
//    }
//
//    [ShellComponent]
//    public class MyCaretDependentFeaturesUtilCompone : CaretDependentFeaturesUtilComponentWithMockingMode,
//        IHideImplementation<RiderCaretDependentFeaturesUtilComponent>
//    {
//        public MyCaretDependentFeaturesUtilCompone([NotNull] IDocumentMarkupManager documentMarkupManager, [NotNull] ITooltipManager tooltipManager, [NotNull] IShellLocks shellLocks, [NotNull] ITextControlSchemeManager textControlSchemeManager)
//            : base(documentMarkupManager, tooltipManager, shellLocks, textControlSchemeManager)
//        {
//        }
//    }
//
//    [ShellComponent]
//    public class MySolutionMarkProvider
//    {
//        public MySolutionMarkProvider(ComponentContainer componentContainer)
//        {
//            componentContainer.Register(c => SolutionMarkFactory.Create(FileSystemPath.Empty));
//        }
//    }
}
#endif