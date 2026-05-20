package intellij.rider.plugins.unity.debugger.textureVisualizer.backend

import com.intellij.platform.rpc.backend.RemoteApiProvider
import fleet.rpc.remoteApiDescriptor
import intellij.rider.plugins.unity.debugger.textureVisualizer.common.RiderTextureDataApi

internal class RiderTextureDataApiProvider : RemoteApiProvider {
  override fun RemoteApiProvider.Sink.remoteApis() {
    remoteApi(remoteApiDescriptor<RiderTextureDataApi>()) {
      BackendRiderTextureDataApi()
    }
  }
}
