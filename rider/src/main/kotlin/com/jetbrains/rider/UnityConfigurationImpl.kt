package com.jetbrains.rider

import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectView.indexing.contentModel.ContentModelExcludes
import com.jetbrains.rider.projectView.indexing.contentModel.ContentModelUserStore
import com.jetbrains.rider.projectView.indexing.contentModel.tryInclude
import java.io.File

class UnityConfigurationImpl(private val project: Project, unityProjectDiscoverer: UnityProjectDiscoverer,
                             excludes: ContentModelExcludes, userStore: ContentModelUserStore) {

    companion object {
        private val ignoredDirectories = arrayOf("Library", "Temp")
    }

    init {
        if (unityProjectDiscoverer.isUnityProject) {
            val excludePaths = ignoredDirectories
                    .mapNotNull(::getChildAsFile)
                    .toHashSet()
            excludes.updateExcludes(this, excludePaths)

            getChildAsFile("Packages")?.let { userStore.tryInclude(arrayOf(it)) }
        }
    }

    private fun getChildAsFile(directoryName: String) : File?
    {
        val libraryFolder = project.projectDir.findChild(directoryName)
        val path = libraryFolder?.canonicalPath ?: return null
        return File(path)
    }
}