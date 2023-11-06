package com.jetbrains.rider.plugins.unity.debugger.valueEvaluators

import com.intellij.xdebugger.XDebugSession
import com.intellij.xdebugger.frame.XValueNode
import com.intellij.xdebugger.frame.XValuePlace
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.debugger.IDotNetValue
import com.jetbrains.rider.debugger.evaluators.RiderCustomComponentEvaluator
import com.jetbrains.rider.debugger.evaluators.RiderCustomComponentEvaluatorFactory
import com.jetbrains.rider.debugger.visualizers.RiderDebuggerValuePresenter
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesBase
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesProxy
import com.jetbrains.rider.model.debuggerWorker.ValueFlags

class UnityTextureCustomComponentEvaluatorFactory : RiderCustomComponentEvaluatorFactory() {
    override fun createEvaluator(node: XValueNode,
                                 properties: ObjectPropertiesBase,
                                 session: XDebugSession,
                                 place: XValuePlace,
                                 presenters: List<RiderDebuggerValuePresenter>,
                                 lifetime: Lifetime,
                                 onPopupBeingClicked: () -> Unit,
                                 shouldIgnorePropertiesComputation: () -> Boolean,
                                 shouldUpdatePresentation: Boolean,
                                 dotNetValue: IDotNetValue,
                                 actionName: String): RiderCustomComponentEvaluator {
        return UnityTextureCustomComponentEvaluator(node,
                                                    properties,
                                                    session,
                                                    place,
                                                    presenters,
                                                    lifetime,
                                                    onPopupBeingClicked,
                                                    shouldIgnorePropertiesComputation,
                                                    shouldUpdatePresentation,
                                                    dotNetValue,
                                                    actionName)
    }

    override fun isApplicable(node: XValueNode, properties: ObjectPropertiesBase, session: XDebugSession): Boolean {
        if (properties is ObjectPropertiesProxy)
            return (session.debugProcess as? DotNetDebugProcess)?.isIl2Cpp == false
                   && !properties.valueFlags.contains(ValueFlags.IsNull)
                   && (properties.instanceType.definitionTypeFullName == "UnityEngine.Texture2D"
                       || properties.instanceType.definitionTypeFullName == "UnityEngine.RenderTexture")

        return false
    }
}