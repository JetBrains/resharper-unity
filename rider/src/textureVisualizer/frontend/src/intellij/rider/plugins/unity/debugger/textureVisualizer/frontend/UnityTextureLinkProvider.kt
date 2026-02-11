package intellij.rider.plugins.unity.debugger.textureVisualizer.frontend

import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.intellij.platform.debugger.impl.shared.proxy.XDebugManagerProxy
import com.intellij.xdebugger.frame.XDebuggerTreeNodeHyperlink
import com.intellij.xdebugger.impl.collection.visualizer.XDebuggerNodeLinkActionProvider
import com.intellij.xdebugger.impl.frame.XDebugView
import com.intellij.xdebugger.impl.ui.tree.nodes.XValueNodeImpl
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureDataApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class UnityTextureLinkProvider : XDebuggerNodeLinkActionProvider {
    override suspend fun CoroutineScope.provideHyperlink(
        project: Project,
        node: XValueNodeImpl
    ): XDebuggerTreeNodeHyperlink? {
        if (!XDebugManagerProxy.getInstance().hasBackendCounterpart(node.valueContainer)) {
            return null
        }
        val session = withContext(Dispatchers.EDT) {
            XDebugView.getSessionProxy(node.tree)
        } ?: return null
        return XDebugManagerProxy.getInstance().withId(node.valueContainer, session) { valueId ->
            val accessorId = RiderTextureDataApi.getInstance().findTextureAccessor(valueId).await() ?: return@withId null
            UnityTextureHyperLink(this, session, accessorId)
        }
    }
}
