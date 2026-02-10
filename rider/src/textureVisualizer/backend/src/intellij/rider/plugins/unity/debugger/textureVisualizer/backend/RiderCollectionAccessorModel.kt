// Copyright 2000-2025 JetBrains s.r.o. and contributors. Use of this source code is governed by the Apache 2.0 license.
package intellij.rider.plugins.unity.debugger.textureVisualizer.backend

import com.intellij.platform.kernel.ids.BackendValueIdType
import com.intellij.platform.kernel.ids.findValueById
import com.intellij.platform.kernel.ids.storeValueGlobally
import com.intellij.xdebugger.impl.XDebugSessionImpl
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureAccessorId
import kotlinx.coroutines.CoroutineScope
import org.jetbrains.annotations.ApiStatus

@ConsistentCopyVisibility
@ApiStatus.Internal
data class RiderTextureAccessorModel internal constructor(
  val coroutineScope: CoroutineScope,
  val accessor: RiderTextureAccessor,
  val session: XDebugSessionImpl,
)

@ApiStatus.Internal
fun RiderTextureAccessorId.findValue(): RiderTextureAccessorModel? {
  return findValueById(this, type = RiderTextureAccessorIdType)
}

@ApiStatus.Internal
fun RiderTextureAccessor.storeGlobally(coroutineScope: CoroutineScope, session: XDebugSessionImpl): RiderTextureAccessorId {
  return storeValueGlobally(coroutineScope, RiderTextureAccessorModel(coroutineScope, this, session), type = RiderTextureAccessorIdType)
}

private object RiderTextureAccessorIdType : BackendValueIdType<RiderTextureAccessorId, RiderTextureAccessorModel>(
  ::RiderTextureAccessorId)
