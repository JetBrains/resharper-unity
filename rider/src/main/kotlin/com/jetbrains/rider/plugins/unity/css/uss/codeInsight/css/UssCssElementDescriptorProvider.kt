package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css

import com.intellij.css.util.CssConstants
import com.intellij.openapi.util.text.StringUtil
import com.intellij.psi.PsiElement
import com.intellij.psi.css.CssElementDescriptorProvider
import com.intellij.psi.css.CssFunction
import com.intellij.psi.css.CssPropertyDescriptor
import com.intellij.psi.css.CssSimpleSelector
import com.intellij.psi.css.descriptor.CssFunctionDescriptor
import com.intellij.psi.css.descriptor.CssPseudoSelectorDescriptor
import com.intellij.psi.css.descriptor.value.CssValueDescriptor
import com.intellij.psi.css.impl.descriptor.value.CssValueValidatorImpl
import com.intellij.psi.css.impl.util.scheme.CssElementDescriptorFactory2
import com.intellij.psi.css.impl.util.scheme.CssElementDescriptorProviderImpl
import com.intellij.psi.css.impl.util.table.CssDescriptorsUtil.filterDescriptorsByContext
import com.intellij.psi.util.PsiTreeUtil
import com.jetbrains.rider.plugins.unity.css.uss.UssLanguage

class UssCssElementDescriptorProvider : CssElementDescriptorProvider() {
    private val factory
        get() = UssCssElementDescriptorFactory.getInstance().getDescriptors()

    override fun isMyContext(psiElement: PsiElement?) = psiElement?.containingFile?.language is UssLanguage

    // Allow all type names as simple selectors for now (e.g. TextField { ... })
    // Future: Implement getSimpleSelectors to list all possible type names and remove this
    override fun isPossibleSelector(selector: String, context: PsiElement) = true

    override fun getDeclarationsForSimpleSelector(selector: CssSimpleSelector): Array<PsiElement> {
        return arrayOf(selector)
    }

    // Pseudo selectors, e.g. :hover
    override fun findPseudoSelectorDescriptors(name: String, context: PsiElement?): MutableCollection<out CssPseudoSelectorDescriptor> {
        return factory.pseudoSelectors.get(StringUtil.toLowerCase(name))
    }

    override fun getAllPseudoSelectorDescriptors(context: PsiElement?): MutableCollection<out CssPseudoSelectorDescriptor> {
        return filterDescriptorsByContext(factory.pseudoSelectors.values(), context)
    }

    // Properties - e.g. border, padding-left, etc.
    override fun findPropertyDescriptors(propertyName: String, context: PsiElement?): MutableCollection<out CssPropertyDescriptor> {
        return factory.properties.get(StringUtil.toLowerCase(propertyName))
    }

    override fun getAllPropertyDescriptors(context: PsiElement?): MutableCollection<out CssPropertyDescriptor> {
        return filterDescriptorsByContext(factory.properties.values(), context)
    }

    override fun findFunctionDescriptors(functionName: String, context: PsiElement?): MutableCollection<out CssFunctionDescriptor> {
        val function = PsiTreeUtil.getNonStrictParentOfType(context, CssFunction::class.java)
        // handling 'var' function that could be applied to ANY property value
        return if (functionName.equals(CssConstants.VAR_FUNCTION_NAME, ignoreCase = true) && function != null) {
            CssElementDescriptorProviderImpl.getVarFunctionDescriptors(context!!, function)
        }
        else factory.functions.get(StringUtil.toLowerCase(functionName))
    }

    // Named values - define a type to reuse in the descriptors file
    // E.g. define a type called "margin-width" and then define "margin-top" in terms of "<margin-width> | inherit"
    override fun getNamedValueDescriptors(name: String, parent: CssValueDescriptor?): MutableCollection<out CssValueDescriptor> {
        return factory.namedValues.get(StringUtil.toLowerCase(name))
    }

    // Validator for property values, especially enums
    override fun getValueValidator() = CssValueValidatorImpl(this)

    override fun shouldAskOtherProviders(context: PsiElement?) = false
    override fun providesClassicCss() = false
}
