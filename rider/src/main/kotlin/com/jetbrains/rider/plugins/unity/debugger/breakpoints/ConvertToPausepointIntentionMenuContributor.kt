package com.jetbrains.rider.plugins.unity.debugger.breakpoints

import com.intellij.codeInsight.daemon.impl.HighlightInfo
import com.intellij.codeInsight.daemon.impl.IntentionMenuContributor
import com.intellij.codeInsight.daemon.impl.ShowIntentionsPass
import com.intellij.codeInsight.intention.IntentionAction
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiFile
import com.intellij.util.ui.EmptyIcon
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.intellij.xdebugger.impl.breakpoints.XBreakpointUtil
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointType

// TODO: Replace this with an EP in DotNetLineBreakpointType.getAdditionalPopupMenuActions
// It's not really a good idea to add intentions to the "gutters" list of actions to show - this should only really be
// modified by GutterIntentionMenuContributor. We have virtually no control of ordering, as this is usually handled by
// the order property of GutterIntentionAction, which is package private, and unavailable to us. Even order of the EP
// registration doesn't affect order.
// Ideally, we should be adding actions via XBreakpointType.getAdditionalPopupMenuActions
@Suppress("UnstableApiUsage")
class ConvertToPausepointIntentionMenuContributor : IntentionMenuContributor {
    override fun collectActions(editor: Editor, psiFile: PsiFile, intentionsInfo: ShowIntentionsPass.IntentionsInfo, passIdToShowIntentionsFor: Int, offset: Int) {
        val project = editor.project ?: return

        val pair = XBreakpointUtil.findSelectedBreakpoint(project, editor)
        val breakpoint = pair.second as? XLineBreakpoint<*> ?: return

        if (breakpoint.type is DotNetLineBreakpointType && breakpoint.type !is UnityPausepointBreakpointType) {
            val descriptor = HighlightInfo.IntentionActionDescriptor(ConvertToPausepointIntentionAction(), EmptyIcon.ICON_16)
            intentionsInfo.guttersToShow.add(descriptor)
        }
    }

    class ConvertToPausepointIntentionAction : IntentionAction, DumbAware {
        // Oof. What's that smell?
        // Actions are added to a set, and then sorted by GutterIntentionAction.order, if the IntentionAction is an
        // instance of GutterIntentionAction, which is unlikely, as it's package private. Other items are declared as
        // "equal" in the comparable, so it then comes down to the order in the set, which is based on hash code.
        // The additional "  " here is a really horrible hack to modify the hash code to push us down the list of
        // actions, although it's not enough to push us the bottom. Implementing Comparable doesn't help, due to the
        // algorithm used to compare items. A much better solution would be to extend
        // DotNetLineBreakpointType.getAdditionalPopupMenuActions
        override fun getText() = UnityPausepointConstants.convertToPausepointText + "  "
        override fun startInWriteAction() = false
        override fun getFamilyName() = "UNUSED"
        override fun isAvailable(project: Project, editor: Editor?, psiFile: PsiFile?) = true

        override fun invoke(project: Project, editor: Editor?, psiFile: PsiFile?) {
            if (editor == null) return

            val pair = XBreakpointUtil.findSelectedBreakpoint(project, editor)
            val breakpointGutterRenderer = pair.first
            val breakpoint = pair.second as? XLineBreakpoint<*>
            if (breakpointGutterRenderer == null || breakpoint == null) {
                return
            }

            if (breakpoint.type is DotNetLineBreakpointType && breakpoint.type !is UnityPausepointBreakpointType) {
                @Suppress("UNCHECKED_CAST")
                convertToPausepoint(project, breakpoint as XLineBreakpoint<DotNetLineBreakpointProperties>, editor, breakpointGutterRenderer)
            }
        }
    }
}
