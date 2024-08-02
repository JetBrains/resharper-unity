package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.unityPackages.spellchecker

import com.intellij.json.JsonDialectUtil
import com.intellij.json.JsonSpellcheckerStrategy
import com.intellij.openapi.project.DumbAware
import com.intellij.psi.PsiElement
import com.intellij.spellchecker.tokenizer.Tokenizer

class UnityPackagesSpellcheckerStrategy : JsonSpellcheckerStrategy(), DumbAware {
    private val MANIFEST_DEFAULT_FILENAME: String = "manifest.json"
    private val PACKAGE_LOCK_DEFAULT_FILENAME: String = "packages-lock.json"
    private val PACKAGES_FOLDER_NAME = "Packages"

    // There is nothing in a manifest or a package-lock file that we want to spell check
    override fun getTokenizer(element: PsiElement): Tokenizer<PsiElement> = EMPTY_TOKENIZER

    override fun isMyContext(element: PsiElement): Boolean {
        val file = element.containingFile ?: return false
        val fileName = file.name

        //check if it's unity packages-related files
        if(MANIFEST_DEFAULT_FILENAME != fileName && PACKAGE_LOCK_DEFAULT_FILENAME != fileName)
            return false

        //check if this file is in Packages folder
        val parentFolderName = file.parent?.name
        if(parentFolderName != PACKAGES_FOLDER_NAME)
            return false

        return JsonDialectUtil.isStandardJson(element)
    }
}
