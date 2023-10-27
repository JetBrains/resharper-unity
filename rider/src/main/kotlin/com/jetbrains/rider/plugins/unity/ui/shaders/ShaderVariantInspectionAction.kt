package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.actionSystem.ex.CustomComponentAction
import com.intellij.openapi.actionSystem.impl.ActionButtonWithText
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.ui.awt.RelativePoint
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
        ShaderVariantPopup.show(project, RelativePoint(component, (e.inputEvent as? MouseEvent)?.point ?: Point(0, 0)))
    }

    override fun update(e: AnActionEvent) {
        val editor = e.getData(CommonDataKeys.EDITOR)?.also {
            e.presentation.text = ShaderVariantPresence.get(it)?.let { shaderVariant ->
                "Active: ${shaderVariant.activeKeywords.size} Suppressed: ${shaderVariant.suppressedKeywords.size}"
            } ?: ""
        }
        e.presentation.isEnabledAndVisible = editor != null
    }
}