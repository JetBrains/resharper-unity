package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rider.plugins.unity.EngineConstants
import com.jetbrains.rider.plugins.unity.model.UnityApplicationData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import java.nio.file.Path
import java.nio.file.Paths

@Service(Service.Level.PROJECT)
class UnityInstallationFinder {

    companion object {
        fun getInstance(project: Project): UnityInstallationFinder = project.service()

        fun getOsSpecificPath(path: Path): Path {
            if (SystemInfo.isMac) {
                if (path.endsWith("${EngineConstants.TuanjieEngineName}.app"))
                    return path.resolve("Contents/MacOS/${EngineConstants.TuanjieEngineName}")
                return path.resolve("Contents/MacOS/${EngineConstants.UnityEngineName}")
            }
            return path
        }
    }

    private var unityApplicationData: UnityApplicationData? = null
    var requiresRiderPackage = Property<Boolean?>(null)
    var isCoreCLR = Property<Boolean?>(null)

    fun getBuiltInPackagesRoot(): Path? {
        return getApplicationContentsPath()?.resolve("Resources/PackageManager/BuiltInPackages")
    }

    // The same as EditorApplication.applicationContentsPath
    // E.g. Mac: /Applications/Unity/Hub/Editor/2018.2.0f2/Unity.app/Contents
    // Windows: C:\Program Files\Unity\Hub\Editor\2018.2.1f1\Editor\Data
    // Linux: /home/ivan/Unity-2018.1.0f2/Editor/Data
    fun getApplicationContentsPath(): Path? {
        val contentsPath = getApplicationContentsPathFromProtocol()
        if (contentsPath != null) return contentsPath

        val root = tryGetApplicationPathFromProtocol() ?: return null
        return when {
            SystemInfo.isMac -> root.resolve("Contents")
            SystemInfo.isWindows -> root.parent.resolve("Data")
            SystemInfo.isLinux -> root.parent.resolve("Data")
            else -> null
        }
    }

    // Path to Unity executable. **NOT** the same as EditorApplication.applicationPath
    // E.g. Mac: /Applications/Unity/Hub/Editor/2018.2.0f2/Unity.app/Contents/MacOS/Unity
    // Windows: C:\Program Files\Unity\Hub\Editor\2018.2.1f1\Editor\Unity.exe
    // Linux: /home/ivan/Unity-2018.1.0f2/Editor/Unity
    fun getApplicationExecutablePath(): Path? {
        val path = tryGetApplicationPathFromProtocol()
        if (path != null)
            return getOsSpecificPath(path)
        return null
    }

    // The standalone player for the current platform is installed here, e.g. MacStandaloneSupport, WindowsStandaloneSupport
    // Mac: /Applications/Unity/Hub/Editor/2020.2.0a15/Unity.app/Contents/PlaybackEngines
    // Windows: C:\Program Files\Unity\Hub\Editor\2020.1.0b13\Editor\Data\PlaybackEngines
    // Linux: ???
    fun getDefaultPlaybackEnginesRoot(): Path? {
        return getApplicationContentsPath()?.resolve("PlaybackEngines")
    }

    // Additional, optional player support files are installed here, e.g. iOSSupport, AndroidPlayer
    // Mac: /Applications/Unity/Hub/Editor/2020.2.0a15/PlaybackEngines
    // Windows: C:\Program Files\Unity\Hub\Editor\2020.1.0b13\Editor\Data\PlaybackEngines
    // Linux: ???
    fun getAdditionalPlaybackEnginesRoot(): Path? {
        val applicationPath = tryGetApplicationPathFromProtocol() ?: return null
        return when {
            SystemInfo.isMac -> applicationPath.parent.resolve("PlaybackEngines")
            SystemInfo.isWindows -> getDefaultPlaybackEnginesRoot()
            SystemInfo.isLinux -> getDefaultPlaybackEnginesRoot()
            else -> null
        }
    }

    private fun getApplicationContentsPathFromProtocol(): Path? {
        return unityApplicationData?.let { Paths.get(it.applicationContentsPath) }
    }

    // This will *usually* return a valid value. If there's a backend<->Unity connection, we'll have the correct path.
    // If not, we'll set the best guess from found installs and the (nearest) current version taken from ProjectSettings
    // This will be null for non-Unity projects, or if Unity is installed to a non-standard location
    private fun tryGetApplicationPathFromProtocol(): Path? {
        return unityApplicationData?.let { Paths.get(it.applicationPath) }
    }

    fun getApplicationVersion(): String? {
        return tryGetApplicationVersionFromProtocol()
    }

    fun getApplicationVersion(count: Int): String? {
        val fullVersion = getApplicationVersion()
        return fullVersion?.split('.')?.take(count)?.joinToString(".")
    }

    private fun tryGetApplicationVersionFromProtocol() = unityApplicationData?.applicationVersion

    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            model.unityApplicationData.advise(lifetime) {
                getInstance(session.project).unityApplicationData = it
            }
            model.requiresRiderPackage.flowInto(lifetime, getInstance(session.project).requiresRiderPackage)
            model.isCoreCLR.flowInto(lifetime, getInstance(session.project).isCoreCLR)
        }
    }
}