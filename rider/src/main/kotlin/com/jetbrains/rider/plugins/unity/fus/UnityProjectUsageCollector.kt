@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.fus

import com.intellij.internal.statistic.beans.MetricEvent
import com.intellij.internal.statistic.eventLog.EventLogGroup
import com.intellij.internal.statistic.eventLog.events.EventFields
import com.intellij.internal.statistic.service.fus.collectors.ProjectUsagesCollector
import com.intellij.openapi.progress.ProgressIndicator
import com.intellij.openapi.project.Project
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.platform.util.createNestedAsyncPromise
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.isNotAlive
import com.jetbrains.rd.util.lifetime.onTermination
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import org.jetbrains.concurrency.CancellablePromise

class UnityProjectUsageCollector  : ProjectUsagesCollector() {
    companion object {
        private val RIDER_UNITY_STATE_GROUP = EventLogGroup("rider.unity.state", 1)

        private val UnityTechnology = RIDER_UNITY_STATE_GROUP.registerEvent(
            "unityTechnology",
            EventFields.String("id", mutableListOf("HDRP", "CoreRP", "URP", "ECS", "InputSystem", "Burst", "Odin", "Peek", "UniRx", "UniTask")),
            EventFields.Boolean("isDiscovered"),
        )
    }

    override fun getGroup(): EventLogGroup {
        return RIDER_UNITY_STATE_GROUP
    }

    override fun getMetrics(project: Project, indicator: ProgressIndicator?): CancellablePromise<out MutableSet<MetricEvent>> {
        val promise = project.lifetime.createNestedAsyncPromise<MutableSet<MetricEvent>>()

        UIUtil.invokeLaterIfNeeded {
            val result = mutableSetOf<MetricEvent>()
            if (project.lifetime.isNotAlive) {
                return@invokeLaterIfNeeded
            }
            val lifetimeDef = project.lifetime.createNested()
            val model = project.solution.frontendBackendModel
            model.isTechnologyDiscoveringFinished.advise(lifetimeDef.lifetime) {
                if (it) {
                    for ((id, state) in model.discoveredTechnologies) {
                        result.add(UnityTechnology.metric(id, state))
                    }
                    promise.setResult(result)
                    lifetimeDef.terminate()
                }
            }
        }
        return promise
    }
}