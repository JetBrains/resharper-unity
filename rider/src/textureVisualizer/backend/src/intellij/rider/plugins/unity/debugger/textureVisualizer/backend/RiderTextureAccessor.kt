package intellij.rider.plugins.unity.debugger.textureVisualizer.backend

import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rider.debugger.DelegatedDotNetValue
import com.jetbrains.rider.debugger.DotNetNamedValue
import com.jetbrains.rider.debugger.DotNetValue
import com.jetbrains.rider.debugger.IDotNetValue
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureAdditionalActionParams
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTexturePropertiesData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import intellij.rider.plugins.unity.debugger.textureVisualizer.common.UnityTextureAdditionalActionResult
import intellij.rider.plugins.unity.debugger.textureVisualizer.common.UnityTextureInfo
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

interface RiderTextureAccessor {
    suspend fun evaluateTexture(): UnityTextureAdditionalActionResult
}

class RiderTextureAccessorImpl(
    private val project: Project,
    private val dotNetValue: IDotNetValue,
    private val unityTextureAdditionalAction: UnityTexturePropertiesData
) : RiderTextureAccessor {
    override suspend fun evaluateTexture(): UnityTextureAdditionalActionResult {
        val stackFrame = when (dotNetValue) {
            is DotNetValue -> dotNetValue.frame
            is DotNetNamedValue -> dotNetValue.frame
            is DelegatedDotNetValue -> dotNetValue.value.frame
            else -> error("Unsupported value. Can't get a stack frame")
        }

        val additionalActionResult = withContext(Dispatchers.EDT) {
            val timeoutForAdvanceUnityEvaluation = project.solution.frontendBackendModel.backendSettings.forcedTimeoutForAdvanceUnityEvaluation.valueOrThrow
            unityTextureAdditionalAction.evaluateTexture
                .startSuspending(
                    dotNetValue.lifetime,
                    UnityTextureAdditionalActionParams(timeoutForAdvanceUnityEvaluation, stackFrame.frameProxy.id)
                )
        }

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
