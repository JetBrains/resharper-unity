package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.openapi.actionSystem.IdeActions
import com.intellij.psi.PsiElement
import com.jetbrains.rdclient.hyperlinks.FrontendCtrlClickHost
import com.jetbrains.rider.actions.RiderActionCallStrategy
import com.jetbrains.rider.actions.RiderActionSupportPolicy
import com.jetbrains.rider.actions.RiderActions

class AsmDefActionCallPolicy : RiderActionSupportPolicy() {
    override fun getCallStrategy(psiElement: PsiElement, backendActionId: String): RiderActionCallStrategy  =
        when (backendActionId) {
            IdeActions.ACTION_RENAME,
            IdeActions.ACTION_FIND_USAGES,
            FrontendCtrlClickHost.backendActionId,
            RiderActions.GOTO_DECLARATION -> RiderActionCallStrategy.BACKEND_FIRST
            else -> RiderActionCallStrategy.FRONTEND_ONLY
        }

    override fun isAvailable(psiElement: PsiElement, backendActionId: String): Boolean {
        val viewProvider = psiElement.containingFile?.viewProvider ?: return false
        return viewProvider.fileType == AsmDefFileType
    }
}