package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.intellij.openapi.fileTypes.FileTypeConsumer
import com.intellij.openapi.fileTypes.FileTypeFactory

class ShaderLabFileTypeFactory : FileTypeFactory() {
    override fun createFileTypes(consumer: FileTypeConsumer) {
        consumer.consume(ShaderLabFileType)
    }
}

