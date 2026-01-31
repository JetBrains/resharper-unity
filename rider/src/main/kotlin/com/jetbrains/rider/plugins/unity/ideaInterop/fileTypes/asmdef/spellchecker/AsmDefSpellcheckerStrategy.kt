package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef.spellchecker

import com.intellij.json.JsonSpellcheckerStrategy
import com.intellij.json.psi.JsonArray
import com.intellij.json.psi.JsonFile
import com.intellij.json.psi.JsonObject
import com.intellij.json.psi.JsonProperty
import com.intellij.json.psi.JsonStringLiteral
import com.intellij.openapi.project.DumbAware
import com.intellij.psi.PsiElement
import com.intellij.spellchecker.tokenizer.Tokenizer
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef.AsmDefFileType

class AsmDefSpellcheckerStrategy : JsonSpellcheckerStrategy(), DumbAware {
    override fun getTokenizer(element: PsiElement): Tokenizer<*> {

        // Only spell check string literals for things we define, which is name, rootNamespace and versionDefines.define
        if (element is JsonStringLiteral) {
            val property = element.parent
            if (property is JsonProperty) {
                if (property.name == "name" || property.name == "rootNamespace") {
                    val rootObject = property.parent as? JsonObject
                    val file = rootObject?.parent as? JsonFile
                    if (file != null)
                        return super.getTokenizer(element)
                }

                if (property.name == "define") {
                    val versionDefinesObject = property.parent as? JsonObject
                    val versionDefinesArray = versionDefinesObject?.parent as? JsonArray
                    val versionDefinesProperty = versionDefinesArray?.parent as? JsonProperty
                    if (versionDefinesProperty?.name == "versionDefines") {
                        return super.getTokenizer(element)
                    }
                }
            }
        }

        return EMPTY_TOKENIZER
    }

    override fun isMyContext(element: PsiElement): Boolean {
        val viewProvider = element.containingFile?.viewProvider ?: return false
        return viewProvider.fileType == AsmDefFileType
    }
}
