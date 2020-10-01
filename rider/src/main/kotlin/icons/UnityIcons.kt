package icons

import com.intellij.openapi.util.IconLoader
import com.intellij.ui.AnimatedIcon
import javax.swing.Icon

// FYI: Icons are defined in C# files in the backend. When being shown in the frontend, only the icon ID is passed to
// the frontend, and IJ will look it up in resources/resharper. The name of the enclosing C# class is stripped of any
// trailing "Icons" or "ThemedIcons" and used as the folder name. The icon is named after the inner class. IJ will
// automatically add `_dark` to the basename of the SVG file if in Darcula.
// Note that IJ has a different palette and colour scheme to ReSharper. This means that the front end svg files might
// not be the same as the backed C# files...

// We need to be in the icons root package so we can use this class from plugin.xml. We also need to use @JvmField so
// that the kotlin value is visible as a JVM field via reflection

class UnityIcons {
    class Icons {
        companion object {
            val UnityLogo = IconLoader.getIcon("/resharper/Logo/Unity.svg", UnityIcons::class.java)
        }
    }

    class Common {
        companion object {
            val UnityEditMode = IconLoader.getIcon("/Icons/common/unityEditMode.svg", UnityIcons::class.java)
            val UnityPlayMode = IconLoader.getIcon("/Icons/common/unityPlayMode.svg", UnityIcons::class.java)
            val UnityToolWindow = IconLoader.getIcon("/Icons/common/unityToolWindow.svg", UnityIcons::class.java)
        }
    }

    class FileTypes {
        companion object {
            val ShaderLab = IconLoader.getIcon("/resharper/ShaderFileType/FileShader.svg", UnityIcons::class.java)
            val Cg = ShaderLab

            val AsmDef: Icon = ReSharperIcons.PsiJavaScript.Json

            val UnityYaml = IconLoader.getIcon("/resharper/YamlFileType/FileYaml.svg", UnityIcons::class.java)
            val UnityScene = IconLoader.getIcon("/resharper/UnityFileType/FileUnity.svg", UnityIcons::class.java)
            val Meta = IconLoader.getIcon("/resharper/UnityFileType/FileUnityMeta.svg", UnityIcons::class.java)
            val Asset = IconLoader.getIcon("/resharper/UnityFileType/FileUnityAsset.svg", UnityIcons::class.java)
            val Prefab = IconLoader.getIcon("/resharper/UnityFileType/FileUnityPrefab.svg", UnityIcons::class.java)

            // These are front end only file types
            val Uss = IconLoader.getIcon("/Icons/fileTypes/uss.svg", UnityIcons::class.java)
            val Uxml = IconLoader.getIcon("/Icons/fileTypes/uxml.svg", UnityIcons::class.java)
        }
    }

    class Status{
        companion object {
            val UnityStatus = IconLoader.getIcon("/Icons/status/unityStatus.svg", UnityIcons::class.java)
            val UnityStatusPlay = IconLoader.getIcon("/Icons/status/unityStatusPlay.svg", UnityIcons::class.java)
            val UnityStatusPause = IconLoader.getIcon("/Icons/status/unityStatusPause.svg", UnityIcons::class.java)

            val UnityStatusProgress = AnimatedIcon(150,
                IconLoader.getIcon("/Icons/status/unityStatusProgress6.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusProgress5.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusProgress4.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusProgress3.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusProgress2.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusProgress1.svg", UnityIcons::class.java))

            val UnityStatusPlayProgress = AnimatedIcon(150,
                IconLoader.getIcon("/Icons/status/unityStatusPlayProgress6.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPlayProgress5.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPlayProgress4.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPlayProgress3.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPlayProgress2.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPlayProgress1.svg", UnityIcons::class.java))

            val UnityStatusPauseProgress = AnimatedIcon(150,
                IconLoader.getIcon("/Icons/status/unityStatusPauseProgress6.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPauseProgress5.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPauseProgress4.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPauseProgress3.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPauseProgress2.svg", UnityIcons::class.java),
                IconLoader.getIcon("/Icons/status/unityStatusPauseProgress1.svg", UnityIcons::class.java))
        }
    }

    class Debugger {
        // Field and file names deliberately match the AllIcons.Debugger icons.
        // Pausepoints are by definition "no suspend". Where the default breakpoint icon set includes a "no_suspend"
        // variant, the same file name is used. Otherwise, the default name is drawn as "no_suspend".
        companion object {
            val Db_dep_line_pausepoint = IconLoader.getIcon("/Icons/debugger/db_dep_line_pausepoint.svg", UnityIcons::class.java)
            val Db_disabled_pausepoint = IconLoader.getIcon("/Icons/debugger/db_disabled_pausepoint.svg", UnityIcons::class.java)
            val Db_invalid_pausepoint = IconLoader.getIcon("/Icons/debugger/db_invalid_pausepoint.svg", UnityIcons::class.java)
            val Db_muted_pausepoint = IconLoader.getIcon("/Icons/debugger/db_muted_pausepoint.svg", UnityIcons::class.java)
            val Db_muted_disabled_pausepoint = IconLoader.getIcon("/Icons/debugger/db_muted_disabled_pausepoint.svg", UnityIcons::class.java)
            val Db_no_suspend_pausepoint = IconLoader.getIcon("/Icons/debugger/db_no_suspend_pausepoint.svg", UnityIcons::class.java)
            val Db_set_pausepoint = Db_no_suspend_pausepoint
            val Db_verified_no_suspend_pausepoint = IconLoader.getIcon("/Icons/debugger/db_verified_no_suspend_pausepoint.svg", UnityIcons::class.java)
            val Db_verified_pausepoint = Db_verified_no_suspend_pausepoint
        }
    }

    class Explorer {
        companion object {

            val AssetsRoot = IconLoader.getIcon("/Icons/Explorer/UnityAssets.svg", UnityIcons::class.java)
            val PackagesRoot = IconLoader.getIcon("/Icons/Explorer/UnityPackages.svg", UnityIcons::class.java)
            val ReferencesRoot: Icon = ReSharperIcons.Common.CompositeElement
            val ReadOnlyPackagesRoot = IconLoader.getIcon("/Icons/Explorer/FolderReadOnly.svg", UnityIcons::class.java)
            val DependenciesRoot = IconLoader.getIcon("/Icons/Explorer/FolderDependencies.svg", UnityIcons::class.java)
            val BuiltInPackagesRoot = IconLoader.getIcon("/Icons/Explorer/FolderModules.svg", UnityIcons::class.java)

            val BuiltInPackage = IconLoader.getIcon("/Icons/Explorer/UnityModule.svg", UnityIcons::class.java)
            val ReferencedPackage = IconLoader.getIcon("/Icons/Explorer/FolderPackageReferenced.svg", UnityIcons::class.java)
            val EmbeddedPackage = IconLoader.getIcon("/Icons/Explorer/FolderPackageEmbedded.svg", UnityIcons::class.java)
            val LocalPackage = IconLoader.getIcon("/Icons/Explorer/FolderPackageLocal.svg", UnityIcons::class.java)
            val LocalTarballPackage = LocalPackage
            val GitPackage = IconLoader.getIcon("/Icons/Explorer/FolderGit.svg", UnityIcons::class.java)
            val UnknownPackage = IconLoader.getIcon("/Icons/Explorer/UnityPackageUnresolved.svg", UnityIcons::class.java)
            val PackageDependency = IconLoader.getIcon("/Icons/Explorer/UnityPackageDependency.svg", UnityIcons::class.java)
            val Reference: Icon = ReSharperIcons.ProjectModel.Assembly

            val AsmdefFolder = IconLoader.getIcon("/Icons/Explorer/FolderAssetsAlt.svg", UnityIcons::class.java)
            val AssetsFolder = IconLoader.getIcon("/Icons/Explorer/FolderAssets.svg", UnityIcons::class.java)
            val EditorDefaultResourcesFolder = IconLoader.getIcon("/Icons/Explorer/FolderEditorResources.svg", UnityIcons::class.java)
            val EditorFolder = IconLoader.getIcon("/Icons/Explorer/FolderEditor.svg", UnityIcons::class.java)
            val GizmosFolder = IconLoader.getIcon("/Icons/Explorer/FolderGizmos.svg", UnityIcons::class.java)
            val PluginsFolder = IconLoader.getIcon("/Icons/Explorer/FolderPlugins.svg", UnityIcons::class.java)
            val ResourcesFolder = IconLoader.getIcon("/Icons/Explorer/FolderResources.svg", UnityIcons::class.java)
            val StreamingAssetsFolder = IconLoader.getIcon("/Icons/Explorer/FolderStreamingAssets.svg", UnityIcons::class.java)
            val UnloadedFolder = IconLoader.getIcon("/Icons/Explorer/FolderUnloaded.svg", UnityIcons::class.java)
        }
    }

    class Actions {
        companion object {
            val UnityActionsGroup = Icons.UnityLogo
            @JvmField val StartUnity = Icons.UnityLogo

            @JvmField val Execute = IconLoader.getIcon("/Icons/actions/execute.svg", UnityIcons::class.java)
            @JvmField val Pause = IconLoader.getIcon("/Icons/actions/pause.svg", UnityIcons::class.java)
            @JvmField val Step = IconLoader.getIcon("/Icons/actions/step.svg", UnityIcons::class.java)
            val FilterEditModeMessages = Common.UnityEditMode
            val FilterPlayModeMessages = Common.UnityPlayMode

            @JvmField val OpenEditorLog = FilterEditModeMessages
            @JvmField val OpenPlayerLog = FilterPlayModeMessages

            @JvmField val AttachToUnity = IconLoader.getIcon("/Icons/actions/attachToUnityProcess.svg", UnityIcons::class.java)

            @JvmField val RefreshInUnity = IconLoader.getIcon("/Icons/actions/refreshInUnity.svg", UnityIcons::class.java)
        }
    }

    class RunConfigurations {
        companion object {
            val AttachToUnityParentConfiguration = Icons.UnityLogo
            val AttachAndDebug = Common.UnityEditMode
            val AttachDebugAndPlay = Common.UnityPlayMode
            val UnityExe = Common.UnityPlayMode
        }
    }

    class Ide {
        companion object {
            val Warning = IconLoader.getIcon("/Icons/ide/warning.svg", UnityIcons::class.java)
            val Info = IconLoader.getIcon("/Icons/ide/info.svg", UnityIcons::class.java)
            val Error = IconLoader.getIcon("/Icons/ide/error.svg", UnityIcons::class.java)
        }
    }

    class ToolWindows {
        companion object {
            val UnityLog = Common.UnityToolWindow
            val UnityExplorer = Common.UnityToolWindow
        }
    }
}

