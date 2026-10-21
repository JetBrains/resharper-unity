package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.util.concurrency.annotations.RequiresEdt
import kotlinx.coroutines.Job
import org.jetbrains.annotations.ApiStatus

/** The three steps of the Unity for Agents setup, in the order the setup chain runs them. */
@ApiStatus.Internal
enum class UnityAgentsSetupStep {
    CLI,
    PIPELINE_PACKAGE,
    CLI_SKILL,
}

@ApiStatus.Internal
interface UnityAgentsSetupProvider {
    /** False when the setup does not apply: the feature is off, the project is default, not a Unity project, or not trusted. */
    fun isAvailable(): Boolean

    /**
     * Reads the steps that are not done yet, in the order of [UnityAgentsSetupStep].
     * Returns null when the setup is not [isAvailable].
     * Runs the Unity CLI up to three times.
     */
    suspend fun readMissingSteps(): List<UnityAgentsSetupStep>?

    /** True while a setup chain runs. */
    fun isRunning(): Boolean

    /**
     * Reports the install click at [source] and runs the setup chain.
     * [source] is the place where the user started the chain. The statistics report it for each install of the chain.
     * [missingSteps] are the steps that the caller showed as missing.
     * A call while a chain runs returns the job of that chain and reports nothing.
     */
    fun requestSetup(source: UnityAgentsSource, missingSteps: List<UnityAgentsSetupStep>): Job

    /**
     * Reports the update click at [source] and runs the update chain for the Unity CLI and the Pipeline package.
     * The statistics report [source] for each update of the chain.
     */
    fun requestUpdate(source: UnityAgentsSource)

    /** Returns when the running setup chain ends. Starts no chain. */
    suspend fun awaitSetup()

    /** Reports the click at [source] and opens the Unity for Agents settings page. */
    @RequiresEdt
    fun openSettings(source: UnityAgentsSource)

    /** Reports the click on "Dismiss" at [source]. The caller hides its install links. */
    fun reportDismiss(source: UnityAgentsSource)

    companion object {
        fun getInstance(project: Project): UnityAgentsSetupProvider = project.service<UnityAgentsSetupService>()
    }
}
