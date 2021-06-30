package com.jetbrains.rider

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VirtualFile

val Project.projectDir: VirtualFile
    get() = LocalFileSystem.getInstance().findFileByPath(this.basePath!!)!!