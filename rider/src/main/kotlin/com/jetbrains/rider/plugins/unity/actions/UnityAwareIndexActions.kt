package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorer
import com.jetbrains.rider.projectView.actions.workspace.StartIndexAction
import com.jetbrains.rider.projectView.actions.workspace.StopIndexAction
import com.jetbrains.rider.projectView.views.getSolutionView

class UnityAwareStartIndexAction : StartIndexAction() {
  override fun update(e: AnActionEvent) {
    if (isInUnityExplorer(e)) {
      e.presentation.isEnabledAndVisible = false
      return
    }
    super.update(e)
  }
}

class UnityAwareStopIndexAction : StopIndexAction() {
  override fun update(e: AnActionEvent) {
    if (isInUnityExplorer(e)) {
      e.presentation.isEnabledAndVisible = false
      return
    }
    super.update(e)
  }
}

private fun isInUnityExplorer(e: AnActionEvent): Boolean =
  e.getSolutionView()?.id == UnityExplorer.ID
