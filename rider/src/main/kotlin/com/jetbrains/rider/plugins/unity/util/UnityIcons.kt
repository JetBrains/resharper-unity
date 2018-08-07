package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.util.IconLoader
import com.intellij.ui.AnimatedIcon
import com.intellij.ui.LayeredIcon
import com.jetbrains.rider.icons.ReSharperAssemblyExplorerIcons
import com.jetbrains.rider.icons.ReSharperCommonIcons
import com.jetbrains.rider.icons.ReSharperProjectModelIcons
import com.jetbrains.rider.icons.ReSharperPsiJavaScriptIcons
import javax.swing.Icon

class UnityIcons {
    class Icons {
        companion object {
            @JvmField
            val UnityLogo = IconLoader.getIcon("/Icons/Logo/UnityLogo.png")

            // TODO: Proper icons!
            @JvmField
            val ImportantActions = UnityLogo
        }
    }

    class FileTypes {
        companion object {
            @JvmField
            val ShaderLab = IconLoader.getIcon("/Icons/fileTypes/shaderLab.svg")

            @JvmField
            val Cg = ShaderLab

            @JvmField
            val AsmDef = ReSharperPsiJavaScriptIcons.Json
        }
    }

    class Status{
        companion object {
            @JvmField
            val UnityStatus = IconLoader.getIcon("/Icons/status/unityStatus.svg")
            @JvmField
            val UnityStatusPlay = IconLoader.getIcon("/Icons/status/unityStatusPlay.svg")

            @JvmField
            val UnityStatusProgress1 = IconLoader.getIcon("/Icons/status/unityStatusProgress1.svg")
            @JvmField
            val UnityStatusProgress2 = IconLoader.getIcon("/Icons/status/unityStatusProgress2.svg")
            @JvmField
            val UnityStatusProgress3 = IconLoader.getIcon("/Icons/status/unityStatusProgress3.svg")
            @JvmField
            val UnityStatusProgress4 = IconLoader.getIcon("/Icons/status/unityStatusProgress4.svg")
            @JvmField
            val UnityStatusProgress5 = IconLoader.getIcon("/Icons/status/unityStatusProgress5.svg")

            val UnityStatusProgress = AnimatedIcon(150,
                UnityStatusProgress5,
                UnityStatusProgress4,
                UnityStatusProgress3,
                UnityStatusProgress2,
                UnityStatusProgress1)

            @JvmField
            val UnityStatusPlayProgress1 = IconLoader.getIcon("/Icons/status/unityStatusPlayProgress1.svg")
            @JvmField
            val UnityStatusPlayProgress2 = IconLoader.getIcon("/Icons/status/unityStatusPlayProgress2.svg")
            @JvmField
            val UnityStatusPlayProgress3 = IconLoader.getIcon("/Icons/status/unityStatusPlayProgress3.svg")
            @JvmField
            val UnityStatusPlayProgress4 = IconLoader.getIcon("/Icons/status/unityStatusPlayProgress4.svg")
            @JvmField
            val UnityStatusPlayProgress5 = IconLoader.getIcon("/Icons/status/unityStatusPlayProgress5.svg")

            val UnityStatusPlayProgress = AnimatedIcon(150,
                UnityStatusPlayProgress5,
                UnityStatusPlayProgress4,
                UnityStatusPlayProgress3,
                UnityStatusPlayProgress2,
                UnityStatusPlayProgress1)
        }
    }

    class Explorer {
        companion object {

            val AssetsRoot = IconLoader.getIcon("/Icons/Explorer/UnityAssets.svg")
            val ReferencesRoot = ReSharperCommonIcons.CompositeElement
            val Reference = ReSharperProjectModelIcons.Assembly

            // TODO: Proper icons!
            val PackagesRoot = IconLoader.getIcon("/Icons/Explorer/FolderAssetsAlt.svg")
            val ReadOnlyPackagesRoot = ReSharperCommonIcons.CompositeElement
            val EmbeddedPackage = IconLoader.getIcon("/Icons/Explorer/FolderAssetsAlt.svg")
            val LocalPackage: Icon = LayeredIcon.create(EmbeddedPackage, ReSharperProjectModelIcons.ShortcutOverlay)
            val ReferencedPackage = EmbeddedPackage
            val GitPackage = ReferencedPackage
            val BuiltInPackagesRoot = ReSharperCommonIcons.CompositeElement
            val BuiltInPackage = ReSharperProjectModelIcons.Assembly
            val UnknownPackage = ReSharperAssemblyExplorerIcons.AssemblyBroken
            val DependenciesRoot = ReSharperProjectModelIcons.Assemblies
            val PackageDependency = ReSharperProjectModelIcons.AssemblyReference    // Assembly with shortcut arrow
        }
    }

    class Actions {
        companion object {
            @JvmField
            val Execute = IconLoader.getIcon("/Icons/actions/execute.svg")
            @JvmField
            val ForceRefresh = IconLoader.getIcon("/Icons/actions/forceRefresh.svg")
            @JvmField
            val GC = IconLoader.getIcon("/Icons/actions/gc.svg")
            @JvmField
            val Pause = IconLoader.getIcon("/Icons/actions/pause.svg")
            @JvmField
            val Step = IconLoader.getIcon("/Icons/actions/step.svg")

            @JvmField
            val FilterEditModeMessages = IconLoader.getIcon("/Icons/actions/filterEditModeMessages.svg")
            @JvmField
            val FilterPlayModeMessages = IconLoader.getIcon("/Icons/actions/filterPlayModeMessages.svg")

            val OpenEditorLog = FilterEditModeMessages
            val OpenPlayerLog = FilterPlayModeMessages

            val AttachToUnity = Icons.UnityLogo
        }
    }

    class RunConfigurations {
        companion object {
            // TODO: Proper icons!
            val AttachToUnityParentConfiguration = Icons.UnityLogo
            val AttachAndDebug = AttachToUnityParentConfiguration
            val AttachDebugAndPlay = AttachToUnityParentConfiguration
        }
    }

    class Ide {
        companion object {
            @JvmField
            val Warning = IconLoader.getIcon("/Icons/ide/warning.svg")
            @JvmField
            val Info = IconLoader.getIcon("/Icons/ide/info.svg")
            @JvmField
            val Error = IconLoader.getIcon("/Icons/ide/error.svg")
        }
    }

    class Toolwindows {
        companion object {
            @JvmField
            val ToolWindowUnityLog = IconLoader.getIcon("/Icons/toolwindows/toolWindowUnityLog.svg")
        }
    }
}
