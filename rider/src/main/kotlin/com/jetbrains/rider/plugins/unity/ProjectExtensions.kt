package com.jetbrains.rider

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.projectView.solutionDirectory

val Project.projectDir: VirtualFile
    get() = this.solutionDirectory.toVirtualFile(true) ?: error("Virtual file not found for solution directory: ${this.solutionDirectory}")