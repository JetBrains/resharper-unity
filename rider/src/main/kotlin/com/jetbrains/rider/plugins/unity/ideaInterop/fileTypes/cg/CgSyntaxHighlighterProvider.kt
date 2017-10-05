package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.fileTypes.SyntaxHighlighter
import com.intellij.openapi.fileTypes.SyntaxHighlighterFactory
import com.intellij.openapi.fileTypes.SyntaxHighlighterProvider
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile

class CgSyntaxHighlighterProvider : SyntaxHighlighterProvider, SyntaxHighlighterFactory() {
    override fun getSyntaxHighlighter(project: Project?, file: VirtualFile?): SyntaxHighlighter {
        return CgSyntaxHighlighter()
    }

    override fun create(fileType: FileType, project: Project?, file: VirtualFile?): SyntaxHighlighter? {
        if (fileType !is CgFileType) return null
        return CgSyntaxHighlighter()
    }
}