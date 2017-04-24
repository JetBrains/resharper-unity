package com.jetbrains.resharper

import com.intellij.openapi.project.Project
import com.jetbrains.resharper.projectView.contentModel.RiderContentModelStore

class UnityConfigurationImpl(project : Project, unityReferenceDiscoverer: UnityReferenceDiscoverer, contentModelStore : RiderContentModelStore) {

    init {
        unityReferenceDiscoverer.addUnityReferenceListener(object : UnityReferenceListener {
            override fun HasUnityReference() {

                ExcludeFolderFromContentStore(contentModelStore, project, "Library")
                ExcludeFolderFromContentStore(contentModelStore, project, "Temp")
            }
        })
    }

    private fun ExcludeFolderFromContentStore(contentModelStore: RiderContentModelStore, project: Project, folderName: String) {
        val libraryFolder = project.baseDir.findChild(folderName)
        if (libraryFolder != null && !contentModelStore.hasExcludedFile(libraryFolder)) {
            contentModelStore.addExcludedFile(libraryFolder)
        }
    }
}