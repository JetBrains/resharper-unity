package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.util.SystemProperties
import com.intellij.util.io.exists
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
    // Linux: /home/ivan/Unity-2018.1.0f2/Editor/Data
    private fun getApplicationContentsPath(): Path? {
        val contentsPath = getApplicationContentsPathFromProtocol()
        if (contentsPath != null) return contentsPath

        val root = getApplicationPath() ?: return null
        return when {
            SystemInfo.isMac -> root.resolve("Contents")
            SystemInfo.isWindows -> root.parent.resolve("Data")
            SystemInfo.isLinux -> root.parent.resolve("Data")
            else -> null
        }
    }

    // The same as EditorApplication.applicationPath, minus the executable name
    // Note that on Mac, the Unity.app remains, as this is also a folder
    // E.g. Mac: /Applications/Unity/Hub/Editor/2018.2.0f2/Unity.app
    // Windows: C:\Program Files\Unity\Hub\Editor\2018.2.1f1\Editor\Unity.exe
    // Linux: /home/ivan/Unity-2018.1.0f2/Editor/Unity
    private fun getApplicationPath(): Path? {
        return tryGetApplicationPathFromProtocol()
            ?: tryGetApplicationPathFromEditorInstanceJson()
            ?: tryGetApplicationPathFromProjectVersion()
    }

    private fun getApplicationContentsPathFromProtocol(): Path? {
        return project.solution.rdUnityModel.applicationContentsPath.valueOrNull?.let { Paths.get(it) }
    }

    private fun tryGetApplicationPathFromProtocol(): Path? {
        return project.solution.rdUnityModel.applicationPath.valueOrNull?.let { Paths.get(it) }
    }

    private fun tryGetApplicationPathFromEditorInstanceJson(): Path? {
        // Get the version from Library/EditorInstance.json and try to heuristically find the application. The plugin
        // and later  versions of Unity will write this file, and it will only exist while the project is open. The
        // protocol is more accurate, as it will give us the actual application path
        val (status, editorInstanceJson) = EditorInstanceJson.load(project)
        if (status == EditorInstanceJsonStatus.Valid) {
            return tryGetApplicationPathFromVersion(editorInstanceJson!!.version)
        }
        return null
    }

    private fun tryGetApplicationPathFromProjectVersion(): Path? {
        // Get the version from ProjectSettings/ProjectVersion.txt, and heuristically try to find the application.
        // This is a best effort attempt to find the application, as the version is the version of Unity that last saved
        // the project, rather than last opened it, and we're guessing where the application folder is
        val projectVersionTxt = project.refreshAndFindFile("ProjectSettings/ProjectVersion.txt") ?: return null
        val text = VfsUtil.loadText(projectVersionTxt)
        val result = Regex("""m_EditorVersion: (?<version>.*$)""").find(text)
        return result?.let { it.groups["version"]?.value }?.let { tryGetApplicationPathFromVersion(it) }
    }

    private fun tryGetApplicationPathFromVersion(version: String): Path? {
        // Best guess, based on version and default install locations
        return tryGetApplicationPathFromDefaultHubInstallLocation(version)
                ?: tryGetApplicationPathFromDefaultInstallLocation(version)
    }

    private val programFiles: String
        // Unity is installed to `C:\Program Files\Unity`, on both 32 bit and 64 bit
        // %PROGRAMFILES% differs if we're a 32 or 64 bit process
        // %PROGRAMFILES(X86)% only exists if we're 64 bit, and is always in the form `C:\Program Files (x86)`
        // %PROGRAMW6432% is always in the form `C:\Program Files`
        get() = System.getenv("ProgramW6432") ?: System.getenv("ProgramFiles") ?: ""

    private fun tryGetApplicationPathFromDefaultHubInstallLocation(version: String): Path? = when {
        SystemInfo.isWindows -> Paths.get("$programFiles/Unity/Hub/Editor/$version/Editor/Unity.exe")
        SystemInfo.isMac -> Paths.get("/Applications/Unity/Hub/Editor/$version/Unity.app")
        // Hub isn't supported on Linux (yet?)
        else -> null
    }?.takeIf { it.exists() }

    private fun tryGetApplicationPathFromDefaultInstallLocation(version: String) = when {
        SystemInfo.isWindows -> Paths.get("$programFiles/Unity/Editor/Unity.exe")
        SystemInfo.isMac -> Paths.get("/Applications/Unity/Unity.app")
        SystemInfo.isLinux -> {
            Paths.get("/opt/Unity-$version/Editor/Unity").takeIf { it.exists() }
                    ?: Paths.get("${SystemProperties.getUserHome()}/Unity-$version/Editor/Unity")
        }
        else -> null
    }?.takeIf { it.exists() }
}