package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rider.model.rdUnityModel
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
        return getApplicationContentsPath()?.resolve("Documentation/en")
    }

    // The same as EditorApplication.applicationContentsPath
    // E.g. Mac: /Applications/Unity/Hub/Editor/2018.2.0f2/Unity.app/Contents
    // Windows: C:\Program Files\Unity\Hub\Editor\2018.2.1f1\Editor\Data
    // Linux: /home/ivan/Unity-2018.1.0f2/Editor/Data
    private fun getApplicationContentsPath(): Path? {
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

    // Path to Unity executable
    // E.g. Mac: /Applications/Unity/Hub/Editor/2018.2.0f2/Unity.app/Contents/MacOS/Unity
    // Windows: C:\Program Files\Unity\Hub\Editor\2018.2.1f1\Editor\Unity.exe
    // Linux: /home/ivan/Unity-2018.1.0f2/Editor/Unity
    fun getApplicationPath(): Path? {
        var path =  tryGetApplicationPathFromProtocol()
        if (SystemInfo.isMac)
            path = path?.resolve("Contents/MacOS/Unity")
        return path
    }

    private fun getApplicationContentsPathFromProtocol(): Path? {
        return project.solution.rdUnityModel.unityApplicationData.valueOrNull?.let { Paths.get(it.applicationContentsPath) }
    }

    private fun tryGetApplicationPathFromProtocol(): Path? {
        return project.solution.rdUnityModel.unityApplicationData.valueOrNull?.let { Paths.get(it.applicationPath) }
    }

    fun getApplicationVersion(): String? {
        return tryGetApplicationVersionFromProtocol()
    }

    fun getApplicationVersion(count:Int):String? {
        val fullVersion = getApplicationVersion()
        return fullVersion?.split('.')?.take(count)?.joinToString(".")
    }

    private fun tryGetApplicationVersionFromProtocol(): String? {
        return project.solution.rdUnityModel.unityApplicationData.valueOrNull?.applicationVersion
    }

    fun requiresRiderPackage(): Boolean {
        return project.solution.rdUnityModel.unityApplicationData.valueOrNull?.requiresRiderPackage ?: return false
    }
}