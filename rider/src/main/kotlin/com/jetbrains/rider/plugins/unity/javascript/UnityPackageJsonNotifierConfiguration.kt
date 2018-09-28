package com.jetbrains.rider.plugins.unity.javascript

import com.intellij.javascript.nodejs.packageJson.PackageJsonNotifierConfiguration
import com.intellij.openapi.fileTypes.FileTypeManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.isLikeUnityProject
import com.jetbrains.rider.plugins.javascript.nodejs.RiderPackageJsonConfigurator
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.indexing.contentModel.ContentModelUserStore

// This service overrides the implementation in Rider, which in turn overrides the implementation in IntelliJ
class UnityPackageJsonNotifierConfiguration(private val project: Project, projectModelHost: ProjectModelViewHost,
                                            store: ContentModelUserStore, fileTypeManager: FileTypeManager)
    : PackageJsonNotifierConfiguration {

    // The original Rider override
    private val originalConfigurator = RiderPackageJsonConfigurator(project, projectModelHost, store, fileTypeManager)

    override fun detectPackageJsonFiles() = originalConfigurator.detectPackageJsonFiles()

    override fun isEssential(packageJson: VirtualFile) = originalConfigurator.isEssential(packageJson)

    override fun isNpmPackageJson(packageJson: VirtualFile) =
        !project.isLikeUnityProject() && originalConfigurator.isNpmPackageJson(packageJson)
}