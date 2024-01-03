package icons

import com.intellij.openapi.util.IconLoader
import icons.ReSharperIcons.PsiSymbols
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

    class Toolbar {
        companion object {
            val Toolbar = IconLoader.getIcon("/resharper/Toolbar/UnityToolbar.svg", UnityIcons::class.java)
            val ToolbarConnected = IconLoader.getIcon("/resharper/Toolbar/UnityToolbarConnected.svg", UnityIcons::class.java)
            val ToolbarDisconnected = IconLoader.getIcon("/resharper/Toolbar/UnityToolbarDisconnected.svg", UnityIcons::class.java)
        }
    }


    class Common {
        companion object {
            val UnityEditMode = IconLoader.getIcon("/unityIcons/common/unityEditMode.svg", UnityIcons::class.java)
            val UnityPlayMode = IconLoader.getIcon("/unityIcons/common/unityPlayMode.svg", UnityIcons::class.java)
            val UnityToolWindow = IconLoader.getIcon("/unityIcons/common/unityToolWindow.svg", UnityIcons::class.java)
        }
    }

    class LogView {
        companion object {
            val FilterBeforePlay = IconLoader.getIcon("/unityIcons/logView/filterBeforePlay.svg", UnityIcons::class.java)
            val FilterBeforeRefresh = IconLoader.getIcon("/unityIcons/logView/filterBeforeRefresh.svg", UnityIcons::class.java)
        }
    }

    class FileTypes {
        companion object {
            val ShaderLab = PsiSymbols.FileShader
            val Cg = PsiSymbols.FileShader

            val AsmDef: Icon = IconLoader.getIcon("/resharper/UnityFileType/Asmdef.svg", UnityIcons::class.java)
            val AsmRef: Icon = IconLoader.getIcon("/resharper/UnityFileType/Asmref.svg", UnityIcons::class.java)

            val UnityYaml = IconLoader.getIcon("/resharper/YamlFileType/FileYaml.svg", UnityIcons::class.java)
            val UnityScene = IconLoader.getIcon("/resharper/UnityFileType/FileUnity.svg", UnityIcons::class.java)
            val Meta = IconLoader.getIcon("/resharper/UnityFileType/FileUnityMeta.svg", UnityIcons::class.java)
            val Asset = IconLoader.getIcon("/resharper/UnityFileType/FileUnityAsset.svg", UnityIcons::class.java)
            val Prefab = IconLoader.getIcon("/resharper/UnityFileType/FileUnityPrefab.svg", UnityIcons::class.java)
            val Controller = IconLoader.getIcon("/resharper/UnityFileType/FileAnimatorController.svg", UnityIcons::class.java)
            val Anim = IconLoader.getIcon("/resharper/UnityFileType/FileAnimationClip.svg", UnityIcons::class.java)
            val InputActions = IconLoader.getIcon("/resharper/UnityFileType/InputActions.svg", UnityIcons::class.java)

            // These are front end only file types
            val Uss = IconLoader.getIcon("/unityIcons/fileTypes/uss.svg", UnityIcons::class.java)
            val Uxml = IconLoader.getIcon("/unityIcons/fileTypes/uxml.svg", UnityIcons::class.java)
        }
    }

    class Debugger {
        // Field and file names deliberately match the AllIcons.Debugger icons.
        // Pausepoints are by definition "no suspend". Where the default breakpoint icon set includes a "no_suspend"
        // variant, the same file name is used. Otherwise, the default name is drawn as "no_suspend".
        companion object {
            val Db_dep_line_pausepoint = IconLoader.getIcon("/unityIcons/debugger/db_dep_line_pausepoint.svg", UnityIcons::class.java)
            val Db_disabled_pausepoint = IconLoader.getIcon("/unityIcons/debugger/db_disabled_pausepoint.svg", UnityIcons::class.java)
            val Db_invalid_pausepoint = IconLoader.getIcon("/unityIcons/debugger/db_invalid_pausepoint.svg", UnityIcons::class.java)
            val Db_muted_pausepoint = IconLoader.getIcon("/unityIcons/debugger/db_muted_pausepoint.svg", UnityIcons::class.java)
            val Db_muted_disabled_pausepoint = IconLoader.getIcon("/unityIcons/debugger/db_muted_disabled_pausepoint.svg", UnityIcons::class.java)
            val Db_no_suspend_pausepoint = IconLoader.getIcon("/unityIcons/debugger/db_no_suspend_pausepoint.svg", UnityIcons::class.java)
            val Db_set_pausepoint = Db_no_suspend_pausepoint
            val Db_verified_no_suspend_pausepoint = IconLoader.getIcon("/unityIcons/debugger/db_verified_no_suspend_pausepoint.svg", UnityIcons::class.java)
            val Db_verified_pausepoint = Db_verified_no_suspend_pausepoint
        }
    }

    class Explorer {
        companion object {

            val AssetsRoot = IconLoader.getIcon("/unityIcons/Explorer/UnityAssets.svg", UnityIcons::class.java)
            val PackagesRoot = IconLoader.getIcon("/resharper/UnityObjectType/UnityPackages.svg", UnityIcons::class.java)
            val ReferencesRoot: Icon = ReSharperIcons.Common.CompositeElement
            val ReadOnlyPackagesRoot = IconLoader.getIcon("/unityIcons/Explorer/FolderReadOnly.svg", UnityIcons::class.java)
            val DependenciesRoot = IconLoader.getIcon("/unityIcons/Explorer/FolderDependencies.svg", UnityIcons::class.java)
            val BuiltInPackagesRoot = IconLoader.getIcon("/unityIcons/Explorer/FolderModules.svg", UnityIcons::class.java)

            val BuiltInPackage = IconLoader.getIcon("/unityIcons/Explorer/UnityModule.svg", UnityIcons::class.java)
            val ReferencedPackage = IconLoader.getIcon("/resharper/UnityFileType/FolderPackageReferenced.svg", UnityIcons::class.java)
            val EmbeddedPackage = IconLoader.getIcon("/unityIcons/Explorer/FolderPackageEmbedded.svg", UnityIcons::class.java)
            val LocalPackage = IconLoader.getIcon("/unityIcons/Explorer/FolderPackageLocal.svg", UnityIcons::class.java)
            val LocalTarballPackage = LocalPackage
            val GitPackage = IconLoader.getIcon("/unityIcons/Explorer/FolderGit.svg", UnityIcons::class.java)
            val UnknownPackage = IconLoader.getIcon("/unityIcons/Explorer/UnityPackageUnresolved.svg", UnityIcons::class.java)
            val PackageDependency = IconLoader.getIcon("/unityIcons/Explorer/UnityPackageDependency.svg", UnityIcons::class.java)
            val Reference: Icon = ReSharperIcons.ProjectModel.Assembly

            val AsmdefFolder = IconLoader.getIcon("/unityIcons/Explorer/FolderAssetsAlt.svg", UnityIcons::class.java)
            val AssetsFolder = IconLoader.getIcon("/unityIcons/Explorer/FolderAssets.svg", UnityIcons::class.java)
            val EditorDefaultResourcesFolder = IconLoader.getIcon("/unityIcons/Explorer/FolderEditorResources.svg", UnityIcons::class.java)
            val EditorFolder = IconLoader.getIcon("/unityIcons/Explorer/FolderEditor.svg", UnityIcons::class.java)
            val GizmosFolder = IconLoader.getIcon("/unityIcons/Explorer/FolderGizmos.svg", UnityIcons::class.java)
            val PluginsFolder = IconLoader.getIcon("/unityIcons/Explorer/FolderPlugins.svg", UnityIcons::class.java)
            val ResourcesFolder = IconLoader.getIcon("/unityIcons/Explorer/FolderResources.svg", UnityIcons::class.java)
            val StreamingAssetsFolder = IconLoader.getIcon("/unityIcons/Explorer/FolderStreamingAssets.svg", UnityIcons::class.java)
            val UnloadedFolder = IconLoader.getIcon("/unityIcons/Explorer/FolderUnloaded.svg", UnityIcons::class.java)
        }
    }

    class Actions {
        companion object {
            val UnityActionsGroup = Icons.UnityLogo
            @JvmField val StartUnity = Icons.UnityLogo

            @JvmField val Execute = IconLoader.getIcon("/unityIcons/actions/execute.svg", UnityIcons::class.java)
            @JvmField val Pause = IconLoader.getIcon("/unityIcons/actions/pause.svg", UnityIcons::class.java)
            @JvmField val Step = IconLoader.getIcon("/unityIcons/actions/step.svg", UnityIcons::class.java)
            val FilterEditModeMessages = Common.UnityEditMode
            val FilterPlayModeMessages = Common.UnityPlayMode

            @JvmField val OpenEditorLog = FilterEditModeMessages
            @JvmField val OpenPlayerLog = FilterPlayModeMessages

            @JvmField val AttachToUnity = IconLoader.getIcon("/unityIcons/actions/attachToUnityProcess.svg", UnityIcons::class.java)

            @JvmField val RefreshInUnity = IconLoader.getIcon("/unityIcons/actions/refreshInUnity.svg", UnityIcons::class.java)
        }
    }

    class RunConfigurations {
        companion object {
            val AttachToUnityParentConfiguration = Icons.UnityLogo
            val AttachAndDebug = Common.UnityEditMode
            val AttachDebugAndPlay = Common.UnityPlayMode
            val AttachToPlayer = Common.UnityPlayMode
            val UnityExe = Common.UnityPlayMode
        }
    }

    class Ide {
        companion object {
            val Warning = IconLoader.getIcon("/unityIcons/ide/warning.svg", UnityIcons::class.java)
            val Info = IconLoader.getIcon("/unityIcons/ide/info.svg", UnityIcons::class.java)
            val Error = IconLoader.getIcon("/unityIcons/ide/error.svg", UnityIcons::class.java)
        }
    }

    class ToolWindows {
        companion object {
            val UnityLog = Common.UnityToolWindow
            val UnityExplorer = Common.UnityToolWindow
        }
    }
}

