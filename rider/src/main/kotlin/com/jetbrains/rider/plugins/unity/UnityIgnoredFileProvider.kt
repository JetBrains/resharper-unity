package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.project.Project
import com.intellij.openapi.vcs.FilePath
import com.intellij.openapi.vcs.changes.IgnoredFileDescriptor
import com.intellij.openapi.vcs.changes.IgnoredFileProvider
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.projectView.solutionDirectory
import java.io.File

class UnityIgnoredFileProvider : IgnoredFileProvider {

    companion object {
        val ignoredExtensions = hashSetOf(
            ".apk",
            ".unitypackage",
            ".pidb.meta",
            "mdb.meta",
            "pdb.meta"
        )
    }

    override fun isIgnoredFile(project: Project, filePath: FilePath): Boolean {
        if (!project.isUnityProject.value)
            return false

        val solDir = project.solutionDirectory
        val ignoredFolders = arrayOf(
            File(solDir, "Library"),
            File(solDir, "library"),
            File(solDir, "Temp"),
            File(solDir, "temp"),
            File(solDir, "Obj"),
            File(solDir, "obj"),
            File(solDir, "Build"),
            File(solDir, "build"),
            File(solDir, "Builds"),
            File(solDir, "builds"),
            File(solDir, "Logs"),
            File(solDir, "logs"),
            File(solDir, "MemoryCaptures"),
            File(solDir, "memoryCaptures"),
            getPluginPath(File(solDir, "Assets")),
            getPluginPath(File(solDir, "assets"))
        )

        val name = filePath.name
        if (name == "sysinfo.txt" || name == "crashlytics-build.properties")
            return true

        for (ext in ignoredExtensions)
            if (name.endsWith(ext))
                return true

        for (ignoredFolder in ignoredFolders)
            if (VfsUtil.isAncestor(ignoredFolder, filePath.ioFile, false))
                return true

        return false
    }


    private fun getPluginPath(file: File): File {
        return file.resolve("Plugins/Editor/Jetbrains")
    }

    override fun getIgnoredGroupDescription(): String {
        return UnityBundle.message("text.unity.ignored.files")
    }

    override fun getIgnoredFiles(project: Project): MutableSet<IgnoredFileDescriptor> {
        return mutableSetOf()
    }

}