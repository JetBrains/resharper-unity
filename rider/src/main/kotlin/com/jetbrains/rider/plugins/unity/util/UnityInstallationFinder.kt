package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.hasSolution
import com.jetbrains.rider.projectView.solution
import java.nio.file.Path
import java.nio.file.Paths

class UnityInstallationFinder(private val project: Project) {

    companion object {
        fun getInstance(project: Project): UnityInstallationFinder {
            return UnityInstallationFinder(project)
        }
    }

    fun getBuiltInPackagesRoot(): Path? {
        return getApplicationContentsPath()?.resolve("Resources/PackageManager/BuiltInPackages")
    }

    fun getPackageManagerDefaultManifest(): Path? {
        return getApplicationContentsPath()?.resolve("Resources/PackageManager/Editor/manifest.json")
    }

    fun getDocumentationRoot(): Path? {
        // On Mac, /Unity.app/Contents/Documentation is actually a link to /Documentation, at the same level as the app
        return getApplicationContentsPath()?.resolve("Documentation/en")
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
        var path =  tryGetApplicationPathFromProtocol()
        if (SystemInfo.isMac)
            path = path?.resolve("Contents/MacOS/Unity")
        return path
    }

    // The standalone player for the current platform is installed here, e.g. MacStandaloneSupport, WindowsStandaloneSupport
    // Mac: /Applications/Unity/Hub/Editor/2020.2.0a15/Unity.app/Contents/PlaybackEngines
    // Windows: C:\Program Files\Unity\Hub\Editor\2020.1.0b13\Editor\Data\PlaybackEngines
    // Linux: ???
    fun getDefaultPlaybackEnginesRoot(): Path? {
        return getApplicationContentsPath()?.resolve("PlaybackEngines")
    }

    // Additional, optional player support files are installed here, e.g. iOSSupport, AndroidSupport
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
        if (!project.hasSolution)
            return null
        return project.solution.frontendBackendModel.unityApplicationData.valueOrNull?.let { Paths.get(it.applicationContentsPath) }
    }

    private fun tryGetApplicationPathFromProtocol(): Path? {
        if (!project.hasSolution)
            return null
        return project.solution.frontendBackendModel.unityApplicationData.valueOrNull?.let { Paths.get(it.applicationPath) }
    }

    fun getApplicationVersion(): String? {
        return tryGetApplicationVersionFromProtocol()
    }

    fun getApplicationVersion(count:Int):String? {
        val fullVersion = getApplicationVersion()
        return fullVersion?.split('.')?.take(count)?.joinToString(".")
    }

    private fun tryGetApplicationVersionFromProtocol(): String? {
        if (!project.hasSolution)
            return null
        return project.solution.frontendBackendModel.unityApplicationData.valueOrNull?.applicationVersion
    }

    fun requiresRiderPackage(): Boolean {
        if (!project.hasSolution)
            return false
        return project.solution.frontendBackendModel.requiresRiderPackage.valueOrDefault(false)
    }
}