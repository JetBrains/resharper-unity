package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.openapi.fileTypes.*

// We can't just say that .asmdef files are JSON files, because Rider treats JSON files as frontend only files. We can't
// override that for only .asmdef files, and even if we did, we'd get duplicate inspections and quick fixes because we'd
// have both the frontend and the backend handling schema validation. Instead, we create our own frontend file type, and
// let the backend do all of the work (syntax highlighting, folding, schema validation/inspections and asmdef specific
// inspections)
class AsmDefFileTypeFactory : FileTypeFactory() {
    override fun createFileTypes(consumer: FileTypeConsumer) {
        consumer.consume(AsmDefFileType)
    }
}
