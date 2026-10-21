package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.internal.statistic.eventLog.EventLogGroup
import com.intellij.internal.statistic.eventLog.events.EventFields
import com.intellij.internal.statistic.eventLog.events.EventId3
import com.intellij.internal.statistic.eventLog.events.VarargEventId
import com.intellij.internal.statistic.service.fus.collectors.CounterUsagesCollector
import com.intellij.openapi.project.Project
import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
enum class UnityAgentsSource {
    NOTIFICATION,
    SETTINGS,
    AGENT_WIDGET,
}

@ApiStatus.Internal
enum class UnityAgentsAction {
    INSTALL,
    UPDATE,
    RETRY,
    DISMISS,
    OPEN_SETTINGS,
    BROWSE_SKILLS,
    LEARN_MORE,
}

@ApiStatus.Internal
enum class UnityAgentsOperation {
    INSTALLING,
    UPDATING,
}

@ApiStatus.Internal
enum class UnityAgentsResult {
    SUCCESS,
    FAILED,
}

@ApiStatus.Internal
object UnityForAgentsUsagesCollector : CounterUsagesCollector() {
    private val GROUP = EventLogGroup("dotnet.unity.agents", 1)

    private val ACTION = EventFields.Enum("action", UnityAgentsAction::class.java, "The action that the user clicked.")
    private val SOURCE = EventFields.Enum("source", UnityAgentsSource::class.java, "The place where the user clicked.")
    private val MISSING_COMPONENTS =
        EventFields.EnumList<UnityAgentsSetupStep>("missing_components", "The Unity for Agents components that the user saw as missing.")
    private val COMPONENT =
        EventFields.Enum("component", UnityAgentsSetupStep::class.java, "The Unity for Agents component of the operation.")
    private val OPERATION = EventFields.Enum("operation", UnityAgentsOperation::class.java, "The operation that Rider ran.")
    private val RESULT = EventFields.Enum("result", UnityAgentsResult::class.java, "The result of the operation.")
    private val AGENT = EventFields.Enum("agent", UnityMcpStore::class.java, "The agent of the MCP entry.")
    private val MCP_OPERATION = EventFields.Enum("operation", UnityMcpOperation::class.java, "The operation that Rider ran.")

    private val ACTION_CLICKED: VarargEventId = GROUP.registerVarargEvent("action_clicked", ACTION, SOURCE, MISSING_COMPONENTS)
    private val COMPONENT_OPERATION_FINISHED: VarargEventId =
        GROUP.registerVarargEvent("component_operation_finished", COMPONENT, OPERATION, SOURCE, RESULT)
    private val MCP_OPERATION_FINISHED: EventId3<UnityMcpStore, UnityMcpOperation, UnityAgentsResult> =
        GROUP.registerEvent("mcp_operation_finished", AGENT, MCP_OPERATION, RESULT)

    override fun getGroup(): EventLogGroup = GROUP

    fun logActionClicked(
        project: Project,
        action: UnityAgentsAction,
        source: UnityAgentsSource,
        missingComponents: List<UnityAgentsSetupStep> = emptyList(),
    ) {
        if (missingComponents.isEmpty()) {
            ACTION_CLICKED.log(project, ACTION.with(action), SOURCE.with(source))
        } else {
            ACTION_CLICKED.log(project, ACTION.with(action), SOURCE.with(source), MISSING_COMPONENTS.with(missingComponents))
        }
    }

    fun logComponentOperationFinished(
        project: Project,
        component: UnityAgentsSetupStep,
        operation: UnityAgentsOperation,
        source: UnityAgentsSource,
        result: UnityAgentsResult,
    ): Unit = COMPONENT_OPERATION_FINISHED.log(
        project,
        COMPONENT.with(component),
        OPERATION.with(operation),
        SOURCE.with(source),
        RESULT.with(result),
    )

    fun logMcpOperationFinished(project: Project, store: UnityMcpStore, operation: UnityMcpOperation, result: UnityAgentsResult): Unit =
        MCP_OPERATION_FINISHED.log(project, store, operation, result)
}
