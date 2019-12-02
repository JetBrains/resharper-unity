package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.openapi.actionSystem.IdeActions
import com.intellij.psi.PsiElement
import com.jetbrains.rider.actions.RiderActionCallStrategy
import com.jetbrains.rider.actions.RiderActionSupportPolicy
import com.jetbrains.rider.actions.RiderActions

class AsmDefActionCallPolicy : RiderActionSupportPolicy() {
    override fun getCallStrategy(psiElement: PsiElement, backendActionId: String): RiderActionCallStrategy {
        if (backendActionId == IdeActions.ACTION_RENAME)
            return RiderActionCallStrategy.FRONTEND_FIRST
        if (RiderActions.GOTO_DECLARATION == backendActionId || IdeActions.ACTION_FIND_USAGES == backendActionId
            || backendActionId == "CtrlClick")
            return RiderActionCallStrategy.BACKEND_FIRST

        return RiderActionCallStrategy.FRONTEND_ONLY
    }

    override fun isAvailable(psiElement: PsiElement, backendActionId: String): Boolean {
        val viewProvider = psiElement.containingFile?.viewProvider ?: return false
        return viewProvider.fileType == AsmDefFileType
    }
}