package intellij.rider.plugins.unity.debugger.textureVisualizer.backend

import com.jetbrains.rider.debugger.DelegatedDotNetValue
import com.jetbrains.rider.debugger.DotNetNamedValue
import com.jetbrains.rider.debugger.DotNetValue
import com.jetbrains.rider.debugger.IDotNetValue
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureAdditionalActionParams
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTexturePropertiesData
import intellij.rider.plugins.unity.debugger.textureVisualizer.UnityTextureAdditionalActionResult
import intellij.rider.plugins.unity.debugger.textureVisualizer.UnityTextureInfo

interface RiderTextureAccessor {
    suspend fun evaluateTexture(timeoutForAdvanceUnityEvaluation: Int): UnityTextureAdditionalActionResult
}

class RiderTextureAccessorImpl(private val dotNetValue: IDotNetValue, private val unityTextureAdditionalAction: UnityTexturePropertiesData) : RiderTextureAccessor {
    override suspend fun evaluateTexture(timeoutForAdvanceUnityEvaluation: Int): UnityTextureAdditionalActionResult {
        val stackFrame = when (dotNetValue) {
            is DotNetValue -> dotNetValue.frame
            is DotNetNamedValue -> dotNetValue.frame
            is DelegatedDotNetValue -> dotNetValue.value.frame
            else -> error("Unsupported value. Can't get a stack frame")
        }

        val additionalActionResult = unityTextureAdditionalAction.evaluateTexture
            .startSuspending(
                dotNetValue.lifetime,
                UnityTextureAdditionalActionParams(timeoutForAdvanceUnityEvaluation, stackFrame.frameProxy.id)
            )

        val rdti = additionalActionResult.unityTextureInfo
        val unityTextureInfo = if (rdti == null) null else UnityTextureInfo(
            rdti.width,
            rdti.height,
            rdti.pixels,
            rdti.originalWidth,
            rdti.originalHeight,
            rdti.graphicsTextureFormat,
            rdti.textureName,
            rdti.hasAlphaChannel
        )

        return UnityTextureAdditionalActionResult(additionalActionResult.error, unityTextureInfo, additionalActionResult.isTerminated)
    }
}
