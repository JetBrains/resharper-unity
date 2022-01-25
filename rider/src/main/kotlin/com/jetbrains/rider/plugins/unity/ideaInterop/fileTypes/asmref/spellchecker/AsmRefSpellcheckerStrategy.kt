package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmref.spellchecker

import com.intellij.json.JsonSpellcheckerStrategy
import com.intellij.psi.PsiElement
import com.intellij.spellchecker.tokenizer.Tokenizer
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmref.AsmRefFileType

class AsmRefSpellcheckerStrategy : JsonSpellcheckerStrategy() {

    // There is nothing in a .asmref file that we want to spell check
    override fun getTokenizer(element: PsiElement): Tokenizer<PsiElement> = EMPTY_TOKENIZER

    override fun isMyContext(element: PsiElement): Boolean {
        val viewProvider = element.containingFile?.viewProvider ?: return false
        return viewProvider.fileType == AsmRefFileType
    }
}
