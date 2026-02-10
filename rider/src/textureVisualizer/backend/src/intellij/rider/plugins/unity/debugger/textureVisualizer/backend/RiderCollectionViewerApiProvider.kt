// Copyright 2000-2025 JetBrains s.r.o. and contributors. Use of this source code is governed by the Apache 2.0 license.
package intellij.rider.plugins.unity.debugger.textureVisualizer.backend

import com.intellij.platform.rpc.backend.RemoteApiProvider
import fleet.rpc.remoteApiDescriptor
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureDataApi

internal class RiderTextureDataApiProvider : RemoteApiProvider {
  override fun RemoteApiProvider.Sink.remoteApis() {
    remoteApi(remoteApiDescriptor<RiderTextureDataApi>()) {
      BackendRiderTextureDataApi()
    }
  }
}
