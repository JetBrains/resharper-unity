package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.project.Project
import com.intellij.openapi.vcs.FilePath
import com.intellij.openapi.vcs.changes.IgnoredFileDescriptor
import com.intellij.openapi.vcs.changes.IgnoredFileProvider
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.UnityProjectDiscoverer
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
        if (!UnityProjectDiscoverer.getInstance(project).isUnityProject)
            return false;

        val ignoredFolders = arrayOf(
            File(project.solutionDirectory, "Library"),
            File(project.solutionDirectory, "library"),
            File(project.solutionDirectory, "Temp"),
            File(project.solutionDirectory, "temp"),
            File(project.solutionDirectory, "Obj"),
            File(project.solutionDirectory, "obj"),
            File(project.solutionDirectory, "Build"),
            File(project.solutionDirectory, "build"),
            File(project.solutionDirectory, "Builds"),
            File(project.solutionDirectory, "builds"),
            File(project.solutionDirectory, "Logs"),
            File(project.solutionDirectory, "logs"),
            File(project.solutionDirectory, "MemoryCaptures"),
            File(project.solutionDirectory, "memoryCaptures"),
            getPluginPath(File(project.solutionDirectory, "Assets")),
            getPluginPath(File(project.solutionDirectory, "assets"))
        )

        val name = filePath.name
        if (name == "sysinfo.txt" || name == "crashlytics-build.properties")
            return true;

       for (ext in ignoredExtensions)
           if (name.endsWith(ext))
               return true

        for (ignoredFolder in ignoredFolders)
            if (VfsUtil.isAncestor(ignoredFolder, filePath.ioFile, false))
                return true

        return false
    }


    private fun getPluginPath(file : File) : File {
        return file.resolve("Plugins/Editor/Jetbrains")
    }
    override fun getIgnoredGroupDescription(): String {
       return "Unity ignored files"
    }

    override fun getIgnoredFiles(project: Project): MutableSet<IgnoredFileDescriptor> {
        return mutableSetOf()
    }

}