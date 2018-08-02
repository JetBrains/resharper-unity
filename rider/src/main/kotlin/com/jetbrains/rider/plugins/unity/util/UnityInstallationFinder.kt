package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.util.idea.tryGetComponent
import java.nio.file.Path

class UnityInstallationFinder(private val project: Project) {

    companion object {
        fun getInstance(project: Project): UnityInstallationFinder {
            return UnityInstallationFinder(project)
        }
    }

    fun getBuiltInPackagesRoot(): Path? {
        return getApplicationContentsPath()?.resolve("Resources/PackageManager/BuiltInPackages")
    }

    fun getDocumentationRoot(): Path? {
        return getApplicationContentsPath()?.resolve("Documentation/en")
    }

    // The same as EditorApplication.applicationContentsPath
    // E.g. Mac: /Applications/Unity/Hub/Editor/2018.2.0f2/Unity.app/Contents
    // Windows: C:\Program Files\Unity\Hub\Editor\2018.2.1f1\Editor\Data
    // TODO: What is Linux path?
    private fun getApplicationContentsPath(): Path? {
        val root = getApplicationPath() ?: return null
        return when {
            SystemInfo.isMac -> root.resolve("Contents")
            SystemInfo.isWindows -> root.resolve("Data")
            SystemInfo.isLinux -> root.resolve("Data")  // TODO: Confirm this
            else -> null
        }
    }

    // The same as EditorApplication.applicationPath, minus the executable name
    // Note that on Mac, the Unity.app remains, as this is also a folder
    // E.g. Mac: /Applications/Unity/Hub/Editor/2018.2.0f2/Unity.app
    // Windows: C:\Program Files\Unity\Hub\Editor\2018.2.1f1\Editor\Unity.exe
    // TODO: What is Linux path?
    private fun getApplicationPath(): Path? {
        return getApplicationPathFromProtocol()
            ?: getApplicationPathFromEditorInstanceJson()
            ?: getApplicationPathFromProjectVersion()
    }

    private fun getApplicationPathFromProtocol(): Path? {
        val unityHost = project.tryGetComponent<UnityHost>() ?: return null
        // TODO: Add app_path and app_contents_path to protocol
        return null
    }

    private fun getApplicationPathFromEditorInstanceJson(): Path? {
        return null
    }

    private fun getApplicationPathFromProjectVersion(): Path? {
        return null
    }
}