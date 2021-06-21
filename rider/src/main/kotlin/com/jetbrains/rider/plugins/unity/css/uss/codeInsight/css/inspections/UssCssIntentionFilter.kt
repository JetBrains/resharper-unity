package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.inspections

import com.intellij.psi.css.actions.CssBaseElementAtCaretIntentionAction
import com.intellij.psi.css.actions.CssIntentionFilter
import com.intellij.psi.css.actions.colors.CssConvertToHslIntention
import com.intellij.psi.css.actions.colors.CssConvertToHwbIntention
import com.intellij.psi.css.actions.colors.CssReplaceWithColorNameIntention

class UssCssIntentionFilter: CssIntentionFilter() {
    override fun isSupported(clazz: Class<out CssBaseElementAtCaretIntentionAction>): Boolean {
        // USS doesn't support these colour types
        return clazz != CssConvertToHslIntention::class.java
            && clazz != CssConvertToHwbIntention::class.java
            && clazz != CssReplaceWithColorNameIntention::class.java
    }
}