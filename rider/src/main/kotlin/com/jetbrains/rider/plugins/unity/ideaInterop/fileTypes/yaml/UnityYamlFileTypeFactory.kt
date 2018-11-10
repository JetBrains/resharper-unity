package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml

import com.intellij.openapi.fileTypes.ExtensionFileNameMatcher
import com.intellij.openapi.fileTypes.FileTypeConsumer
import com.intellij.openapi.fileTypes.FileTypeFactory

class UnityYamlFileTypeFactory : FileTypeFactory() {

    companion object {
        // This MUST match the list in UnityYamlFileExtensionMapping in the backend
        val extensions = arrayListOf("meta", "unity", "prefab", "asset")
    }

    private val matchers = extensions.map { ExtensionFileNameMatcher(it) }

    override fun createFileTypes(consumer: FileTypeConsumer) {
        for (matcher in matchers) {
            consumer.consume(UnityYamlFileType, matcher)
        }
    }
}
