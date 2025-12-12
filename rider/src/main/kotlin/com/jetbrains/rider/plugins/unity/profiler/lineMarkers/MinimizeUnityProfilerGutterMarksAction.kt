package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.projectView.solution

class MinimizeUnityProfilerGutterMarksAction : AnAction() {
    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = true
        val editor = e.getData(CommonDataKeys.EDITOR)
        e.presentation.isVisible =
            editor != null &&
            UnityProfilerActiveLineMarkerRenderer.editorRenderSettings(editor) == ProfilerGutterMarkRenderSettings.Default
    }

    override fun getActionUpdateThread() = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.frontendBackendModel.frontendBackendProfilerModel.setGutterMarksRenderSetting.fire(ProfilerGutterMarkRenderSettings.Minimized)
//        LineProfilerUsageCollector.logShowPerformanceHints(project)
    }
}

class MaximizeUnityProfilerGutterMarksAction : AnAction() {
    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = true
        val editor = e.getData(CommonDataKeys.EDITOR)
        e.presentation.isVisible =
            editor != null &&
                UnityProfilerActiveLineMarkerRenderer.editorRenderSettings(editor) == ProfilerGutterMarkRenderSettings.Minimized
    }

    override fun getActionUpdateThread() = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.frontendBackendModel.frontendBackendProfilerModel.setGutterMarksRenderSetting.fire(ProfilerGutterMarkRenderSettings.Default)
//        LineProfilerService.getInstance(project).showAnnotations()
//        LineProfilerUsageCollector.logShowPerformanceHints(project)
    }
}