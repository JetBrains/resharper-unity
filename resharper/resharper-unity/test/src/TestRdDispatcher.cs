
using JetBrains.Collections.Viewable;
using JetBrains.Rd;
#if RIDER
using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.ReSharper.Host.Features.Components;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Notifications;

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
        public MyProtocol(string name, ISerializers serializers, IIdentities identities, IScheduler scheduler, IWire wire, Lifetime lifetime)
            : base(name, serializers, identities, scheduler, wire, lifetime)
        {
        }

        public MyProtocol(IShellLocks locks, ITypesRegistrar typesCatalog, Lifetime lifetime)
            : base(locks, typesCatalog, lifetime)
        {
        }
    }
    
    [ShellComponent]
    public class MyRdShellModel : RdShellModel, IHideImplementation<RdShellModel>
    {
        public MyRdShellModel(Lifetime lifetime, IProtocol protocol)
            : base(lifetime, protocol)
        {
        }
    }
    
    [ShellComponent]
    public class MyNotificationModel : NotificationsModel, IHideImplementation<NotificationsModel>
    {
        public MyNotificationModel(Lifetime lifetime, IProtocol protocol)
            : base(lifetime, protocol)
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