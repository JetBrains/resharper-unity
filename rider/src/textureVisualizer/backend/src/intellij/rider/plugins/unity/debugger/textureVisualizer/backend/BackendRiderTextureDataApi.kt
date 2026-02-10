// Copyright 2000-2025 JetBrains s.r.o. and contributors. Use of this source code is governed by the Apache 2.0 license.
package intellij.rider.plugins.unity.debugger.textureVisualizer.backend

import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.platform.debugger.impl.rpc.TimeoutSafeResult
import com.intellij.platform.debugger.impl.rpc.XValueId
import com.intellij.platform.util.coroutines.childScope
import com.intellij.xdebugger.XDebugSession
import com.intellij.xdebugger.frame.XValue
import com.intellij.xdebugger.impl.XDebugSessionImpl
import com.intellij.xdebugger.impl.rpc.models.BackendXValueModel
import com.jetbrains.rider.debugger.IDotNetValue
import com.jetbrains.rider.debugger.dotnetDebugProcess
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesBase
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesProxy
import com.jetbrains.rider.model.debuggerWorker.ValueFlags
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTexturePropertiesData
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureAccessorId
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureDataApi
import intellij.rider.plugins.unity.debugger.textureVisualizer.UnityTextureAdditionalActionResult
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.async

internal class BackendRiderTextureDataApi : RiderTextureDataApi {
    override suspend fun findTextureAccessor(valueId: XValueId): TimeoutSafeResult<RiderTextureAccessorId?> {
        val valueModel = BackendXValueModel.findById(valueId) ?: return CompletableDeferred(null)
        return valueModel.session.coroutineScope.async {
            val value = valueModel.xValue
            val accessor = createAccessor(value, valueModel.session) ?: return@async null
            val accessorScope = valueModel.session.coroutineScope.childScope("Accessor for XValue ${valueId.uid}")
            accessor.storeGlobally(accessorScope, valueModel.session)
        }
    }

    fun isApplicable(properties: ObjectPropertiesBase, session: XDebugSession): Boolean {
        if (properties is ObjectPropertiesProxy)
            return session.dotnetDebugProcess?.isIl2Cpp == false
                && !properties.valueFlags.contains(ValueFlags.IsNull)
                && (properties.instanceType.definitionTypeFullName == "UnityEngine.Texture2D"
                || properties.instanceType.definitionTypeFullName == "UnityEngine.RenderTexture")

        return false
    }

    private fun createAccessor(
        value: XValue,
        session: XDebugSessionImpl
    ): RiderTextureAccessor? {
        val dotNetValue = value as? IDotNetValue ?: return null
        val properties = dotNetValue.objectProperties ?: return null
        if (isApplicable(properties, session)) {
            val unityTextureAdditionalAction = properties.additionalData.filterIsInstance<UnityTexturePropertiesData>().firstOrNull()
            if (unityTextureAdditionalAction == null)
                return null
            return RiderTextureAccessorImpl(dotNetValue, unityTextureAdditionalAction)
        }
        return null
    }

    override suspend fun evaluateTexture(
        accessorId: RiderTextureAccessorId,
        timeoutForAdvanceUnityEvaluation: Int
    ): UnityTextureAdditionalActionResult {
        val accessorModel = accessorId.accessorModelOrNull() ?: error("Cannot find accessor for $accessorId")
        return accessorModel.accessor.evaluateTexture(timeoutForAdvanceUnityEvaluation)
    }

    private fun RiderTextureAccessorId.accessorModelOrNull(): RiderTextureAccessorModel? {
        val model = findValue()
        if (model == null) {
            logger.warn("Cannot find accessor for $this. Probably it was deleted")
        }
        return model
    }

    companion object {
        private val logger = thisLogger()
    }
}
