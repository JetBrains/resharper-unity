package intellij.rider.plugins.unity.debugger.textureVisualizer.frontend

import com.intellij.platform.debugger.impl.shared.proxy.XDebugManagerProxy
import com.intellij.platform.debugger.impl.shared.proxy.XDebugSessionProxy
import com.intellij.xdebugger.frame.XDebuggerTreeNodeHyperlink
import com.intellij.xdebugger.frame.XValue
import com.jetbrains.rider.debugger.RiderDebuggerLinkProvider
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureDataApi
import kotlinx.coroutines.CoroutineScope

class UnityTextureLinkProvider : RiderDebuggerLinkProvider {
    override suspend fun getAdditionalLink(scope: CoroutineScope, session: XDebugSessionProxy, value: XValue): XDebuggerTreeNodeHyperlink? {
        if (!XDebugManagerProxy.getInstance().hasBackendCounterpart(value))
            return null
        return XDebugManagerProxy.getInstance().withId(value, session) { valueId ->
            val accessorId = RiderTextureDataApi.getInstance().findTextureAccessor(valueId).await() ?: return@withId null
            UnityTextureHyperLink(scope, session, accessorId)
        }
    }
}
