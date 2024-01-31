package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.openapi.actionSystem.IdeActions
import com.intellij.psi.PsiElement
import com.jetbrains.rdclient.hyperlinks.FrontendCtrlClickHost
import com.jetbrains.rider.actions.RiderActionCallStrategy
import com.jetbrains.rider.actions.RiderActionSupportPolicy
import com.jetbrains.rider.actions.RiderActions

class AsmDefActionCallPolicy : RiderActionSupportPolicy() {
    override fun getCallStrategy(psiElement: PsiElement, backendActionId: String): RiderActionCallStrategy =
    // Note that this is backend action ID, which means it's the R# action ID, which may or may not be the same as the IdeAction.
        when (backendActionId) {
            "Rename",
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