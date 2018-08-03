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

    fun getDocumentationRoot(): Path? {
        return getApplicationContentsPath()?.resolve("Documentation/en")
    }

    // The same as EditorApplication.applicationContentsPath
    // E.g. Mac: /Applications/Unity/Hub/Editor/2018.2.0f2/Unity.app/Contents
    // Windows: C:\Program Files\Unity\Hub\Editor\2018.2.1f1\Editor\Data
    // TODO: What is Linux path?
    private fun getApplicationContentsPath(): Path? {
        val contentsPath = getApplicationContentsPathFromProtocol()
        if (contentsPath != null) return contentsPath

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

    private fun getApplicationContentsPathFromProtocol(): Path? {
        return project.solution.rdUnityModel.applicationContentsPath.valueOrNull?.let { Paths.get(it) }
    }

    private fun getApplicationPathFromProtocol(): Path? {
        return project.solution.rdUnityModel.applicationPath.valueOrNull?.let { Paths.get(it) }
    }

    private fun getApplicationPathFromEditorInstanceJson(): Path? {
        // Get the version from Library/EditorInstance.json and try and heuristically try to find the application.
        // Later versions of Unity will write this file, and it will only exist while the project is open. The protocol
        // is more accurate, as it will give us the actual application path
        return null
    }

    private fun getApplicationPathFromProjectVersion(): Path? {
        // Get the version from ProjectSettings/ProjectVersion.txt, and heuristically try to find the application.
        // This is a best effort attempt to find the application, as the version is the version of Unity that last saved
        // the project, rather than last opened it, and we're guessing where the application folder is
        return null
    }
}