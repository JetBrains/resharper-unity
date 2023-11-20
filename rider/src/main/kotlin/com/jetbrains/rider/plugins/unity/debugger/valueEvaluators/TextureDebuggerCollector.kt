package com.jetbrains.rider.plugins.unity.debugger.valueEvaluators

import com.intellij.internal.statistic.IdeActivityDefinition
import com.intellij.internal.statistic.StructuredIdeActivity
import com.intellij.internal.statistic.eventLog.EventLogGroup
import com.intellij.internal.statistic.eventLog.events.*
import com.intellij.internal.statistic.service.fus.collectors.CounterUsagesCollector
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.AtomicReference
import java.util.concurrent.TimeUnit

internal class TextureDebuggerCollector : CounterUsagesCollector() {
    companion object {

        private val GROUP = EventLogGroup("rider.unity.debugger.texturevisualizers", 1)

        private val TEXTURE_WIDTH = EventFields.Int("texture_width")
        private val TEXTURE_HEIGHT = EventFields.Int("texture_height")

        val STAGE_CLASS = EventFields.Enum<StageType>("stage")
        val TIME_SINCE_START = EventFields.Long("time_since_start")
        val EXECUTION_RESULT_TYPE: EnumEventField<ExecutionResult> = EventFields.Enum("finish_type", ExecutionResult::class.java)
        val TEXTURE_DEBUGGING_ACTIVITY: IdeActivityDefinition = GROUP.registerIdeActivity(null, emptyArray(), arrayOf(EXECUTION_RESULT_TYPE))
        val TEXTURE_DEBUGGING_STAGE: VarargEventId = TEXTURE_DEBUGGING_ACTIVITY.registerStage("stage", arrayOf(STAGE_CLASS, TIME_SINCE_START, TEXTURE_WIDTH, TEXTURE_HEIGHT))

        fun registerStageStarted(activityAtomicReference: AtomicReference<StructuredIdeActivity?>, stage: StageType,
                                 textureInfo: UnityTextureCustomComponentEvaluator.TextureInfo? = null) {

            val activity = activityAtomicReference.get()
            if(textureInfo == null)
                activity?.stageStarted(TEXTURE_DEBUGGING_STAGE) { listOf(
                    STAGE_CLASS.with(stage),
                    TIME_SINCE_START.with(TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - activity.startedTimestamp))
                )}
            else
                activity?.stageStarted(TEXTURE_DEBUGGING_STAGE) { listOf(
                    STAGE_CLASS.with(stage),
                    TIME_SINCE_START.with(TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - activity.startedTimestamp)),
                    TEXTURE_WIDTH.with(textureInfo.Width),
                    TEXTURE_HEIGHT.with(textureInfo.Height)
                )}
        }

        fun createTextureDebuggingActivity(project: Project?):AtomicReference<StructuredIdeActivity?> {
            return AtomicReference(TEXTURE_DEBUGGING_ACTIVITY.started(project))
        }

        fun finishActivity(activityAtomicReference:  AtomicReference<StructuredIdeActivity?>, finishType: ExecutionResult) {
            val activity = activityAtomicReference.getAndSet(null)
            activity?.finished { listOf(EXECUTION_RESULT_TYPE.with(finishType)) }
        }
    }

    override fun getGroup() = GROUP
}

enum class ExecutionResult {
    Succeed, Failed, Terminated;
}

enum class StageType(val string: String) {
    LOAD_DLL("load_dll"),
    EVALUATE_VALUE_NAME("evaluate_value_name"),
    TEXTURE_PIXELS_REQUEST("texture_pixels_request"),
    PREPARE_TEXTURE_PIXELS_TO_SHOW("prepare_texture_pixels_to_show")
}