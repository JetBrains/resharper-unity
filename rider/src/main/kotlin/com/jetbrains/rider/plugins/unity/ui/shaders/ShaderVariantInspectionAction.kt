package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.actionSystem.ex.CustomComponentAction
import com.intellij.openapi.actionSystem.impl.ActionButtonWithText
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons
import java.awt.Point
import java.awt.event.MouseEvent
import javax.swing.JComponent

class ShaderVariantInspectionAction : DumbAwareAction(UnityIcons.FileTypes.ShaderLab), CustomComponentAction {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun createCustomComponent(presentation: Presentation, place: String): JComponent {
        return object : ActionButtonWithText(this, presentation, place, ActionToolbar.DEFAULT_MINIMUM_BUTTON_SIZE), ShaderVariantPresence.ChangeListener {
            private var shaderVariantPresence: ShaderVariantPresence? = null

            override fun addNotify() {
                super.addNotify()
                val dataContext = ActionToolbar.getDataContextFor(this)
                dataContext.getData(CommonDataKeys.EDITOR)?.let { editor ->
                    shaderVariantPresence = ShaderVariantPresence.ensure(editor).also { it.addChangeListener(this) }
                }
            }

            override fun removeNotify() {
                shaderVariantPresence?.removeChangeListener(this)
                super.removeNotify()
            }

            override fun shaderVariantKeywordsChanged() {
                val context = ActionToolbar.getDataContextFor(this)
                val actionEvent = AnActionEvent.createFromDataContext(place, presentation, context)
                action.update(actionEvent)
            }
        }
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val component = e.inputEvent?.component ?: return
        val showAt = RelativePoint(component, (e.inputEvent as? MouseEvent)?.point ?: Point(0, 0))
        ShaderVariantPopup.show(project, showAt)
    }

    override fun update(e: AnActionEvent) {
        val editor = e.getData(CommonDataKeys.EDITOR)?.also { editor ->
            e.presentation.text = ShaderVariantPresence.get(editor)?.let { shaderVariant ->
                val activeCount = shaderVariant.getActiveCountForEnabledOnly()
                val suppressedCount = shaderVariant.suppressedKeywords.size
                when {
                    activeCount > 0 && suppressedCount > 0 -> UnityBundle.message("widgets.shaderVariants.activeAndSuppressedKeywords", activeCount, suppressedCount)
                    activeCount > 0 -> UnityBundle.message("widgets.shaderVariants.activeKeywords", activeCount)
                    suppressedCount > 0 -> UnityBundle.message("widgets.shaderVariants.suppressedKeywords", suppressedCount)
                    else -> null
                }
            } ?: ""
        }
        e.presentation.isEnabledAndVisible = editor != null
    }
}