package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.fileTypes.SyntaxHighlighter
import com.intellij.openapi.fileTypes.SyntaxHighlighterFactory
import com.intellij.openapi.fileTypes.SyntaxHighlighterProvider
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile

class AsmDefSyntaxHighlighterProvider : SyntaxHighlighterProvider, SyntaxHighlighterFactory() {
    override fun getSyntaxHighlighter(project: Project?, file: VirtualFile?): SyntaxHighlighter {
        return AsmDefSyntaxHighlighter()
    }

    override fun create(fileType: FileType, project: Project?, file: VirtualFile?): SyntaxHighlighter? {
        if (fileType !is AsmDefFileType) return null
        return AsmDefSyntaxHighlighter()
    }
}
