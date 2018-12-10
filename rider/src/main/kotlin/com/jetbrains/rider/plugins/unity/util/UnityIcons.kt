package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.util.IconLoader
import com.intellij.ui.AnimatedIcon
import com.jetbrains.rider.icons.*
import com.jetbrains.rider.plugins.unity.util.UnityIcons.Icons.Companion.UnityLogo
import javax.swing.Icon

class UnityIcons {
    class Icons {
        companion object {
            @JvmField
            val UnityLogo = IconLoader.getIcon("/Icons/Logo/UnityLogo.svg")
        }
    }

    class Common {
        companion object {
            @JvmField
            val UnityEditMode = IconLoader.getIcon("/Icons/common/unityEditMode.svg")
            @JvmField
            val UnityPlayMode = IconLoader.getIcon("/Icons/common/unityPlayMode.svg")
            @JvmField
            val UnityDefault = IconLoader.getIcon("/Icons/common/unityDefault.svg")
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

            val UnityYaml: Icon = IconLoader.getIcon("/resharper/YamlFileType/FileYaml.svg")
            val UnityScene = UnityLogo
            val Meta = UnityYaml
            val Asset = UnityYaml
            val Prefab = UnityYaml
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
            val PackagesRoot = IconLoader.getIcon("/Icons/Explorer/UnityPackages.svg")
            val ReferencesRoot = ReSharperCommonIcons.CompositeElement
            val ReadOnlyPackagesRoot = IconLoader.getIcon("/Icons/Explorer/FolderReadOnly.svg")
            val DependenciesRoot = IconLoader.getIcon("/Icons/Explorer/FolderDependencies.svg")
            val BuiltInPackagesRoot = IconLoader.getIcon("/Icons/Explorer/FolderModules.svg")

            val BuiltInPackage = IconLoader.getIcon("/Icons/Explorer/UnityModule.svg")
            val ReferencedPackage = IconLoader.getIcon("/Icons/Explorer/FolderPackageReferenced.svg")
            val EmbeddedPackage = IconLoader.getIcon("/Icons/Explorer/FolderPackageEmbedded.svg")
            val LocalPackage: Icon = IconLoader.getIcon("/Icons/Explorer/FolderPackageLocal.svg")
            val UnknownPackage = IconLoader.getIcon("/Icons/Explorer/UnityPackageUnresolved.svg")
            val PackageDependency = IconLoader.getIcon("/Icons/Explorer/UnityPackageDependency.svg")
            val Reference = ReSharperProjectModelIcons.Assembly


            // Not yet supported by Unity, but we're ready! Except it could probably do with it's own icon...
            val GitPackage = LocalPackage

            val AsmdefFolder = IconLoader.getIcon("/Icons/Explorer/FolderAssetsAlt.svg")
            val AssetsFolder = IconLoader.getIcon("/Icons/Explorer/FolderAssets.svg")
            val EditorDefaultResourcesFolder = IconLoader.getIcon("/Icons/Explorer/FolderEditorResources.svg")
            val EditorFolder = IconLoader.getIcon("/Icons/Explorer/FolderEditor.svg")
            val GizmosFolder = IconLoader.getIcon("/Icons/Explorer/FolderGizmos.svg")
            val PluginsFolder = IconLoader.getIcon("/Icons/Explorer/FolderPlugins.svg")
            val ResourcesFolder = IconLoader.getIcon("/Icons/Explorer/FolderResources.svg")
            val StreamingAssetsFolder = IconLoader.getIcon("/Icons/Explorer/FolderStreamingAssets.svg")
        }
    }

    class Actions {
        companion object {
            @JvmField
            val ImportantActions = Icons.UnityLogo
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
            val FilterEditModeMessages = Common.UnityEditMode
            @JvmField
            val FilterPlayModeMessages = Common.UnityPlayMode

            val OpenEditorLog = FilterEditModeMessages
            val OpenPlayerLog = FilterPlayModeMessages

            val AttachToUnity = IconLoader.getIcon("/Icons/actions/attachToUnityProcess.svg")
        }
    }

    class RunConfigurations {
        companion object {
            val AttachToUnityParentConfiguration = Icons.UnityLogo
            val AttachAndDebug = Common.UnityEditMode
            val AttachDebugAndPlay = Common.UnityPlayMode
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

    class ToolWindows {
        companion object {
            @JvmField
            val UnityLog = Common.UnityDefault
            val UnityExplorer = Common.UnityDefault
        }
    }
}

