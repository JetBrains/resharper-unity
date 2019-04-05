package icons

import com.intellij.openapi.util.IconLoader
import com.intellij.ui.AnimatedIcon
import com.jetbrains.rider.icons.*
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
            val UnityLogo = IconLoader.getIcon("/resharper/Logo/UnityLogo.svg")
        }
    }

    class Common {
        companion object {
            val UnityEditMode = IconLoader.getIcon("/Icons/common/unityEditMode.svg")
            val UnityPlayMode = IconLoader.getIcon("/Icons/common/unityPlayMode.svg")
            val UnityToolWindow = IconLoader.getIcon("/Icons/common/unityToolWindow.svg")
        }
    }

    class FileTypes {
        companion object {
            val ShaderLab = IconLoader.getIcon("/resharper/ShaderFileType/FileShader.svg")
            val Cg = ShaderLab

            val AsmDef = ReSharperPsiJavaScriptIcons.Json

            val UnityYaml: Icon = IconLoader.getIcon("/resharper/YamlFileType/FileYaml.svg")
            val UnityScene = IconLoader.getIcon("/resharper/UnityFileType/FileUnity.svg")
            val Meta = IconLoader.getIcon("/resharper/UnityFileType/FileUnityMeta.svg")
            val Asset = IconLoader.getIcon("/resharper/UnityFileType/FileUnityAsset.svg")
            val Prefab = IconLoader.getIcon("/resharper/UnityFileType/FileUnityPrefab.svg")
        }
    }

    class Status{
        companion object {
            val UnityStatus = IconLoader.getIcon("/Icons/status/unityStatus.svg")
            val UnityStatusPlay = IconLoader.getIcon("/Icons/status/unityStatusPlay.svg")

            val UnityStatusProgress1 = IconLoader.getIcon("/Icons/status/unityStatusProgress1.svg")
            val UnityStatusProgress2 = IconLoader.getIcon("/Icons/status/unityStatusProgress2.svg")
            val UnityStatusProgress3 = IconLoader.getIcon("/Icons/status/unityStatusProgress3.svg")
            val UnityStatusProgress4 = IconLoader.getIcon("/Icons/status/unityStatusProgress4.svg")
            val UnityStatusProgress5 = IconLoader.getIcon("/Icons/status/unityStatusProgress5.svg")

            val UnityStatusProgress = AnimatedIcon(150,
                UnityStatusProgress5,
                UnityStatusProgress4,
                UnityStatusProgress3,
                UnityStatusProgress2,
                UnityStatusProgress1)

            val UnityStatusPlayProgress1 = IconLoader.getIcon("/Icons/status/unityStatusPlayProgress1.svg")
            val UnityStatusPlayProgress2 = IconLoader.getIcon("/Icons/status/unityStatusPlayProgress2.svg")
            val UnityStatusPlayProgress3 = IconLoader.getIcon("/Icons/status/unityStatusPlayProgress3.svg")
            val UnityStatusPlayProgress4 = IconLoader.getIcon("/Icons/status/unityStatusPlayProgress4.svg")
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
            val GitPackage: Icon = IconLoader.getIcon("/Icons/Explorer/FolderGit.svg")
            val UnknownPackage = IconLoader.getIcon("/Icons/Explorer/UnityPackageUnresolved.svg")
            val PackageDependency = IconLoader.getIcon("/Icons/Explorer/UnityPackageDependency.svg")
            val Reference = ReSharperProjectModelIcons.Assembly

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
            val UnityActionsGroup = Icons.UnityLogo
            @JvmField val StartUnity = Icons.UnityLogo

            @JvmField val Execute = IconLoader.getIcon("/Icons/actions/execute.svg")
            @JvmField val Pause = IconLoader.getIcon("/Icons/actions/pause.svg")
            @JvmField val Step = IconLoader.getIcon("/Icons/actions/step.svg")
            val FilterEditModeMessages = Common.UnityEditMode
            val FilterPlayModeMessages = Common.UnityPlayMode

            val OpenEditorLog = FilterEditModeMessages
            val OpenPlayerLog = FilterPlayModeMessages

            @JvmField val AttachToUnity = IconLoader.getIcon("/Icons/actions/attachToUnityProcess.svg")
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
            val Warning = IconLoader.getIcon("/Icons/ide/warning.svg")
            val Info = IconLoader.getIcon("/Icons/ide/info.svg")
            val Error = IconLoader.getIcon("/Icons/ide/error.svg")
        }
    }

    class ToolWindows {
        companion object {
            val UnityLog = Common.UnityToolWindow
            val UnityExplorer = Common.UnityToolWindow
        }
    }
}

