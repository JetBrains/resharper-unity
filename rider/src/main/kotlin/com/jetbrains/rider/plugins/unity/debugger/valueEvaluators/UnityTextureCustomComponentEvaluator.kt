package com.jetbrains.rider.plugins.unity.debugger.valueEvaluators

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.FrameWrapper
import com.intellij.xdebugger.frame.XValueNode
import com.intellij.xdebugger.impl.ui.tree.nodes.XValueNodeImpl
import com.jetbrains.rider.debugger.evaluators.RiderCustomComponentEvaluator
import com.jetbrains.rider.debugger.getSimplePresentation
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesProxy
import com.jetbrains.rider.model.debuggerWorker.ValueFlags
import com.jetbrains.rider.plugins.unity.UnityBundle
import java.awt.CardLayout
import java.awt.event.MouseEvent
import javax.swing.JPanel


class UnityTextureCustomComponentEvaluator : RiderCustomComponentEvaluator("UnityTextureEvaluator") {

    override fun startEvaluation(callback: XFullValueEvaluationCallback) {
        callback.evaluated(properties.value.getSimplePresentation())
    }

    override fun isApplicable(node: XValueNode, properties: ObjectPropertiesProxy): Boolean =
        !properties.valueFlags.contains(ValueFlags.IsNull)
        && node is XValueNodeImpl
        && (properties.instanceType.definitionTypeFullName == "UnityEngine.Texture2D"
            || properties.instanceType.definitionTypeFullName == "UnityEngine.RenderTexture")

    override fun show(event: MouseEvent, project: Project, editor: Editor?) {
        val panel = FrameWrapper(project = project,
                                 dimensionKey = "texture-debugger",
                                 title = UnityBundle.message("debugging.texture.preview.title"),
                                 isDialog = true,
                                 component = JPanel(CardLayout()))

        lifetime.onTermination { panel.close() }

        val callback = EvaluationCallback(panel.component!!, this, project)
        this.startEvaluation(callback)
        panel.show()
    }
}