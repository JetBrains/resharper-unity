package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.openapi.fileTypes.ExtensionFileNameMatcher
import com.intellij.openapi.fileTypes.FileNameMatcher
import com.intellij.openapi.fileTypes.FileTypeConsumer
import com.intellij.openapi.fileTypes.FileTypeFactory

class CgFileTypeFactory : FileTypeFactory() {
    companion object {
        // Don't forget to update CgProjectFileType list on the backend
        val extensions: List<String> = arrayListOf("cginc", "compute", "hlsl", "glsl", "hlslinc", "glslinc")
    }

    private val matchers: List<FileNameMatcher> = extensions.map { ExtensionFileNameMatcher(it) }

    override fun createFileTypes(consumer: FileTypeConsumer) {
        for (matcher in matchers){
            consumer.consume(CgFileType, matcher)
        }
    }
}

