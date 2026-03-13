package intellij.rider.plugins.unity.debugger.textureVisualizer.frontend

import com.intellij.frontend.FrontendApplicationInfo
import com.intellij.frontend.FrontendType
import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.intellij.platform.debugger.impl.shared.proxy.XDebugManagerProxy
import com.intellij.platform.debugger.impl.shared.proxy.XDebugSessionProxy
import com.intellij.xdebugger.frame.XDebuggerTreeNodeHyperlink
import com.intellij.xdebugger.frame.XValue
import com.intellij.xdebugger.impl.collection.visualizer.XDebuggerNodeLinkActionProvider
import com.intellij.xdebugger.impl.frame.XDebugView
import com.intellij.xdebugger.impl.ui.tree.nodes.XValueNodeImpl
import com.jetbrains.rider.debugger.shared.RiderDebuggerLinkProvider
import intellij.rider.plugins.unity.debugger.textureVisualizer.common.RiderTextureDataApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

internal class UnityTextureXDebuggerNodeLinkProvider : XDebuggerNodeLinkActionProvider {
    override suspend fun CoroutineScope.provideHyperlink(
        project: Project,
        node: XValueNodeImpl,
    ): XDebuggerTreeNodeHyperlink? {
        //TODO(Korovin): RIDER-136325 [Split Debugger, RemDev] Collection visualizers don't work
        if (FrontendApplicationInfo.getFrontendType() is FrontendType.Monolith)
            return null

        val session = withContext(Dispatchers.EDT) {
            XDebugView.getSessionProxy(node.tree)
        } ?: return null
        return hyperlink(node.valueContainer, session, this)
    }
}

class UnityTextureLinkProvider : RiderDebuggerLinkProvider {
    override suspend fun getAdditionalLink(scope: CoroutineScope, session: XDebugSessionProxy, value: XValue): XDebuggerTreeNodeHyperlink? {
        //TODO(Korovin): RIDER-136325 [Split Debugger, RemDev] Collection visualizers don't work
        if (FrontendApplicationInfo.getFrontendType() is FrontendType.Remote)
            return null

        return hyperlink(value, session, scope)
    }
}

private suspend fun hyperlink(
    value: XValue,
    session: XDebugSessionProxy,
    scope: CoroutineScope
): UnityTextureHyperLink? {
    if (!XDebugManagerProxy.getInstance().hasBackendCounterpart(value))
        return null
    return XDebugManagerProxy.getInstance().withId(value, session) { valueId ->
        val accessorId = RiderTextureDataApi.getInstance().findTextureAccessor(valueId).await() ?: return@withId null
        UnityTextureHyperLink(scope, session, accessorId)
    }
}
