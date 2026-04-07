package com.jetbrains.rider

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.refreshAndFindVirtualDirectory
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.solutionDirectoryPath

val Project.projectDir: VirtualFile
    get() = this.solutionDirectoryPath.refreshAndFindVirtualDirectory() ?: error("Virtual file not found for solution directory: ${this.solutionDirectoryPath}")