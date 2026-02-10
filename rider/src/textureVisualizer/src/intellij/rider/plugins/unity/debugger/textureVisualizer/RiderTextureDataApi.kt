// Copyright 2000-2025 JetBrains s.r.o. and contributors. Use of this source code is governed by the Apache 2.0 license.
package intellij.rider.plugins.unity.debugger.textureVisualizer

import com.intellij.platform.debugger.impl.rpc.TimeoutSafeResult
import com.intellij.platform.debugger.impl.rpc.XValueId
import com.intellij.platform.rpc.Id
import com.intellij.platform.rpc.RemoteApiProviderService
import com.intellij.platform.rpc.UID
import fleet.rpc.RemoteApi
import fleet.rpc.Rpc
import fleet.rpc.remoteApiDescriptor
import kotlinx.serialization.Serializable
import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
@Serializable
data class RiderTextureAccessorId(override val uid: UID) : Id

@ApiStatus.Internal
@Rpc
interface RiderTextureDataApi : RemoteApi<Unit> {
    suspend fun findTextureAccessor(valueId: XValueId): TimeoutSafeResult<RiderTextureAccessorId?>

    suspend fun evaluateTexture(
        accessorId: RiderTextureAccessorId,
        timeoutForAdvanceUnityEvaluation: Int
    ): UnityTextureAdditionalActionResult

    companion object {
        @JvmStatic
        suspend fun getInstance(): RiderTextureDataApi {
            return RemoteApiProviderService.resolve(remoteApiDescriptor<RiderTextureDataApi>())
        }
    }
}

@ApiStatus.Internal
@Serializable
data class UnityTextureAdditionalActionResult (
    val error: String?,
    val unityTextureInfo: UnityTextureInfo?,
    val isTerminated: Boolean
)

@ApiStatus.Internal
@Serializable
class UnityTextureInfo (
    val width: Int,
    val height: Int,
    val pixels: List<Int>,
    val originalWidth: Int,
    val originalHeight: Int,
    val graphicsTextureFormat: String,
    val textureName: String,
    val hasAlphaChannel: Boolean
)