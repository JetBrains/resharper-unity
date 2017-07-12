package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.openapi.fileTypes.FileTypeConsumer
import com.intellij.openapi.fileTypes.FileTypeFactory

class CgFileTypeFactory : FileTypeFactory() {
    override fun createFileTypes(consumer: FileTypeConsumer) {
        consumer.consume(CgFileType)
    }
}

