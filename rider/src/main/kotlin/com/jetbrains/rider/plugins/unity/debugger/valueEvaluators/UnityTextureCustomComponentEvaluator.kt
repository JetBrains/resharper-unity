package com.jetbrains.rider.plugins.unity.debugger.valueEvaluators

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.FrameWrapper
import com.intellij.xdebugger.XDebugSession
import com.intellij.xdebugger.frame.XValueNode
import com.intellij.xdebugger.frame.XValuePlace
import com.jetbrains.rd.platform.util.getLogger
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.debugger.RiderDebuggerStatisticsCollector
import com.jetbrains.rider.debugger.evaluators.RiderCustomComponentEvaluator
import com.jetbrains.rider.debugger.evaluators.SELECTED_VIEW_KEY_PREFIX
import com.jetbrains.rider.debugger.getSimplePresentation
import com.jetbrains.rider.debugger.visualizers.RiderDebuggerValuePresenter
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesProxy
import java.awt.CardLayout
import java.awt.event.MouseEvent
import javax.swing.JComponent
import javax.swing.JPanel

private val logger = getLogger<UnityTextureCustomComponentEvaluator>()

class UnityTextureCustomComponentEvaluator : RiderCustomComponentEvaluator("") {
    private lateinit var place: XValuePlace
    private lateinit var lifetime: Lifetime
    private lateinit var session: XDebugSession
    private lateinit var properties: ObjectPropertiesProxy
    private lateinit var presenters: List<RiderDebuggerValuePresenter>
    private lateinit var node: XValueNode

    override fun startEvaluation(callback: XFullValueEvaluationCallback) {
        callback.evaluated(properties.value.getSimplePresentation())
    }

    override fun createComponent(fullValue: String?) : JComponent? {
        val tabs = presenters.flatMap { it.createTabs(node, properties, fullValue, place, session, lifetime) }.toTypedArray()
        if (tabs.isEmpty()) {
            logger.warn("The following presenters [${presenters.joinToString(", ") { it::class.java.simpleName }}] produced zero possible tabs")
            return null
        }
        if (tabs.size == 1) {
            val presenterTab = tabs.first()
            presenterTab.contentType?.let {
                RiderDebuggerStatisticsCollector.Util.logViewLinkHit(session.project, it)
            }
            return presenterTab.component
        }
        return tabs(SELECTED_VIEW_KEY_PREFIX, session.project, lifetime, *tabs)
    }

    override fun isApplicable(node: XValueNode, properties: ObjectPropertiesProxy): Boolean =
        properties.instanceType.definitionTypeFullName == "UnityEngine.Texture2D"

    override fun initialize(node: XValueNode,
                            shouldIgnorePropertiesComputation: () -> Boolean,
                            valuePresenterList: List<RiderDebuggerValuePresenter>,
                            properties: ObjectPropertiesProxy,
                            session: XDebugSession,
                            lifetime: Lifetime,
                            place: XValuePlace) {
        this.node = node
        this.presenters = valuePresenterList
        this.properties = properties
        this.session = session
        this.lifetime = lifetime
        this.place = place
    }

    override fun show(event: MouseEvent, project: Project, editor: Editor?) {
        val panel = FrameWrapper(project = project,
                                 dimensionKey = "texturedebugger",
                                 title = "Texture Preview",
                                 isDialog = true,
                                 component = JPanel(CardLayout()))

        lifetime.onTermination { panel.close() }

        val callback = EvaluationCallback(panel.component!!, this, project)
        this.startEvaluation(callback)
        panel.show()
    }
}