package com.jetbrains.rider.plugins.unity.debugger.valueEvaluators

import com.intellij.openapi.Disposable
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.FrameWrapper
import com.intellij.openapi.util.Disposer
import com.intellij.ui.JBColor
import com.intellij.xdebugger.XDebugSession
import com.intellij.xdebugger.frame.XValueNode
import com.intellij.xdebugger.frame.XValuePlace
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rider.debugger.IDotNetValue
import com.jetbrains.rider.debugger.evaluators.RiderCustomComponentEvaluator
import com.jetbrains.rider.debugger.getSimplePresentation
import com.jetbrains.rider.debugger.visualizers.RiderDebuggerValuePresenter
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesBase
import com.jetbrains.rider.plugins.unity.UnityBundle
import java.awt.CardLayout
import java.awt.Color
import java.awt.event.ActionEvent
import java.awt.event.KeyEvent
import java.awt.event.MouseEvent
import java.awt.event.WindowEvent
import java.util.function.BooleanSupplier
import javax.swing.*


class UnityTextureCustomComponentEvaluator(node: XValueNode,
                                           properties: ObjectPropertiesBase,
                                           session: XDebugSession,
                                           place: XValuePlace,
                                           presenters: List<RiderDebuggerValuePresenter>,
                                           private val lifetimeDefinition: LifetimeDefinition,
                                           onPopupBeingClicked: () -> Unit,
                                           shouldIgnorePropertiesComputation: () -> Boolean,
                                           shouldUpdatePresentation: Boolean,
                                           dotNetValue: IDotNetValue,
                                           actionName: String) : RiderCustomComponentEvaluator(node, properties, session, place, presenters,
                                                                                               lifetimeDefinition.lifetime,
                                                                                               onPopupBeingClicked,
                                                                                               shouldIgnorePropertiesComputation,
                                                                                               shouldUpdatePresentation, dotNetValue,
                                                                                               actionName) {

    override fun startEvaluation(callback: XFullValueEvaluationCallback) {
        callback.evaluated(properties.value.getSimplePresentation())
    }

    override fun show(event: MouseEvent, project: Project, editor: Editor?) {
        val panel = FrameWrapper(
            project = project,
            dimensionKey = "texture-debugger",
            title = UnityBundle.message("debugging.texture.preview.title"),
            isDialog = true,
            component = JPanel(CardLayout()).apply { background = JBColor.RED },
        ).apply {
            closeOnEsc()
        }

        val callback = EvaluationCallback(panel.component!!, this, project)
        this.startEvaluation(callback)
        Disposer.register(panel) { lifetimeDefinition.terminate() }
        panel.show()
        lifetime.onTerminationIfAlive  { panel.close() }
    }
}