package com.jetbrains.rider.plugins.unity.debugger.visualizers

import com.intellij.internal.statistic.StructuredIdeActivity
import com.intellij.internal.statistic.eventLog.EventLogGroup
import com.intellij.internal.statistic.eventLog.events.EventFields
import com.intellij.internal.statistic.service.fus.collectors.CounterUsagesCollector
import com.intellij.openapi.project.Project

enum class ExecutionResult {
    Succeed, Failed, Terminated;
}

internal object TextureDebuggerCollector : CounterUsagesCollector() {
    override fun getGroup() = GROUP

    private val GROUP = EventLogGroup("rider.unity.debugger.texturevisualizers", 1)

    private val TEXTURE_WIDTH = EventFields.Int("texture_width")
    private val TEXTURE_HEIGHT = EventFields.Int("texture_height")
    private val EXECUTION_RESULT = EventFields.Enum<ExecutionResult>("execution_result")

    private val TEXTURE_PRESENTATION_ACTIVITY = GROUP.registerIdeActivity("texture_presentation_request", arrayOf(EXECUTION_RESULT),
                                                                          finishEventAdditionalFields = arrayOf(EXECUTION_RESULT))
    private val LOAD_DLL_OPERATION = GROUP.registerIdeActivity("load_dll",
                                                               parentActivity = TEXTURE_PRESENTATION_ACTIVITY,
                                                               finishEventAdditionalFields = arrayOf(EXECUTION_RESULT))
    private val EVALUATE_VALUE_NAME_OPERATION = GROUP.registerIdeActivity("evaluate_value_name",
                                                                          parentActivity = TEXTURE_PRESENTATION_ACTIVITY,
                                                                          finishEventAdditionalFields = arrayOf(EXECUTION_RESULT))
    private val TEXTURE_PIXELS_REQUEST_OPERATION = GROUP.registerIdeActivity("request_texture_pixels",
                                                                             parentActivity = TEXTURE_PRESENTATION_ACTIVITY,
                                                                             finishEventAdditionalFields = arrayOf(EXECUTION_RESULT))

    private val PREPARE_TEXTURE_PIXELS_TO_SHOW_OPERATION = GROUP.registerIdeActivity("prepare_pixels_to_show",
                                                                                     parentActivity = TEXTURE_PRESENTATION_ACTIVITY,
                                                                                     startEventAdditionalFields = arrayOf(TEXTURE_WIDTH,
                                                                                                                          TEXTURE_HEIGHT),
                                                                                     finishEventAdditionalFields = arrayOf(
                                                                                         EXECUTION_RESULT))

    fun texturePresentationRequestStarted(project: Project?): StructuredIdeActivity {
        return TEXTURE_PRESENTATION_ACTIVITY.started(project)
    }

    fun loadDllStarted(project: Project?, parentActivity: StructuredIdeActivity?): StructuredIdeActivity? {
        if (parentActivity == null)
            return null

        return LOAD_DLL_OPERATION.startedWithParent(project, parentActivity)
    }

    fun evaluateValueFullNameStarted(project: Project?, parentActivity: StructuredIdeActivity?): StructuredIdeActivity? {
        if (parentActivity == null)
            return null
        return EVALUATE_VALUE_NAME_OPERATION.startedWithParent(project, parentActivity)
    }

    fun requestTexturePixelsStarted(project: Project?, parentActivity: StructuredIdeActivity?): StructuredIdeActivity? {
        if (parentActivity == null)
            return null
        return TEXTURE_PIXELS_REQUEST_OPERATION.startedWithParent(project, parentActivity)
    }

    fun prepareTextureToShowStarted(project: Project?, parentActivity: StructuredIdeActivity?, width: Int, height: Int): StructuredIdeActivity? {
        if (parentActivity == null)
            return null
        return PREPARE_TEXTURE_PIXELS_TO_SHOW_OPERATION.startedWithParent(project, parentActivity) { listOf(TEXTURE_WIDTH.with(width), TEXTURE_HEIGHT.with(height)) }
    }

    fun finishActivity(activity: StructuredIdeActivity?, executionResult: ExecutionResult) {
        if(activity == null)
            return

        activity.finished { listOf(EXECUTION_RESULT.with(executionResult)) }
    }
}
