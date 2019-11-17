package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uss.codeInsight.css.inspections

import com.intellij.psi.PsiElement
import com.intellij.psi.css.inspections.CssApiBaseInspection
import com.intellij.psi.css.inspections.CssInspectionFilter
import com.intellij.psi.css.inspections.bugs.CssUnitlessNumberInspection

class UssCssInspectionFilter: CssInspectionFilter() {
    override fun isSupported(clazz: Class<out CssApiBaseInspection>, context: PsiElement): Boolean {
        // The CssDescriptorsLoader class creates instances of CssPropertyDescriptorImplEx, which returns false to
        // CssPropertyDescriptor.allowsIntegerWithoutSuffix, which means this inspection fires on <number> which is
        // wrong. The standard CSS elements from CssElementDescriptorProviderImpl appear to be loaded twice. Once with
        // CssDescriptorsLoader via (CssElementDescriptorFactory2) and once with CssElementDescriptorFactory. The
        // CssPropertyDescriptorImpl instances returns an appropriate value for allowsIntegerWithoutSuffix, so the
        // standard properties don't have this problem
        return clazz != CssUnitlessNumberInspection::class.java
    }
}
