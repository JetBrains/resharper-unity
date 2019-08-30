package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.projectDir
import java.nio.file.Paths

fun Project.refreshAndFindFile(relativeFile: String): VirtualFile? {
    val path = Paths.get(this.projectDir.path, relativeFile)
    return VfsUtil.findFile(path, true)
}

fun Project.findFile(relativeFile: String): VirtualFile? {
    return this.projectDir.findFileByRelativePath(relativeFile)
}