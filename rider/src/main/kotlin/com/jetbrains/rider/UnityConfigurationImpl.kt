package com.jetbrains.rider

import com.intellij.openapi.project.Project

class UnityConfigurationImpl(project: Project, unityReferenceDiscoverer: UnityReferenceDiscoverer) {

    init {
        unityReferenceDiscoverer.addUnityReferenceListener(object : UnityReferenceListener {
            override fun HasUnityReference() {
                ExcludeFolderFromContentStore(project, "Library")
                ExcludeFolderFromContentStore(project, "Temp")
            }
        })
    }

    private fun ExcludeFolderFromContentStore(project: Project, folderName: String) {
//        val libraryFolder = project.baseDir.findChild(folderName)
//        if (libraryFolder != null && !contentModelStore.hasExcludedFile(libraryFolder)) {
//            contentModelStore.addExcludedFile(libraryFolder)
//        }
    }
}