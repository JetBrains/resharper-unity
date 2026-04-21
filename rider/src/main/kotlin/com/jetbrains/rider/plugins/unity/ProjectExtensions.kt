package com.jetbrains.rider

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.projectView.solutionDirectory

val Project.projectDir: VirtualFile
    // changing to this.solutionDirectoryPath.refreshAndFindVirtualDirectory() is dangerous, may cause perf degradation
    get() = this.solutionDirectory.toVirtualFile(true) ?: error("Virtual file not found for solution directory: ${this.solutionDirectory}")