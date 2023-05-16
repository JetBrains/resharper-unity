package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.inspections

import com.intellij.codeInspection.InspectionProfileEntry
import com.intellij.codeInspection.InspectionSuppressor
import com.intellij.codeInspection.SuppressQuickFix
import com.intellij.psi.PsiElement
import com.intellij.psi.css.inspections.invalid.CssUnknownTargetInspection
import com.jetbrains.rider.plugins.unity.css.uss.UssLanguage

class UssInspectionSuppressor : InspectionSuppressor {
    override fun isSuppressedFor(element: PsiElement, toolId: String): Boolean {
        val containingFile = element.containingFile ?: return false
        return containingFile.language == UssLanguage &&
               toolId == InspectionProfileEntry.getShortName(CssUnknownTargetInspection::class.java.simpleName)
    }

    override fun getSuppressActions(element: PsiElement?, toolId: String): Array<SuppressQuickFix> {
        return arrayOf()
    }
}
