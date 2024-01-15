package com.jetbrains.rider.plugins.unity.ui.unitTesting

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.ex.ComboBoxAction
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnitTestLaunchPreference
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import org.jetbrains.annotations.Nls
import javax.swing.JComponent

class SwitchUnitTestLauncherComboBoxAction : ComboBoxAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    @Nls
    private fun getLauncherDescription(currentPreference: UnitTestLaunchPreference?): String {
        val preferenceNotNull = currentPreference ?: return UseNUnitLauncherAction.UseNUnitLauncherActionText

        return when (preferenceNotNull) {
            UnitTestLaunchPreference.EditMode -> UseUnityEditLauncherAction.EditModeText
            UnitTestLaunchPreference.NUnit -> UseNUnitLauncherAction.UseNUnitLauncherActionText
            UnitTestLaunchPreference.PlayMode -> UseUnityPlayLauncherAction.PlayModeDescription
            UnitTestLaunchPreference.Both -> UseUnityBothLauncherAction.BothModeText
        }
    }

    override fun createPopupActionGroup(p0: JComponent?): DefaultActionGroup {
        return object : DefaultActionGroup(UseUnityEditLauncherAction(), UseUnityPlayLauncherAction(), UseUnityBothLauncherAction(), UseNUnitLauncherAction()) {
            init {
              templatePresentation.isPopupGroup = true
            }
            override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
            override fun update(e: AnActionEvent) {
                val project = e.project ?: return

                val currentPreference = project.solution.frontendBackendModel.unitTestPreference.value
                e.presentation.text = getLauncherDescription(currentPreference)

                e.presentation.description = getLauncherDescription(currentPreference)
                e.presentation.isEnabledAndVisible = true
                e.presentation.icon = AllIcons.General.Settings

                super.update(e)
            }
        }
    }

    override fun update(e: AnActionEvent) {

        val project = e.project ?: return
        val currentPreference = project.solution.frontendBackendModel.unitTestPreference.value
        e.presentation.text = getLauncherDescription(currentPreference)

        e.presentation.description = getLauncherDescription(currentPreference)
        e.presentation.isEnabledAndVisible = project.isUnityProject()

        super.update(e)
    }

    override fun shouldShowDisabledActions(): Boolean {
        return true
    }
}