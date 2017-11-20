package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.openapi.fileTypes.*

// Ideally, this should come automatically from ReSharper, via catalog.json
// See RSRP-467093 and RIDER-11470
class AsmDefFileTypeFactory : FileTypeFactory() {

    override fun createFileTypes(consumer: FileTypeConsumer) {
        consumer.consume(com.intellij.json.JsonFileType.INSTANCE, ExtensionFileNameMatcher("asmdef"))
    }
}
