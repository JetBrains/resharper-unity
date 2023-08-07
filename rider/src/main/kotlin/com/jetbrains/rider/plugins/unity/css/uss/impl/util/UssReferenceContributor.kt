package com.jetbrains.rider.plugins.unity.css.uss.impl.util

import com.intellij.patterns.PlatformPatterns
import com.intellij.psi.PsiReferenceContributor
import com.intellij.psi.PsiReferenceRegistrar
import com.intellij.psi.css.CssString
import com.intellij.psi.filters.position.FilterPattern


class UssReferenceContributor : PsiReferenceContributor() {
    override fun registerReferenceProviders(registrar: PsiReferenceRegistrar) {
        registerUssReferenceProviders(registrar)
    }

    private fun registerUssReferenceProviders(registrar: PsiReferenceRegistrar) {
        val elementFilter = UssReferenceProvider.UssReferenceFilter()
        val provider = UssReferenceProvider()
        registrar.registerReferenceProvider(PlatformPatterns.psiElement(
            CssString::class.java).and(FilterPattern(elementFilter)), provider)
    }
}

